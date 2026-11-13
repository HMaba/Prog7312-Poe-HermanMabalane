namespace SmartAPI.Services;

public class PowerSimulator : BackgroundService
{
    private readonly FarmStore _store;
    private readonly Random _random = new();

    public PowerSimulator(FarmStore store)
    {
        _store = store;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var power = _store.Power;

            power.MainPower = Math.Clamp(power.MainPower + _random.Next(-4, 3), 25, 100);
            power.SolarPower = Math.Clamp(power.SolarPower + _random.Next(-3, 4), 20, 95);
            power.Consumption = Math.Clamp(power.Consumption + (_random.NextDouble() - 0.5) * 2, 10, 30);
            power.BatteryLevel = Math.Clamp(power.BatteryLevel + (power.MainPower < 40 ? -1 : 0.3), 10, 100);
            power.GeneratorRunning = power.MainPower < 35 || power.BatteryLevel < 20;
            power.Status = power.MainPower < 35 || power.BatteryLevel < 20 ? "Warning" : "Normal";

            if (power.Status == "Warning")
                _store.AddAlert("FACILITY", "Main Facility", "Facility power is low", "Critical");

            await Task.Delay(3000, stoppingToken);
        }
    }
}
