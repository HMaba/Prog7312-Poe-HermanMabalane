using System.Windows.Controls;
using System.Windows.Threading;
using SmartXWpf.Models;
using SmartXWpf.Services;

namespace SmartXWpf.Views;

public partial class DashboardView : Page
{
    private readonly ApiService _api = new();
    private readonly DispatcherTimer _timer = new();

    public DashboardView()
    {
        InitializeComponent();

        _timer.Interval = TimeSpan.FromSeconds(2);
        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();

        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var devices = await _api.GetAsync<List<Device>>("api/devices");
        var telemetry = await _api.GetAsync<List<TelemetryRecord>>("api/telemetry");
        var alerts = await _api.GetAsync<List<Alert>>("api/alerts");
        var power = await _api.GetAsync<FacilityPower>("api/facilitypower");

        if (devices != null)
            SensorCount.Text = devices.Count.ToString();

        if (telemetry != null)
            TelemetryGrid.ItemsSource = telemetry;

        if (alerts != null)
        {
            AlertCount.Text = alerts.Count.ToString();
            AlertList.ItemsSource = alerts.Take(10)
                .Select(a => $"{a.Timestamp:HH:mm:ss} | {a.Severity} | {a.Zone} | {a.Message}")
                .ToList();
        }

        if (power != null)
        {
            PowerStatus.Text = power.Status;
            BatteryStatus.Text = $"{power.BatteryLevel:F0}%";
            PowerDetails.Text =
                $"Main: {power.MainPower:F0}%    Solar: {power.SolarPower:F0}%    " +
                $"Battery: {power.BatteryLevel:F0}%    Consumption: {power.Consumption:F1} kW    " +
                $"Generator: {(power.GeneratorRunning ? "ON" : "OFF")}";
        }
    }
}
