namespace rushhour.src.Nodes;

using rushhour.src.Model;
using System;
using System.Collections.Generic;
using Godot;

public partial class MainScene : Control {
    [Export] public GameBoard gameBoard = null!;
    [Export] public Node3D GraphScene = null!;
    [Export] public float algoStepDelay = 1;
    [Export] public int selectedLevel = 1;

    public static MainScene Instance {get; private set;} = null!;

    Random random = new Random();

    double time = 0;

    Solver solver = null!;
    public override void _Ready(){
        Instance = this;

        string version = System.Environment.Version.ToString();
        GD.Print("🚀 C# is working!");
        GD.Print($"System .NET Version: {version}");
        
        // Let's also change the background color to prove it's running
        RenderingServer.SetDefaultClearColor(Colors.Black);

        var (title, lvl) = Levels.LoadLevel(selectedLevel);

        GD.Print(title);
        lvl.PrintState();

        // solver = new BacktrackingSolver(new DistanceHeuristic());
        // solver = new BacktrackingSolver(new FreeSpacesHeuristic());
        solver = new BacktrackingSolver(new MoverHeuristic());
        // solver = new AcGraphSolver(new MoverHeuristic());
        // solver = new AcGraphSolver(new DistanceHeuristic());

        solver.NewEdge += Edge.OnNewEdge;
        solver.PathChange += Edge.OnPathChange;
        solver.DiscoveredEdges += Edge.OnDiscoveredEdges;
        solver.NewCurrent += Vertex.OnNewCurrent;
        solver.NewCurrent += gameBoard.DisplayState;

        // We have to create the first vertex
        Vertex.GetOrCreate(lvl, null);

        solver.Start(lvl);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public async override void _Process(double delta) {
        time += delta;
        if (time < algoStepDelay) {	
            return;
        } else {
            time = 0;
        }
        if (solver.Status == SolverStatus.Running){
            // solver.Current?.PrintState();
            solver.Step();
        }
    }

    public async override void _PhysicsProcess(double delta) {
        // Rebuild the Barnes-Hut OctTree once per physics update for repulsion forces
        var vertexNodes = GetTree().GetNodesInGroup("Vertices");
        var vertexList = new List<Vertex>();
        foreach (var v in vertexNodes) vertexList.Add((Vertex)v);
        OctTree.BuildAndSetCurrent(vertexList);
    }
}
