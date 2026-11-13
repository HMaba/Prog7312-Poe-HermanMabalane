using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SmartXWpf.ViewModels;

namespace SmartXWpf.Views;

public partial class TopologyView : Page
{
    private const double NodeSize = 34;

    private readonly TopologyViewModel _viewModel;

    public TopologyView(TopologyViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.Updated += Redraw;
        Loaded += (_, _) => Redraw();
    }

    private void MeshCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private async void Fail_Click(object sender, RoutedEventArgs e) => await _viewModel.FailNodeAsync();

    private async void Restore_Click(object sender, RoutedEventArgs e) => await _viewModel.RestoreNodeAsync();

    private async void HistorySearch_Click(object sender, RoutedEventArgs e) => await _viewModel.SearchHistoryAsync();

    private void Redraw()
    {
        MeshCanvas.Children.Clear();

        var topology = _viewModel.Topology;
        var width = MeshCanvas.ActualWidth;
        var height = MeshCanvas.ActualHeight;

        if (topology == null || width < 100 || height < 100)
            return;

        var centre = new Point(width / 2, height / 2);
        var radius = Math.Max(40, Math.Min(width, height) / 2 - 50);
        var positions = new Dictionary<string, Point>
        {
            [topology.Gateway] = centre
        };

        var sensors = topology.Nodes.Where(n => n.Id != topology.Gateway).ToList();
        for (var i = 0; i < sensors.Count; i++)
        {
            var angle = 2 * Math.PI * i / Math.Max(1, sensors.Count) - Math.PI / 2;
            positions[sensors[i].Id] = new Point(
                centre.X + radius * Math.Cos(angle),
                centre.Y + radius * Math.Sin(angle));
        }

        foreach (var link in topology.Links)
        {
            if (positions.TryGetValue(link.From, out var a) && positions.TryGetValue(link.To, out var b))
                AddLine(a, b, Brushes.LightGray, 1.5);
        }

        foreach (var link in topology.Mst)
        {
            if (!positions.TryGetValue(link.Child, out var a) || !positions.TryGetValue(link.Parent, out var b))
                continue;

            AddLine(a, b, Brushes.ForestGreen, 4);
            AddText(link.Weight.ToString(), new Point((a.X + b.X) / 2 + 4, (a.Y + b.Y) / 2 - 16), Brushes.DarkGreen, 11, FontWeights.Bold);
        }

        var hops = _viewModel.RouteHops;
        for (var i = 0; i < hops.Count - 1; i++)
        {
            if (positions.TryGetValue(hops[i], out var a) && positions.TryGetValue(hops[i + 1], out var b))
                AddLine(a, b, Brushes.DarkOrange, 7);
        }

        foreach (var node in topology.Nodes)
        {
            if (!positions.TryGetValue(node.Id, out var point))
                continue;

            var isGateway = node.Id == topology.Gateway;
            var isSelected = node.Id == _viewModel.SelectedMac;
            var size = isGateway ? NodeSize + 10 : NodeSize;

            var ellipse = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = isGateway ? Brushes.SteelBlue : node.IsUp ? Brushes.SeaGreen : Brushes.Crimson,
                Stroke = isSelected ? Brushes.DarkOrange : Brushes.White,
                StrokeThickness = isSelected ? 4 : 2
            };

            Canvas.SetLeft(ellipse, point.X - size / 2);
            Canvas.SetTop(ellipse, point.Y - size / 2);
            MeshCanvas.Children.Add(ellipse);

            AddText(isGateway ? "GW" : node.Label.Replace("SENSOR-", ""), new Point(point.X - size / 2 + (isGateway ? 9 : 5), point.Y - 8), Brushes.White, 12, FontWeights.Bold);
            AddText(node.Label, new Point(point.X - 32, point.Y + size / 2 + 2), Brushes.Black, 10, FontWeights.Normal);
        }
    }

    private void AddLine(Point from, Point to, Brush brush, double thickness)
    {
        MeshCanvas.Children.Add(new Line
        {
            X1 = from.X,
            Y1 = from.Y,
            X2 = to.X,
            Y2 = to.Y,
            Stroke = brush,
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        });
    }

    private void AddText(string text, Point at, Brush brush, double size, FontWeight weight)
    {
        var block = new TextBlock
        {
            Text = text,
            Foreground = brush,
            FontSize = size,
            FontWeight = weight
        };

        Canvas.SetLeft(block, at.X);
        Canvas.SetTop(block, at.Y);
        MeshCanvas.Children.Add(block);
    }
}
