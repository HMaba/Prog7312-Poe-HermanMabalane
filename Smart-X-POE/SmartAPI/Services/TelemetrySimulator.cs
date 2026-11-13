using SmartAPI.Models;
using SmartAPI.Services;
using SmartAPI.Models;

namespace SmartAPI.Services;

public class TelemetrySimulator : BackgroundService
{
    private readonly FarmStore _store;
    private readonly DataHub _hub;
    private readonly Random _random = new();

    public TelemetrySimulator(FarmStore store, DataHub hub)
    {
        _store = store;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pumpOn = _hub.IsPumpOn();

            foreach (var latest in _store.SnapshotTelemetry())
            {
                var next = new TelemetryRecord
                {
                    SensorId = latest.SensorId,
                    Zone = latest.Zone,
                    SoilMoisture = Math.Clamp(latest.SoilMoisture + (pumpOn ? _random.Next(1, 4) : _random.Next(-3, 2)), 15, 80),
                    Temperature = Math.Clamp(latest.Temperature + (_random.NextDouble() - 0.45), 15, 40),
                    Humidity = Math.Clamp(latest.Humidity + _random.Next(-2, 3), 30, 95),
                    WaterFlow = Math.Clamp(latest.WaterFlow + _random.NextDouble() - 0.5, 0, 30),
                    Battery = Math.Clamp(latest.Battery - 0.05, 5, 100),
                    Timestamp = DateTime.Now
                };

                _hub.Enqueue(new TelemetryPacket<TelemetryRecord>
                {
                    SensorId = next.SensorId,
                    Timestamp = next.Timestamp,
                    Value = next
                });
            }

            _hub.ProcessAll();

            await Task.Delay(2000, stoppingToken);
        }
    }
}
