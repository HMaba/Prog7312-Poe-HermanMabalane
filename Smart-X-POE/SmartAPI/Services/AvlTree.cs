namespace SmartAPI.Services;

public class AvlTree<T>
{
    private sealed class Node
    {
        public DateTime Key;
        public T Value;
        public Node? Left;
        public Node? Right;
        public int Height = 1;

        public Node(DateTime key, T value)
        {
            Key = key;
            Value = value;
        }
    }

    private Node? _root;

    public int Count { get; private set; }

    public void Insert(DateTime key, T value)
    {
        _root = Insert(_root, key, value);
        Count++;
    }

    public List<T> RangeQuery(DateTime from, DateTime to)
    {
        var result = new List<T>();
        if (from > to)
            return result;

        Collect(_root, from, to, result);
        return result;
    }

    private static Node Insert(Node? node, DateTime key, T value)
    {
        if (node == null)
            return new Node(key, value);

        if (key < node.Key)
            node.Left = Insert(node.Left, key, value);
        else
            node.Right = Insert(node.Right, key, value);

        return Rebalance(node);
    }

    private static void Collect(Node? node, DateTime from, DateTime to, List<T> result)
    {
        if (node == null)
            return;

        if (from <= node.Key)
            Collect(node.Left, from, to, result);

        if (node.Key >= from && node.Key <= to)
            result.Add(node.Value);

        if (to >= node.Key)
            Collect(node.Right, from, to, result);
    }

    private static int HeightOf(Node? node) => node?.Height ?? 0;

    private static int BalanceOf(Node node) => HeightOf(node.Left) - HeightOf(node.Right);

    private static void UpdateHeight(Node node) =>
        node.Height = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));

    private static Node RotateRight(Node y)
    {
        var x = y.Left!;
        y.Left = x.Right;
        x.Right = y;
        UpdateHeight(y);
        UpdateHeight(x);
        return x;
    }

    private static Node RotateLeft(Node x)
    {
        var y = x.Right!;
        x.Right = y.Left;
        y.Left = x;
        UpdateHeight(x);
        UpdateHeight(y);
        return y;
    }

    private static Node Rebalance(Node node)
    {
        UpdateHeight(node);
        var balance = BalanceOf(node);

        if (balance > 1)
        {
            if (BalanceOf(node.Left!) < 0)
                node.Left = RotateLeft(node.Left!);
            return RotateRight(node);
        }

        if (balance < -1)
        {
            if (BalanceOf(node.Right!) > 0)
                node.Right = RotateRight(node.Right!);
            return RotateLeft(node);
        }

        return node;
    }
}
