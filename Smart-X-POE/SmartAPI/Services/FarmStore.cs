using SmartAPI.Models;
using SmartAPI.Models;

namespace SmartAPI.Services;

public class FarmStore
{
    private readonly object _lock = new();

    public List<Device> Devices { get; } =
    [
        new Device { Id = 1, SensorId = "SENSOR-001", MacAddress = "AA:BB:CC:00:00:01", Zone = "Field A", Category = "Soil Moisture", Status = "Online", Battery = 91 },
        new Device { Id = 2, SensorId = "SENSOR-002", MacAddress = "AA:BB:CC:00:00:02", Zone = "Field B", Category = "Soil Moisture", Status = "Online", Battery = 84 },
        new Device { Id = 3, SensorId = "SENSOR-003", MacAddress = "AA:BB:CC:00:00:03", Zone = "Field C", Category = "Temperature", Status = "Online", Battery = 76 },
        new Device { Id = 4, SensorId = "SENSOR-004", MacAddress = "AA:BB:CC:00:00:04", Zone = "Greenhouse", Category = "Humidity", Status = "Online", Battery = 68 },
        new Device { Id = 5, SensorId = "SENSOR-005", MacAddress = "AA:BB:CC:00:00:05", Zone = "Field A", Category = "Water Flow", Status = "Online", Battery = 95 },
        new Device { Id = 6, SensorId = "SENSOR-006", MacAddress = "AA:BB:CC:00:00:06", Zone = "Field D", Category = "Temperature", Status = "Online", Battery = 58 },
        new Device { Id = 7, SensorId = "SENSOR-007", MacAddress = "AA:BB:CC:00:00:07", Zone = "Utility Room", Category = "Power Meter", Status = "Online", Battery = 100 },
        new Device { Id = 8, SensorId = "SENSOR-008", MacAddress = "AA:BB:CC:00:00:08", Zone = "Greenhouse", Category = "Soil Moisture", Status = "Warning", Battery = 19 },
        new Device { Id = 9, SensorId = "Pump A", MacAddress = "AA:BB:CC:00:01:01", Zone = "Field B", Category = "Pump", Status = "Online", Battery = 100, IsActuator = true, State = "Off" },
        new Device { Id = 10, SensorId = "Relay 3", MacAddress = "AA:BB:CC:00:01:02", Zone = "Utility Room", Category = "Relay", Status = "Online", Battery = 100, IsActuator = true, State = "Off" }
    ];

    public List<TelemetryRecord> Telemetry { get; } =
    [
        new TelemetryRecord { SensorId = "SENSOR-001", Zone = "Field A", SoilMoisture = 44, Temperature = 25, Humidity = 62, WaterFlow = 12, Battery = 91, Timestamp = DateTime.Now },
        new TelemetryRecord { SensorId = "SENSOR-002", Zone = "Field B", SoilMoisture = 34, Temperature = 28, Humidity = 57, WaterFlow = 10, Battery = 84, Timestamp = DateTime.Now },
        new TelemetryRecord { SensorId = "SENSOR-003", Zone = "Field C", SoilMoisture = 61, Temperature = 23, Humidity = 71, WaterFlow = 8, Battery = 76, Timestamp = DateTime.Now },
        new TelemetryRecord { SensorId = "SENSOR-004", Zone = "Greenhouse", SoilMoisture = 48, Temperature = 27, Humidity = 74, WaterFlow = 6, Battery = 68, Timestamp = DateTime.Now },
        new TelemetryRecord { SensorId = "SENSOR-005", Zone = "Field A", SoilMoisture = 52, Temperature = 24, Humidity = 65, WaterFlow = 14, Battery = 95, Timestamp = DateTime.Now },
        new TelemetryRecord { SensorId = "SENSOR-006", Zone = "Field D", SoilMoisture = 39, Temperature = 31, Humidity = 55, WaterFlow = 9, Battery = 58, Timestamp = DateTime.Now },
        new TelemetryRecord { SensorId = "SENSOR-007", Zone = "Utility Room", SoilMoisture = 0, Temperature = 22, Humidity = 40, WaterFlow = 0, Battery = 100, Timestamp = DateTime.Now },
        new TelemetryRecord { SensorId = "SENSOR-008", Zone = "Greenhouse", SoilMoisture = 28, Temperature = 29, Humidity = 78, WaterFlow = 11, Battery = 19, Timestamp = DateTime.Now }
    ];

    public List<Alert> Alerts { get; } = new();

    public FacilityPower Power { get; set; } = new()
    {
        MainPower = 88,
        SolarPower = 64,
        BatteryLevel = 78,
        Consumption = 18.5,
        GeneratorRunning = false,
        Status = "Normal"
    };

    public List<TelemetryRecord> SnapshotTelemetry()
    {
        lock (_lock)
        {
            return Telemetry.ToList();
        }
    }

    public void UpdateTelemetry(TelemetryRecord record)
    {
        lock (_lock)
        {
            var index = Telemetry.FindIndex(t => t.SensorId == record.SensorId);
            if (index >= 0)
                Telemetry[index] = record;
            else
                Telemetry.Add(record);
        }
    }

    public int AddDevice(Device device)
    {
        lock (_lock)
        {
            device.Id = Devices.Count == 0 ? 1 : Devices.Max(x => x.Id) + 1;
            device.SensorId = $"SENSOR-{device.Id:000}";
            device.Status = "Online";
            device.Battery = 100;
            Devices.Add(device);

            Telemetry.Add(new TelemetryRecord
            {
                SensorId = device.SensorId,
                Zone = device.Zone,
                SoilMoisture = 45,
                Temperature = 24,
                Humidity = 60,
                WaterFlow = 5,
                Battery = 100,
                Timestamp = DateTime.Now
            });

            return device.Id;
        }
    }

    public void AddAlert(string sensorId, string zone, string message, string severity)
    {
        lock (_lock)
        {
            if (Alerts.Any(a => a.SensorId == sensorId && a.Message == message && (DateTime.Now - a.Timestamp).TotalSeconds < 10))
                return;

            Alerts.Insert(0, new Alert
            {
                Id = Alerts.Count + 1,
                SensorId = sensorId,
                Zone = zone,
                Message = message,
                Severity = severity,
                Timestamp = DateTime.Now
            });

            if (Alerts.Count > 20)
                Alerts.RemoveAt(Alerts.Count - 1);
        }
    }
}
