namespace SmartAPI.Models;

public class PowerMeter
{
    public int Watts { get; set; }

    public static PowerMeter operator +(PowerMeter a, PowerMeter b)
    {
        return new PowerMeter { Watts = a.Watts + b.Watts };
    }
}
