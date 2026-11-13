using Microsoft.AspNetCore.Rewrite;
using SmartAPI.Models;
using SmartAPI.Services;
using SmartAPI.Models;

namespace SmartAPI.Services;

public class DataHub
{
    public const string Gateway = "GATEWAY";

    private const int TimelineCap = 500;
    private const int HistoryCap = 200;
    private const int SuggestionThreshold = 3;
    private const string DisconnectedType = "Disconnected";

    private readonly object _lock = new();
    private readonly FarmStore _store;
    private readonly Dictionary<string, string> _macBySensorId = new();
    private readonly Dictionary<int, string> _macById = new();
    private readonly Dictionary<(string Condition, string Action), int> _pairCounts = new();

    public Queue<TelemetryPacket<TelemetryRecord>> TelemetryQueue { get; } = new();
    public PriorityQueue<Alert, int> CriticalAlertQueue { get; } = new();
    public Stack<Command> CommandUndoStack { get; } = new();
    public Dictionary<string, Device> DeviceRegistry { get; } = new();
    public SortedDictionary<DateTime, TelemetryRecord> SensorTimeline { get; } = new();
    public HashSet<string> ActiveErrors { get; } = new();
    public Dictionary<string, AvlTree<TelemetryRecord>> History { get; } = new();
    public List<HistoryEvent> HistoryEvents { get; } = new();

    public DataHub(FarmStore store)
    {
        _store = store;

        foreach (var device in store.Devices)
            RegisterDevice(device);

        SeedHistory("Soil moisture below 30% (SENSOR-008)", "Command", "Pump A On", 4);
        SeedHistory("Soil moisture below 30% (SENSOR-002)", "Command", "Pump A On", 3);
        SeedHistory("Battery below 20% (SENSOR-008)", "Command", "Relay 3 Toggle", 3);
        SeedHistory("Soil moisture below 30% (SENSOR-008)", "Search", "Search Soil Moisture", 3);
    }

    public void RegisterDevice(Device device)
    {
        lock (_lock)
        {
            DeviceRegistry[device.MacAddress] = device;
            _macBySensorId[device.SensorId] = device.MacAddress;
            _macById[device.Id] = device.MacAddress;
        }
    }

    public bool HasDevice(string mac)
    {
        lock (_lock)
        {
            return DeviceRegistry.ContainsKey(mac);
        }
    }

    public List<Device> GetDevices()
    {
        lock (_lock)
        {
            return DeviceRegistry.Values.OrderBy(d => d.Id).ToList();
        }
    }

    public bool IsPumpOn()
    {
        lock (_lock)
        {
            return DeviceRegistry.Values.Any(d => d.IsActuator && d.Category == "Pump" && d.State == "On");
        }
    }

    public void Enqueue(TelemetryPacket<TelemetryRecord> packet)
    {
        lock (_lock)
        {
            TelemetryQueue.Enqueue(packet);
        }
    }

    public void ProcessAll()
    {
        lock (_lock)
        {
            while (ProcessNext())
            {
            }
        }
    }

    private bool ProcessNext()
    {
        if (CriticalAlertQueue.TryDequeue(out var alert, out _))
        {
            _store.AddAlert(alert.SensorId, alert.Zone, alert.Message, alert.Severity);
            return true;
        }

        if (!TelemetryQueue.TryDequeue(out var packet))
            return false;

        var record = packet.Value;

        if (!_macBySensorId.TryGetValue(packet.SensorId, out var mac) ||
            !DeviceRegistry.TryGetValue(mac, out var device))
            return true;

        if (ActiveErrors.Contains(ErrorKey(device.SensorId, DisconnectedType)))
        {
            device.Status = "Offline";
            return true;
        }

        device.Battery = record.Battery;
        device.Status = record.Battery < 20 ? "Warning" : "Online";

        _store.UpdateTelemetry(record);
        AddToTimeline(record);

        if (!History.TryGetValue(mac, out var tree))
        {
            tree = new AvlTree<TelemetryRecord>();
            History[mac] = tree;
        }

        tree.Insert(record.Timestamp, record);

        CheckThresholds(record);
        return true;
    }

    private void AddToTimeline(TelemetryRecord record)
    {
        var key = record.Timestamp;
        while (SensorTimeline.ContainsKey(key))
            key = key.AddTicks(1);

        SensorTimeline[key] = record;

        if (SensorTimeline.Count > TimelineCap)
            SensorTimeline.Remove(SensorTimeline.Keys.First());
    }

    private void CheckThresholds(TelemetryRecord record)
    {
        Evaluate(record, "LowMoisture", record.SoilMoisture < 30, "Low soil moisture", "Warning");
        Evaluate(record, "HighTemp", record.Temperature > 35, "High temperature", "Critical");
        Evaluate(record, "LowBattery", record.Battery < 20, "Low battery", "Warning");
        Evaluate(record, "HighFlow", record.WaterFlow > 25, "Excessive water flow", "Warning");
    }

