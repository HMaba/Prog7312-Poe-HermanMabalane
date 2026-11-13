using System.Collections.ObjectModel;
using SmartXWpf.Models;
using SmartXWpf.Services;

namespace SmartXWpf.ViewModels;

public class DevicesViewModel : ViewModelBase
{
    private readonly ApiService _apiService = new();

    public ObservableCollection<Device> Devices { get; } = new();

    public async Task LoadAsync()
    {
        var devices = await _apiService.GetAsync<List<Device>>("api/devices");

        if (devices == null)
            return;

        Devices.Clear();
        foreach (var device in devices)
            Devices.Add(device);
    }

    public async Task<bool> RegisterAsync(string macAddress, string zone, string category)
    {
        var device = await _apiService.PostAsync<Device>(
            "api/devices",
            new Device
            {
                MacAddress = macAddress,
                Zone = zone,
                Category = category
            });

        if (device == null)
            return false;

        await LoadAsync();
        return true;
    }
}
