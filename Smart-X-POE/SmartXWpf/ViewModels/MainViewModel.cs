namespace SmartXWpf.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly CommandStreamViewModel _commandStream = new();
    private readonly TopologyViewModel _topology = new();

    public CommandStreamViewModel CommandStream => _commandStream;
    public TopologyViewModel Topology => _topology;
}