    private void Evaluate(TelemetryRecord record, string type, bool breached, string message, string severity)
    {
        var key = ErrorKey(record.SensorId, type);

        if (!breached)
        {
            ActiveErrors.Remove(key);
            return;
        }

        if (!ActiveErrors.Add(key))
            return;

        CriticalAlertQueue.Enqueue(new Alert
        {
            SensorId = record.SensorId,
            Zone = record.Zone,
            Message = message,
            Severity = severity,
            Timestamp = DateTime.Now
        }, severity == "Critical" ? 0 : 1);
    }

    public List<TelemetryRecord> GetTimeline(int count)
    {
        lock (_lock)
        {
            var take = Math.Clamp(count, 1, TimelineCap);
            return SensorTimeline.Values.Skip(Math.Max(0, SensorTimeline.Count - take)).ToList();
        }
    }

    public CommandResult ApplyCommand(int deviceId, string action)
    {
        lock (_lock)
        {
            if (!_macById.TryGetValue(deviceId, out var mac) ||
                !DeviceRegistry.TryGetValue(mac, out var device) ||
                !device.IsActuator)
                return new CommandResult { Success = false, Message = "Actuator not found.", DeviceId = deviceId };

            var requested = (action ?? "").Trim().ToLowerInvariant();
            var (label, newState) = requested switch
            {
                "on" => ("On", "On"),
                "off" => ("Off", "Off"),
                "toggle" => ("Toggle", device.State == "On" ? "Off" : "On"),
                _ => ("", "")
            };

            if (label == "")
                return new CommandResult { Success = false, Message = "Unknown action. Use On, Off or Toggle.", DeviceId = deviceId, State = device.State };

            CommandUndoStack.Push(new Command
            {
                DeviceId = deviceId,
                Action = label,
                PreviousState = device.State,
                Timestamp = DateTime.Now
            });

            device.State = newState;
            Record("Command", $"{device.SensorId} {label}", true);

            return new CommandResult
            {
                Success = true,
                Message = $"{device.SensorId} is now {newState}.",
                DeviceId = deviceId,
                State = newState
            };
        }
    }

    public CommandResult Undo()
    {
        lock (_lock)
        {
            if (!CommandUndoStack.TryPop(out var command))
                return new CommandResult { Success = false, Message = "Nothing to undo." };

            if (!_macById.TryGetValue(command.DeviceId, out var mac) ||
                !DeviceRegistry.TryGetValue(mac, out var device))
                return new CommandResult { Success = false, Message = "The device for that command no longer exists.", DeviceId = command.DeviceId };

            device.State = command.PreviousState;
            Record("Command", $"Undo {device.SensorId} {command.Action}", false);

            return new CommandResult
            {
                Success = true,
                Message = $"Undid {command.Action} on {device.SensorId}. State restored to {device.State}.",
                DeviceId = command.DeviceId,
                State = device.State
            };
        }
    }

    public List<HistoryEvent> GetHistoryEvents(int count)
    {
        lock (_lock)
        {
            return HistoryEvents.AsEnumerable().Reverse().Take(Math.Clamp(count, 1, HistoryCap)).ToList();
        }
    }

