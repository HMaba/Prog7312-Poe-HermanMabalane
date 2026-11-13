namespace SmartAPI.Models;

public class Command
{
    public int DeviceId { get; set; }
    public string Action { get; set; } = "";
    public string PreviousState { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class CommandRequest
{
    public int DeviceId { get; set; }
    public string Action { get; set; } = "";
}

public class CommandResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int DeviceId { get; set; }
    public string State { get; set; } = "";
}
