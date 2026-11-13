namespace SmartAPI.Models;

public class HistoryEvent
{
    public string Type { get; set; } = "";
    public string Text { get; set; } = "";
    public string ActiveCondition { get; set; } = "none";
    public DateTime Timestamp { get; set; }
}
