using System.Windows;
using System.Windows.Controls;
using SmartXWpf.Services;
using SmartXWpf.ViewModels;

namespace SmartXWpf.Views;

public partial class DevicesView : Page
{
    private readonly DevicesViewModel _viewModel = new();

    public DevicesView()
    {
        InitializeComponent();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await _viewModel.LoadAsync();
        DeviceGrid.ItemsSource = _viewModel.Devices;
    }

    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        if (!AuthSession.IsSignedIn)
        {
            MessageBox.Show("Sign in to register sensors.");
            return;
        }

        if (string.IsNullOrWhiteSpace(MacBox.Text) ||
            string.IsNullOrWhiteSpace(ZoneBox.Text) ||
            string.IsNullOrWhiteSpace(CategoryBox.Text))
        {
            MessageBox.Show("Enter MAC address, zone and category.");
            return;
        }

        var success = await _viewModel.RegisterAsync(
            MacBox.Text,
            ZoneBox.Text,
            CategoryBox.Text);

        MessageBox.Show(success ? "Sensor registered." : "Registration failed.");

        if (success)
        {
            MacBox.Clear();
            ZoneBox.Clear();
            CategoryBox.Clear();
        }
    }
}
