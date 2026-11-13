using System.Collections.ObjectModel;
using System.Windows.Threading;
using SmartXWpf.Models;
using SmartXWpf.Services;

namespace SmartXWpf.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly ApiService _apiService = new();
    private readonly DispatcherTimer _timer;

    public ObservableCollection<TelemetryRecord> Telemetry { get; } = new();
    public ObservableCollection<Alert> Alerts { get; } = new();

    private FacilityPower _power = new();
    public FacilityPower Power
    {
        get => _power;
        private set => SetProperty(ref _power, value);
    }

    public DashboardViewModel()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };

        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();

        _ = RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        var telemetry = await _apiService.GetAsync<List<TelemetryRecord>>("api/telemetry");
        var alerts = await _apiService.GetAsync<List<Alert>>("api/alerts");
        var power = await _apiService.GetAsync<FacilityPower>("api/facilitypower");

        if (telemetry != null)
        {
            Telemetry.Clear();
            foreach (var item in telemetry)
                Telemetry.Add(item);
        }

        if (alerts != null)
        {
            Alerts.Clear();
            foreach (var item in alerts.Take(10))
                Alerts.Add(item);
        }

        if (power != null)
            Power = power;
    }
}
