namespace rushhour.src.Nodes.UI;

using Godot;

public partial class TabCont : TabContainer {

    public SolverSettingsTab SolverSettingsTab => GetChild<SolverSettingsTab>(1);
    public AlgoPlayer AlgoPlayer => GetChild<VBoxContainer>(2).GetChild<AlgoPlayer>(0);

    public override void _Ready() {
        AlgoPlayer.solver = SolverSettingsTab.GetSolver();
    }
}
