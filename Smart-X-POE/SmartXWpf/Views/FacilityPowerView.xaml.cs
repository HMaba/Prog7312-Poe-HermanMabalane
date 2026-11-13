using System.Windows.Controls;
using System.Windows.Threading;
using SmartXWpf.Models;
using SmartXWpf.Services;

namespace SmartXWpf.Views;

public partial class FacilityPowerView : Page
{
    private readonly ApiService _api = new();
    private readonly DispatcherTimer _timer = new();

    public FacilityPowerView()
    {
        InitializeComponent();
        _timer.Interval = TimeSpan.FromSeconds(2);
        _timer.Tick += async (_, _) => await LoadAsync();
        _timer.Start();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var power = await _api.GetAsync<FacilityPower>("api/facilitypower");

        if (power == null)
        {
            Details.Text = "API unavailable.";
            return;
        }

        Details.Text =
            $"Main Power: {power.MainPower:F0}%\n" +
            $"Solar Power: {power.SolarPower:F0}%\n" +
            $"Battery: {power.BatteryLevel:F0}%\n" +
            $"Consumption: {power.Consumption:F1} kW\n" +
            $"Generator: {(power.GeneratorRunning ? "ON" : "OFF")}\n" +
            $"Status: {power.Status}";
    }
}
