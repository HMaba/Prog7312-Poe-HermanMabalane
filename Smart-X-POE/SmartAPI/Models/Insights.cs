namespace SmartAPI.Models;

public class Suggestion
{
    public string Action { get; set; } = "";
    public string Condition { get; set; } = "";
    public int Count { get; set; }
    public string Evidence { get; set; } = "";
}

public class ProactiveInsight
{
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "Warning";
}

public class InsightsResult
{
    public List<Suggestion> Suggestions { get; set; } = new();
    public List<ProactiveInsight> Proactive { get; set; } = new();
}
