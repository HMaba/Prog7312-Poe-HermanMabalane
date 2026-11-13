using System.Collections.ObjectModel;
using System.Windows.Threading;
using SmartXWpf.Models;
using SmartXWpf.Services;

namespace SmartXWpf.ViewModels;

public class CommandStreamViewModel : ViewModelBase
{
    private readonly ApiService _api = new();
    private readonly DispatcherTimer _timer;
    private bool _refreshing;

    public ObservableCollection<TelemetryRecord> Timeline { get; } = new();
    public ObservableCollection<Device> Actuators { get; } = new();
    public ObservableCollection<Device> SearchResults { get; } = new();
    public ObservableCollection<HistoryEvent> HistoryEvents { get; } = new();
    public ObservableCollection<string> Suggestions { get; } = new();
    public ObservableCollection<string> Insights { get; } = new();

    public List<string> AlertStates { get; } = new()
    {
        "Any", "Alerting", "Normal", "LowMoisture", "HighTemp", "LowBattery", "HighFlow", "Disconnected"
    };

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _selectedAlertState = "Any";
    public string SelectedAlertState
    {
        get => _selectedAlertState;
        set => SetProperty(ref _selectedAlertState, value);
    }

    private string _statusMessage = "Connecting to the API...";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public CommandStreamViewModel()
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
        if (_refreshing)
            return;

        _refreshing = true;

        try
        {
            var timeline = await _api.GetTimelineAsync(40);
            var devices = await _api.GetDevicesAsync();
            var insights = await _api.GetInsightsAsync();
            var history = await _api.GetCommandHistoryAsync(30);

            if (timeline == null && devices == null)
            {
                StatusMessage = "The API is not reachable. Start the API and the view will recover on its own.";
                return;
            }

            if (timeline != null)
                Replace(Timeline, timeline.AsEnumerable().Reverse());

            if (devices != null)
                Replace(Actuators, devices.Where(d => d.IsActuator));

            if (history != null)
                Replace(HistoryEvents, history);

            if (insights != null)
            {
                Replace(Suggestions, insights.Suggestions.Select(s => s.Evidence));
                Replace(Insights, insights.Proactive.Select(p => $"[{p.Severity}] {p.Message}"));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Refresh failed: {ex.Message}";
        }
        finally
        {
            _refreshing = false;
        }
    }

    public Task PumpOnAsync() => SendAsync("Pump", "On");

    public Task PumpOffAsync() => SendAsync("Pump", "Off");

    public Task RelayToggleAsync() => SendAsync("Relay", "Toggle");

    public async Task UndoAsync()
    {
        if (!AuthSession.IsSignedIn)
        {
            StatusMessage = "Sign in to undo commands.";
            return;
        }

        try
        {
            var result = await _api.UndoCommandAsync();
            StatusMessage = result?.Message ?? "Undo failed. Is the API running?";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Undo failed: {ex.Message}";
        }

        await RefreshAsync();
    }

    public async Task SearchAsync()
    {
        try
        {
            var state = string.IsNullOrWhiteSpace(SelectedAlertState) ? "Any" : SelectedAlertState;
            var results = await _api.SearchDevicesAsync(SearchText?.Trim() ?? "", state);

            if (results == null)
            {
                StatusMessage = "Search failed. Is the API running?";
                return;
            }

            Replace(SearchResults, results);
            StatusMessage = $"{results.Count} device(s) found.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
        }

        await RefreshAsync();
    }

    private async Task SendAsync(string category, string action)
    {
        if (!AuthSession.IsSignedIn)
        {
            StatusMessage = "Sign in to send commands.";
            return;
        }

        var actuator = Actuators.FirstOrDefault(a => a.Category == category);
        if (actuator == null)
        {
            StatusMessage = $"No {category} actuator is available yet.";
            return;
        }

        try
        {
            var result = await _api.SendCommandAsync(actuator.Id, action);
            StatusMessage = result?.Message ?? "Command failed. Is the API running?";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Command failed: {ex.Message}";
        }

        await RefreshAsync();
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        var list = items.ToList();
        target.Clear();
        foreach (var item in list)
            target.Add(item);
    }
}
