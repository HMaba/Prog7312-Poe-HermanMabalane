namespace SmartAPI.Services;

public record Edge(string Neighbour, int Weight);

public record MstEdge(string Child, string Parent, int Weight);

public class MeshGraph
{
    private readonly Dictionary<string, List<Edge>> _adjacency = new();

    public IEnumerable<string> Nodes => _adjacency.Keys;

    public bool Contains(string node) => _adjacency.ContainsKey(node);

    public void AddNode(string node)
    {
        if (!_adjacency.ContainsKey(node))
            _adjacency[node] = new List<Edge>();
    }

    public void AddEdge(string a, string b, int weight)
    {
        if (a == b)
            return;

        AddNode(a);
        AddNode(b);

        if (_adjacency[a].Any(e => e.Neighbour == b))
            return;

        _adjacency[a].Add(new Edge(b, weight));
        _adjacency[b].Add(new Edge(a, weight));
    }

    public void RemoveNode(string node)
    {
        if (!_adjacency.Remove(node, out var edges))
            return;

        foreach (var edge in edges)
            _adjacency[edge.Neighbour].RemoveAll(e => e.Neighbour == node);
    }

    public List<(string From, string To, int Weight)> GetEdges()
    {
        var result = new List<(string From, string To, int Weight)>();

        foreach (var (node, edges) in _adjacency)
        {
            foreach (var edge in edges)
            {
                if (string.CompareOrdinal(node, edge.Neighbour) < 0)
                    result.Add((node, edge.Neighbour, edge.Weight));
            }
        }

        return result;
    }

    public List<string> Bfs(string start)
    {
        var order = new List<string>();
        if (!_adjacency.ContainsKey(start))
            return order;

        var visited = new HashSet<string> { start };
        var queue = new Queue<string>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            order.Add(current);

            foreach (var edge in _adjacency[current])
            {
                if (visited.Add(edge.Neighbour))
                    queue.Enqueue(edge.Neighbour);
            }
        }

        return order;
    }

    public List<MstEdge> Prim(string start)
    {
        var tree = new List<MstEdge>();
        if (!_adjacency.ContainsKey(start))
            return tree;

        var visited = new HashSet<string> { start };
        var queue = new PriorityQueue<MstEdge, int>();

        foreach (var edge in _adjacency[start])
            queue.Enqueue(new MstEdge(edge.Neighbour, start, edge.Weight), edge.Weight);

        while (queue.TryDequeue(out var candidate, out _))
        {
            if (!visited.Add(candidate.Child))
                continue;

            tree.Add(candidate);

            foreach (var edge in _adjacency[candidate.Child])
            {
                if (!visited.Contains(edge.Neighbour))
                    queue.Enqueue(new MstEdge(edge.Neighbour, candidate.Child, edge.Weight), edge.Weight);
            }
        }

        return tree;
    }

    public List<string>? RouteToGateway(string node, string gateway, IEnumerable<MstEdge> mst)
    {
        var parents = mst.ToDictionary(e => e.Child, e => e.Parent);
        var route = new List<string> { node };
        var current = node;

        while (current != gateway)
        {
            if (!parents.TryGetValue(current, out var parent))
                return null;

            route.Add(parent);
            current = parent;
        }

        return route;
    }
}
