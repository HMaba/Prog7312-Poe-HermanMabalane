namespace SmartXWpf.Models;

public class FacilityPower
{
    public double MainPower { get; set; }
    public double SolarPower { get; set; }
    public double BatteryLevel { get; set; }
    public double Consumption { get; set; }
    public bool GeneratorRunning { get; set; }
    public string Status { get; set; } = "";
}
