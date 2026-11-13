using Microsoft.Win32;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using SmartXWpf.Services;
using SmartXWpf.ViewModels;
using System.IO;

namespace SmartXWpf.Views;

public partial class MainWindow : Window
{
    private readonly ApiService _api = new();
    private readonly MainViewModel _mainViewModel = new();

    private DashboardView? _dashboardView;
    private DevicesView? _devicesView;
    private TelemetryView? _telemetryView;
    private AlertsView? _alertsView;
    private FacilityPowerView? _powerView;
    private CommandStreamView? _commandStreamView;
    private TopologyView? _topologyView;
    private Page? _currentSensorPage;

    public MainWindow()
    {
        InitializeComponent();
        ShowSensorPage(_dashboardView ??= new DashboardView());
        CheckApi();
    }

    private void ShowSensorPage(Page page)
    {
        _currentSensorPage = page;
        SensorSubMenu.Visibility = Visibility.Visible;
        ContentFrame.Navigate(page);
    }

    private void SensorModule_Click(object sender, RoutedEventArgs e) =>
        ShowSensorPage(_currentSensorPage ?? (_dashboardView ??= new DashboardView()));

    private void CommandModule_Click(object sender, RoutedEventArgs e)
    {
        SensorSubMenu.Visibility = Visibility.Collapsed;
        ContentFrame.Navigate(_commandStreamView ??= new CommandStreamView(_mainViewModel.CommandStream));
    }

    private void TopologyModule_Click(object sender, RoutedEventArgs e)
    {
        SensorSubMenu.Visibility = Visibility.Collapsed;
        ContentFrame.Navigate(_topologyView ??= new TopologyView(_mainViewModel.Topology));
    }

    private async void CheckApi()
    {
        var devices = await _api.GetAsync<object>("api/devices");
        ApiStatus.Text = devices == null ? "API OFFLINE" : "API ONLINE";
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e) => ShowSensorPage(_dashboardView ??= new DashboardView());
    private void Sensors_Click(object sender, RoutedEventArgs e) => ShowSensorPage(_devicesView ??= new DevicesView());
    private void Telemetry_Click(object sender, RoutedEventArgs e) => ShowSensorPage(_telemetryView ??= new TelemetryView());
    private void Alerts_Click(object sender, RoutedEventArgs e) => ShowSensorPage(_alertsView ??= new AlertsView());
    private void Power_Click(object sender, RoutedEventArgs e) => ShowSensorPage(_powerView ??= new FacilityPowerView());

    private async void Upload_Click(object sender, RoutedEventArgs e)
    {
        if (!AuthSession.IsSignedIn)
        {
            MessageBox.Show("Sign in to upload files.");
            return;
        }

        var dialog = new OpenFileDialog();
        if (dialog.ShowDialog() != true)
            return;

        using var content = new MultipartFormDataContent();
        await using var stream = File.OpenRead(dialog.FileName);

        content.Add(new StreamContent(stream), "file", Path.GetFileName(dialog.FileName));

        var result = await _api.PostMultipartAsync<object>("api/files/upload", content);

        MessageBox.Show(result != null ? "File uploaded." : "Upload failed.");
    }
}
