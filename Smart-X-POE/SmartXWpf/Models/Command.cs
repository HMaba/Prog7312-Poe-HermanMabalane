namespace SmartXWpf.Models;

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

public class HistoryEvent
{
    public string Type { get; set; } = "";
    public string Text { get; set; } = "";
    public string ActiveCondition { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
