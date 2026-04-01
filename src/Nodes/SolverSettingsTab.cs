using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes;
using System;

public partial class SolverSettingsTab : VBoxContainer {
    public static SolverSettingsTab Instance {get; private set;} = null!;

    public SolverSettingsTab() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of SolverSettingsTab!");
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        AlgoPlayer.Instance.solver = GetNewSolver();
    }

    public Solver GetNewSolver() {
        Solver s;
        // s = new TabuSolver(new MoverHeuristic(), 10, 1);
        // s = new BacktrackingSolver(new DistanceHeuristic());
        // s = new BacktrackingSolver(new FreeSpacesHeuristic());
        s = new BacktrackingSolver(new MoverHeuristic());
        // s = new AcGraphSolver(new MoverHeuristic());
        // s = new AcGraphSolver(new DistanceHeuristic());
        return s;
    }
}
