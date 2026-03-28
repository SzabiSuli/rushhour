namespace rushhour.src.Nodes;

using System;
using System.Collections.Generic;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.Board;
using rushhour.src.Nodes.Nodes3D;

public partial class MainScene : Control {
    [Export] public MainGameBoard gameBoard = null!;
    [Export] public Node3D GraphScene = null!;
    public float algoStepDelay = 0.1f;
    public int selectedLevel = 3;

    public static MainScene Instance {get; private set;} = null!;

    Random random = new Random();

    double time = 0;

    Solver solver = null!;
    public override void _Ready(){
        Instance = this;

        RenderingServer.SetDefaultClearColor(Colors.Black);

        var (title, lvl) = Levels.LoadLevel(selectedLevel);

        GD.Print(title);
        lvl.PrintState();


        solver = new TabuSolver(new MoverHeuristic(), 10, 1);
        // solver = new BacktrackingSolver(new DistanceHeuristic());
        // solver = new BacktrackingSolver(new FreeSpacesHeuristic());
        // solver = new BacktrackingSolver(new MoverHeuristic());
        // solver = new AcGraphSolver(new MoverHeuristic());
        // solver = new AcGraphSolver(new DistanceHeuristic());

        solver.NewEdge += Edge.OnNewEdge;
        solver.PathChange += Edge.OnPathChange;
        solver.NewCurrent += Vertex.OnNewCurrent;
        solver.NewCurrent += gameBoard.OnNewAlgoCurrent;

        gameBoard.Setup(lvl);
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
