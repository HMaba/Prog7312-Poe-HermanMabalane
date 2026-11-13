namespace SmartAPI.Models;

public class FacilityNode
{
    public string Name { get; set; } = "";
    public FacilityNode? Parent { get; set; }
    public List<FacilityNode> Children { get; set; } = new();

    public float[][] Batches { get; set; } = Array.Empty<float[]>();
    public static bool Validate(FacilityNode node)
    {
        if (node.Parent == null)
            return true;

        return Validate(node.Parent);
    }

    // Hard-coded demo tree: Facility A -> Zone 1 -> SubZone B -> Node -> Sensor
    // (5 levels). No database needed to prove the recursion works.
    public static FacilityNode BuildDemoTree()
    {
        var facility = new FacilityNode
        {
            Name = "Facility A",
            Batches = new[] { new float[] { 21.4f, 21.6f, 21.9f }, new float[] { 22.0f } }
        };

        var zone = new FacilityNode
        {
            Name = "Zone 1",
            Parent = facility,
            Batches = new[] { new float[] { 40.1f, 41.2f, 39.8f, 40.5f } }
        };

        var subZone = new FacilityNode
        {
            Name = "SubZone B",
            Parent = zone,
            Batches = new[] { new float[] { 5.5f, 5.6f }, new float[] { 5.7f, 5.9f, 6.0f } }
        };

        var node = new FacilityNode
        {
            Name = "Node NODE-A1",
            Parent = subZone,
            Batches = new[] { new float[] { 100.2f, 99.8f, 101.1f, 100.5f, 99.9f } }
        };

        var sensor = new FacilityNode
        {
            Name = "Sensor Soil-Moisture",
            Parent = node,
            Batches = new[] { new float[] { 62.1f, 61.9f, 62.4f } }
        };

        facility.Children.Add(zone);
        zone.Children.Add(subZone);
        subZone.Children.Add(node);
        node.Children.Add(sensor);

        return sensor; // return the deepest leaf so Validate has real climbing to do
    }
}
