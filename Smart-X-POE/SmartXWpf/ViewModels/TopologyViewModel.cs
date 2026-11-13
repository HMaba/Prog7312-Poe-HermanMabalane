using System.Collections.ObjectModel;
using System.Windows.Threading;
using SmartXWpf.Models;
using SmartXWpf.Services;

namespace SmartXWpf.ViewModels;

public class TopologyViewModel : ViewModelBase
{
    private const int MaxHistoryRows = 500;

    private readonly ApiService _api = new();
    private readonly DispatcherTimer _timer;
    private bool _refreshing;

    public event Action? Updated;

    public ObservableCollection<TopologyNode> SelectableNodes { get; } = new();
    public ObservableCollection<TelemetryRecord> HistoryRows { get; } = new();

    private TopologyResponse? _topology;
    public TopologyResponse? Topology
    {
        get => _topology;
        private set => SetProperty(ref _topology, value);
    }

    private List<string> _routeHops = new();
    public List<string> RouteHops
    {
        get => _routeHops;
        private set => SetProperty(ref _routeHops, value);
    }

    private string? _selectedMac;
    public string? SelectedMac
    {
        get => _selectedMac;
        set
        {
            if (SetProperty(ref _selectedMac, value))
                _ = LoadRouteAsync();
        }
    }

    private string _routeText = "Select a sensor to see its route to the gateway.";
    public string RouteText
    {
        get => _routeText;
        private set => SetProperty(ref _routeText, value);
    }

    private string _summaryText = "Waiting for topology data...";
    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private string _historyMessage = "Pick a sensor, set an optional time range and press Search.";
    public string HistoryMessage
    {
        get => _historyMessage;
        private set => SetProperty(ref _historyMessage, value);
    }

    private string _fromText = "";
    public string FromText
    {
        get => _fromText;
        set => SetProperty(ref _fromText, value);
    }

    private string _toText = "";
    public string ToText
    {
        get => _toText;
        set => SetProperty(ref _toText, value);
    }

    public TopologyViewModel()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };

        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();

        _ = RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (_refreshing)
            return;

        _refreshing = true;

        try
        {
            var topology = await _api.GetTopologyAsync();

            if (topology == null)
            {
                SummaryText = "The API is not reachable.";
                return;
            }

            Topology = topology;
            UpdateSelectableNodes(topology);

            var down = topology.Unreachable.Count;
            SummaryText = $"{topology.Reachable.Count} node(s) connected, {down} down. Total MST power cost: {topology.TotalCost}.";

            if (!string.IsNullOrEmpty(SelectedMac))
                await LoadRouteAsync();
            else
                Updated?.Invoke();
        }
        catch (Exception ex)
        {
            SummaryText = $"Refresh failed: {ex.Message}";
        }
        finally
        {
            _refreshing = false;
        }
    }

    public async Task FailNodeAsync()
    {
        await ChangeNodeAsync(true);
    }

    public async Task RestoreNodeAsync()
    {
        await ChangeNodeAsync(false);
    }

    public async Task SearchHistoryAsync()
    {
        if (string.IsNullOrEmpty(SelectedMac))
        {
            HistoryMessage = "Select a sensor first.";
            return;
        }

        DateTime? from = null;
        DateTime? to = null;

        if (!string.IsNullOrWhiteSpace(FromText))
        {
            if (!DateTime.TryParse(FromText, out var parsedFrom))
            {
                HistoryMessage = "The from time is not a valid date and time.";
                return;
            }

            from = parsedFrom;
        }

        if (!string.IsNullOrWhiteSpace(ToText))
        {
            if (!DateTime.TryParse(ToText, out var parsedTo))
            {
                HistoryMessage = "The to time is not a valid date and time.";
                return;
            }

            to = parsedTo;
        }

        try
        {
            var rows = await _api.GetHistoryAsync(SelectedMac, from, to);

            if (rows == null)
            {
                HistoryMessage = "History query failed. Check the time range and that the API is running.";
                return;
            }

            var shown = rows.AsEnumerable().Reverse().Take(MaxHistoryRows).ToList();
            HistoryRows.Clear();
            foreach (var row in shown)
                HistoryRows.Add(row);

            HistoryMessage = rows.Count > shown.Count
                ? $"{rows.Count} reading(s) found. Showing the newest {shown.Count}."
                : $"{rows.Count} reading(s) found.";
        }
        catch (Exception ex)
        {
            HistoryMessage = $"History query failed: {ex.Message}";
        }
    }

    private async Task ChangeNodeAsync(bool fail)
    {
        if (!AuthSession.IsSignedIn)
        {
            StatusMessage = "Sign in to fail or restore nodes.";
            return;
        }

        if (string.IsNullOrEmpty(SelectedMac))
        {
            StatusMessage = "Select a sensor first.";
            return;
        }

        try
        {
            var result = fail
                ? await _api.FailNodeAsync(SelectedMac)
                : await _api.RestoreNodeAsync(SelectedMac);

            StatusMessage = result?.Message ?? "The request failed. Is the API running?";
        }
        catch (Exception ex)
        {
            StatusMessage = $"The request failed: {ex.Message}";
        }

        await RefreshAsync();
    }

    private async Task LoadRouteAsync()
    {
        var mac = SelectedMac;

        if (string.IsNullOrEmpty(mac))
        {
            RouteHops = new List<string>();
            RouteText = "Select a sensor to see its route to the gateway.";
            Updated?.Invoke();
            return;
        }

        try
        {
            var route = await _api.GetRouteAsync(mac);

            if (mac != SelectedMac)
                return;

            if (route == null)
            {
                RouteHops = new List<string>();
                RouteText = "The route could not be loaded.";
            }
            else if (!route.Reachable)
            {
                RouteHops = new List<string>();
                RouteText = route.Message;
            }
            else
            {
                RouteHops = route.Hops;
                RouteText = "Route: " + string.Join(" > ", route.Hops.Select(LabelFor));
            }
        }
        catch (Exception ex)
        {
            RouteText = $"The route could not be loaded: {ex.Message}";
        }

        Updated?.Invoke();
    }

    private string LabelFor(string id)
    {
        var node = Topology?.Nodes.FirstOrDefault(n => n.Id == id);
        return node?.Label ?? id;
    }

    private void UpdateSelectableNodes(TopologyResponse topology)
    {
        var sensors = topology.Nodes.Where(n => n.Id != topology.Gateway).ToList();
        var sameIds = sensors.Select(n => n.Id).SequenceEqual(SelectableNodes.Select(n => n.Id));

        if (sameIds)
            return;

        var keep = _selectedMac;

        SelectableNodes.Clear();
        foreach (var node in sensors)
            SelectableNodes.Add(node);

        if (keep != null && sensors.Any(n => n.Id == keep))
            SelectedMac = keep;
    }
}
