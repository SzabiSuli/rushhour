namespace rushhour.src.Nodes;

using System;
using System.Collections.Generic;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.Board;
using rushhour.src.Nodes.Nodes3D;

public partial class MainScene : Control {
    [Export] public MainGameBoard gameBoard = null!;
    [Export] public GraphScene graphScene = null!;
    [Export] public TabContainer tabContainer = null!;
    public float algoStepDelay = 0.1f;
    public int selectedLevel = 3;

    public static MainScene Instance {get; private set;} = null!;

    double time = 0;

    Solver? solver;
    public override void _Ready(){
        Instance = this;

        RenderingServer.SetDefaultClearColor(Colors.Black);
    }

    public Solver GetNewSolver() {
        Solver s;
        // s = new TabuSolver(new MoverHeuristic(), 10, 1);
        // s = new BacktrackingSolver(new DistanceHeuristic());
        // s = new BacktrackingSolver(new FreeSpacesHeuristic());
        // s = new BacktrackingSolver(new MoverHeuristic());
        s = new AcGraphSolver(new MoverHeuristic());
        // s = new AcGraphSolver(new DistanceHeuristic());
        return s;
    }


    public void SubToSolver() {
        if (solver == null) {
            return;
        }
        
        solver.NewEdge += Edge.OnNewEdge;
        solver.PathChange += Edge.OnPathChange;
        solver.NewCurrent += Vertex.OnNewCurrent;
        solver.NewCurrent += gameBoard.OnNewAlgoCurrent;
    }

    public void UnSubFromSolver() {
        if (solver == null) {
            return;
        }

        solver.NewEdge -= Edge.OnNewEdge;
        solver.PathChange -= Edge.OnPathChange;
        solver.NewCurrent -= Vertex.OnNewCurrent;
        solver.NewCurrent -= gameBoard.OnNewAlgoCurrent;
    }



    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public async override void _Process(double delta) {
        if (solver == null) {
            return;
        }
        
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

    public void LoadLevel(string levelString, RHGameState level) {
        UnSubFromSolver();
        
        solver = GetNewSolver();
        SubToSolver();

        gameBoard.Setup(level);

        graphScene.Setup(level);

        solver.Start(level);
    
        // set the game board tab to be active
        tabContainer.CurrentTab = 0;
    }
}
