namespace SmartAPI.Models;

public class TelemetryPacket<T>
{
    public string SensorId { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public T Value { get; set; } = default!;
}
