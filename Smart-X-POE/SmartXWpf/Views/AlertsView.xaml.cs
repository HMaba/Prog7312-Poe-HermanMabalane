using System.Windows.Controls;
using System.Windows.Threading;
using SmartXWpf.Services;

namespace SmartXWpf.Views;

public partial class AlertsView : Page
{
    private readonly ApiService _api = new();
    private readonly DispatcherTimer _timer = new();

    public AlertsView()
    {
        InitializeComponent();
        _timer.Interval = TimeSpan.FromSeconds(2);
        _timer.Tick += async (_, _) => await LoadAsync();
        _timer.Start();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Grid.ItemsSource = await _api.GetAsync<List<object>>("api/alerts");
    }
}
