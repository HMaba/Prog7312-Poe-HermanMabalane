using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartXWpf.Models;

namespace SmartXWpf.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public ApiService(string baseUrl = "http://localhost:5029/")
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    // Sends the request with the signed-in user's Firebase bearer token (if any).
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string endpoint, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, endpoint) { Content = content };

        var token = await AuthenticationService.GetValidTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _httpClient.SendAsync(request);
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            using var response = await SendAsync(HttpMethod.Get, endpoint);

            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object payload)
    {
        try
        {
            using var content = JsonContent.Create(payload, payload.GetType(), null, JsonOptions);
            using var response = await SendAsync(HttpMethod.Post, endpoint, content);

            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    public async Task<T?> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content)
    {
        try
        {
            using var response = await SendAsync(HttpMethod.Post, endpoint, content);

            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    public async Task<List<TelemetryRecord>?> GetTimelineAsync(int count = 40)
    {
        try
        {
            return await GetAsync<List<TelemetryRecord>>($"api/telemetry/timeline?count={count}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<Device>?> GetDevicesAsync()
    {
        try
        {
            return await GetAsync<List<Device>>("api/devices");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<Device>?> SearchDevicesAsync(string category, string alertState)
    {
        try
        {
            var url = $"api/devices/search?category={Uri.EscapeDataString(category)}&alertState={Uri.EscapeDataString(alertState)}";
            return await GetAsync<List<Device>>(url);
        }
        catch
        {
            return null;
        }
    }

    public async Task<CommandResult?> SendCommandAsync(int deviceId, string action)
    {
        try
        {
            return await PostAsync<CommandResult>("api/commands", new CommandRequest { DeviceId = deviceId, Action = action });
        }
        catch
        {
            return null;
        }
    }

    public async Task<CommandResult?> UndoCommandAsync()
    {
        try
        {
            return await PostAsync<CommandResult>("api/commands/undo", new object());
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<HistoryEvent>?> GetCommandHistoryAsync(int count = 30)
    {
        try
        {
            return await GetAsync<List<HistoryEvent>>($"api/commands/history?count={count}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<InsightsResult?> GetInsightsAsync()
    {
        try
        {
            return await GetAsync<InsightsResult>("api/insights");
        }
        catch
        {
            return null;
        }
    }

    public async Task<TopologyResponse?> GetTopologyAsync()
    {
        try
        {
            return await GetAsync<TopologyResponse>("api/topology");
        }
        catch
        {
            return null;
        }
    }

    public async Task<RouteResponse?> GetRouteAsync(string mac)
    {
        try
        {
            return await GetAsync<RouteResponse>($"api/topology/route/{Uri.EscapeDataString(mac)}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<TelemetryRecord>?> GetHistoryAsync(string mac, DateTime? from, DateTime? to)
    {
        try
        {
            var url = $"api/telemetry/{Uri.EscapeDataString(mac)}/history";
            var query = new List<string>();

            if (from.HasValue)
                query.Add($"from={Uri.EscapeDataString(from.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff"))}");
            if (to.HasValue)
                query.Add($"to={Uri.EscapeDataString(to.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff"))}");
            if (query.Count > 0)
                url += "?" + string.Join("&", query);

            return await GetAsync<List<TelemetryRecord>>(url);
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiMessage?> FailNodeAsync(string mac)
    {
        try
        {
            return await PostAsync<ApiMessage>($"api/topology/nodes/{Uri.EscapeDataString(mac)}/fail", new object());
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiMessage?> RestoreNodeAsync(string mac)
    {
        try
        {
            return await PostAsync<ApiMessage>($"api/topology/nodes/{Uri.EscapeDataString(mac)}/restore", new object());
        }
        catch
        {
            return null;
        }
    }
}
