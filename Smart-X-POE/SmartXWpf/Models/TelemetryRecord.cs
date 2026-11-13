namespace SmartXWpf.Models;

public class TelemetryRecord
{
    public string SensorId { get; set; } = "";
    public string Zone { get; set; } = "";
    public double SoilMoisture { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double WaterFlow { get; set; }
    public double Battery { get; set; }
    public DateTime Timestamp { get; set; }
}
