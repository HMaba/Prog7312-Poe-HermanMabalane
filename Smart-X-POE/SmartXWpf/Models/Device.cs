namespace SmartXWpf.Models;

public class Device
{
    public int Id { get; set; }
    public string SensorId { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string Zone { get; set; } = "";
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";
    public double Battery { get; set; }
    public bool IsActuator { get; set; }
    public string State { get; set; } = "";
}
