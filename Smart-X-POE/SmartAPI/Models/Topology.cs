namespace SmartAPI.Models;

public class TopologyNode
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public bool IsUp { get; set; }
}

public class TopologyLink
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public int Weight { get; set; }
}

public class MstLink
{
    public string Child { get; set; } = "";
    public string Parent { get; set; } = "";
    public int Weight { get; set; }
}

public class TopologyResult
{
    public string Gateway { get; set; } = "";
    public List<TopologyNode> Nodes { get; set; } = new();
    public List<TopologyLink> Links { get; set; } = new();
    public List<MstLink> Mst { get; set; } = new();
    public List<string> Reachable { get; set; } = new();
    public List<string> Unreachable { get; set; } = new();
    public int TotalCost { get; set; }
}

public class RouteResult
{
    public string Mac { get; set; } = "";
    public bool Reachable { get; set; }
    public List<string> Hops { get; set; } = new();
    public string Message { get; set; } = "";
}