    public List<Device> SearchDevices(string? category, string? alertState)
    {
        lock (_lock)
        {
            IEnumerable<Device> result = DeviceRegistry.Values;
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(category))
            {
                var text = category.Trim();
                result = result.Where(d => d.Category.Contains(text, StringComparison.OrdinalIgnoreCase));
                parts.Add(text);
            }

            if (!string.IsNullOrWhiteSpace(alertState) && !alertState.Trim().Equals("Any", StringComparison.OrdinalIgnoreCase))
            {
                var state = alertState.Trim();
                var wantsAlerting = !state.Equals("Normal", StringComparison.OrdinalIgnoreCase);
                var matchesAnyType = state.Equals("Normal", StringComparison.OrdinalIgnoreCase) ||
                                     state.Equals("Alerting", StringComparison.OrdinalIgnoreCase);

                var matching = ActiveErrors
                    .Select(SplitKey)
                    .Where(k => matchesAnyType || k.Type.Equals(state, StringComparison.OrdinalIgnoreCase))
                    .Select(k => k.SensorId)
                    .ToHashSet();

                result = result.Where(d => matching.Contains(d.SensorId) == wantsAlerting);
                parts.Add(state);
            }

            if (parts.Count > 0)
                Record("Search", "Search " + string.Join(" ", parts), true);

            return result.OrderBy(d => d.Id).ToList();
        }
    }

    public InsightsResult GetInsights()
    {
        lock (_lock)
        {
            var insights = new InsightsResult();
            var active = ActiveConditions();

            foreach (var pair in _pairCounts
                         .Where(p => p.Value >= SuggestionThreshold && active.Contains(p.Key.Condition))
                         .OrderByDescending(p => p.Value))
            {
                insights.Suggestions.Add(new Suggestion
                {
                    Action = pair.Key.Action,
                    Condition = pair.Key.Condition,
                    Count = pair.Value,
                    Evidence = $"{pair.Key.Action}: you did this {pair.Value} times when {pair.Key.Condition}."
                });
            }

            foreach (var device in DeviceRegistry.Values.Where(d => !d.IsActuator).OrderBy(d => d.Id))
            {
                if (device.Status == "Offline")
                    insights.Proactive.Add(new ProactiveInsight
                    {
                        Subject = device.SensorId,
                        Message = $"{device.SensorId} is offline and not reporting.",
                        Severity = "Critical"
                    });
                else if (device.Battery < 20 || device.Status == "Warning")
                    insights.Proactive.Add(new ProactiveInsight
                    {
                        Subject = device.SensorId,
                        Message = $"{device.SensorId} battery is at {device.Battery:F0}%. Recharge or replace it soon.",
                        Severity = "Warning"
                    });
            }

            foreach (var reading in _store.SnapshotTelemetry().OrderBy(r => r.SensorId))
            {
                if (_macBySensorId.TryGetValue(reading.SensorId, out var mac) &&
                    DeviceRegistry.TryGetValue(mac, out var device) &&
                    device.Status == "Offline")
                    continue;

                if (reading.SoilMoisture < 30)
                    AddReadingInsight(insights, reading.SensorId, $"{reading.SensorId} soil moisture is {reading.SoilMoisture:F0}%, below the normal range of 30-80%.", "Warning");

                if (reading.Temperature > 35)
                    AddReadingInsight(insights, reading.SensorId, $"{reading.SensorId} temperature is {reading.Temperature:F1} C, above the normal maximum of 35 C.", "Critical");

                if (reading.Humidity < 35 || reading.Humidity > 90)
                    AddReadingInsight(insights, reading.SensorId, $"{reading.SensorId} humidity is {reading.Humidity:F0}%, outside the normal range of 35-90%.", "Warning");

                if (reading.WaterFlow > 25)
                    AddReadingInsight(insights, reading.SensorId, $"{reading.SensorId} water flow is {reading.WaterFlow:F1} L/min, above the normal maximum of 25.", "Warning");
            }

            return insights;
        }
    }

    private static void AddReadingInsight(InsightsResult insights, string subject, string message, string severity)
    {
        insights.Proactive.Add(new ProactiveInsight { Subject = subject, Message = message, Severity = severity });
    }

    public List<TelemetryRecord> GetHistory(string mac, DateTime from, DateTime to)
    {
        lock (_lock)
        {
            return History.TryGetValue(mac, out var tree) ? tree.RangeQuery(from, to) : new List<TelemetryRecord>();
        }
    }

    public MeshGraph BuildGraph(HashSet<string> down)
    {
        lock (_lock)
        {
            var graph = new MeshGraph();
            graph.AddNode(Gateway);

            var sensors = DeviceRegistry.Values
                .Where(d => !d.IsActuator)
                .OrderBy(d => d.Id)
                .Select((d, index) => (d.MacAddress, Index: index))
                .Where(s => !down.Contains(s.MacAddress))
                .ToList();

            for (var i = 0; i < sensors.Count; i++)
            {
                var current = sensors[i];
                graph.AddNode(current.MacAddress);

                for (var step = 1; step <= 2; step++)
                {
                    if (i + step < sensors.Count)
                    {
                        var next = sensors[i + step];
                        graph.AddEdge(current.MacAddress, next.MacAddress, (current.Index + next.Index) % 5 + 1);
                    }
                }

                if (i < 3)
                    graph.AddEdge(current.MacAddress, Gateway, current.Index % 5 + 1);
            }

            return graph;
        }
    }

    public TopologyResult GetTopology()
    {
        lock (_lock)
        {
            var graph = BuildGraph(DisconnectedMacs());
            var reachable = graph.Bfs(Gateway);
            var reachableSet = reachable.ToHashSet();
            var mst = graph.Prim(Gateway);
            var sensors = DeviceRegistry.Values.Where(d => !d.IsActuator).OrderBy(d => d.Id).ToList();

            var result = new TopologyResult
            {
                Gateway = Gateway,
                Reachable = reachable.Where(n => n != Gateway).ToList(),
                Unreachable = sensors.Where(s => !reachableSet.Contains(s.MacAddress)).Select(s => s.MacAddress).ToList(),
                TotalCost = mst.Sum(e => e.Weight)
            };

            result.Nodes.Add(new TopologyNode { Id = Gateway, Label = Gateway, IsUp = true });
            foreach (var sensor in sensors)
                result.Nodes.Add(new TopologyNode
                {
                    Id = sensor.MacAddress,
                    Label = sensor.SensorId,
                    IsUp = reachableSet.Contains(sensor.MacAddress)
                });

            foreach (var (from, to, weight) in graph.GetEdges())
                result.Links.Add(new TopologyLink { From = from, To = to, Weight = weight });

            foreach (var edge in mst)
                result.Mst.Add(new MstLink { Child = edge.Child, Parent = edge.Parent, Weight = edge.Weight });

            return result;
        }
    }

    public RouteResult? GetRoute(string mac)
    {
        lock (_lock)
        {
            if (!DeviceRegistry.TryGetValue(mac, out var device) || device.IsActuator)
                return null;

            var graph = BuildGraph(DisconnectedMacs());
            var mst = graph.Prim(Gateway);
            var hops = graph.RouteToGateway(mac, Gateway, mst);

            if (hops == null)
                return new RouteResult
                {
                    Mac = mac,
                    Reachable = false,
                    Message = $"{device.SensorId} has no route to the gateway."
                };

            return new RouteResult
            {
                Mac = mac,
                Reachable = true,
                Hops = hops,
                Message = $"{hops.Count - 1} hop(s) to the gateway."
            };
        }
    }

    public bool SetDisconnected(string mac, bool disconnected)
    {
        lock (_lock)
        {
            if (!DeviceRegistry.TryGetValue(mac, out var device) || device.IsActuator)
                return false;

            var key = ErrorKey(device.SensorId, DisconnectedType);

            if (disconnected)
            {
                if (ActiveErrors.Add(key))
                {
                    device.Status = "Offline";
                    CriticalAlertQueue.Enqueue(new Alert
                    {
                        SensorId = device.SensorId,
                        Zone = device.Zone,
                        Message = "Node disconnected from the mesh",
                        Severity = "Critical",
                        Timestamp = DateTime.Now
                    }, 0);
                    ProcessAll();
                }
            }
            else if (ActiveErrors.Remove(key))
            {
                device.Status = device.Battery < 20 ? "Warning" : "Online";
            }

            return true;
        }
    }

    private HashSet<string> DisconnectedMacs()
    {
        var down = new HashSet<string>();

        foreach (var key in ActiveErrors)
        {
            var (sensorId, type) = SplitKey(key);
            if (type == DisconnectedType && _macBySensorId.TryGetValue(sensorId, out var mac))
                down.Add(mac);
        }

        return down;
    }

    private void Record(string type, string text, bool countPattern)
    {
        var conditions = ActiveConditions();

        HistoryEvents.Add(new HistoryEvent
        {
            Type = type,
            Text = text,
            ActiveCondition = conditions.Count == 0 ? "none" : string.Join("; ", conditions),
            Timestamp = DateTime.Now
        });

        if (HistoryEvents.Count > HistoryCap)
            HistoryEvents.RemoveAt(0);

        if (!countPattern)
            return;

        foreach (var condition in conditions)
        {
            var pair = (condition, text);
            _pairCounts[pair] = _pairCounts.GetValueOrDefault(pair) + 1;
        }
    }

    private void SeedHistory(string condition, string type, string text, int times)
    {
        for (var i = 0; i < times; i++)
        {
            HistoryEvents.Add(new HistoryEvent
            {
                Type = type,
                Text = text,
                ActiveCondition = condition,
                Timestamp = DateTime.Now.AddDays(-(i + 1))
            });
        }

        var pair = (condition, text);
        _pairCounts[pair] = _pairCounts.GetValueOrDefault(pair) + times;
    }

    private List<string> ActiveConditions()
    {
        return ActiveErrors.OrderBy(k => k, StringComparer.Ordinal).Select(Describe).ToList();
    }

    private static string ErrorKey(string sensorId, string type) => $"{sensorId}:{type}";

    private static (string SensorId, string Type) SplitKey(string key)
    {
        var index = key.IndexOf(':');
        return index < 0 ? (key, "") : (key[..index], key[(index + 1)..]);
    }

    private static string Describe(string key)
    {
        var (sensorId, type) = SplitKey(key);

        return type switch
        {
            "LowMoisture" => $"Soil moisture below 30% ({sensorId})",
            "HighTemp" => $"Temperature above 35 C ({sensorId})",
            "LowBattery" => $"Battery below 20% ({sensorId})",
            "HighFlow" => $"Water flow above 25 L/min ({sensorId})",
            DisconnectedType => $"Node disconnected ({sensorId})",
            _ => key
        };
    }
}
