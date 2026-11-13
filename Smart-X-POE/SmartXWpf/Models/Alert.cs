namespace SmartXWpf.Models;

public class Alert
{
    public int Id { get; set; }
    public string SensorId { get; set; } = "";
    public string Zone { get; set; } = "";
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
