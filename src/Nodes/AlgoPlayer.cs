namespace rushhour.src.Nodes;


using System;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.Board;
using rushhour.src.Nodes.Nodes3D;

public partial class AlgoPlayer : VBoxContainer {
    [Export] public HSlider slider = null!;
    public double algoStepDelay = 0.5;
    public double timeSinceLastStep = 0;

    public Solver solver = null!;

    public TabContainer TabCont => GetParent<VBoxContainer>().GetParent<TabContainer>();

    public static AlgoPlayer Instance {get; private set;} = null!;

    public AlgoPlayer() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of AlgoPlayer!");
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        slider.ValueChanged += OnSliderValueChanged;
    }


    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (solver.Status != SolverStatus.Running) {
            return;
        }
        timeSinceLastStep += delta;
        
        if (timeSinceLastStep < algoStepDelay) {	
            return;
        } 
        timeSinceLastStep = 0;
        
        solver.Step();
    }

    public void LoadLevel(RHGameState level) {
        UnSubFromSolver();
        
        solver = SolverSettingsTab.Instance.GetNewSolver();
        SubToSolver();

        MainGameBoard.Instance.Setup(level);

        GraphScene.Instance.Setup(level);

        solver.Start(level);

        // Switch to game board tab
        TabCont.CurrentTab = 0;
    }

    public void SubToSolver() {
        solver.NewEdge += Edge.OnNewEdge;
        solver.PathChange += Edge.OnPathChange;
        solver.NewCurrent += Vertex.OnNewCurrent;
        solver.NewCurrent += MainGameBoard.Instance.OnNewAlgoCurrent;
    }

    public void UnSubFromSolver() {
        solver.NewEdge -= Edge.OnNewEdge;
        solver.PathChange -= Edge.OnPathChange;
        solver.NewCurrent -= Vertex.OnNewCurrent;
        solver.NewCurrent -= MainGameBoard.Instance.OnNewAlgoCurrent;
    }

    public void OnSliderValueChanged(double value) {
        if (value == slider.MaxValue) {
            algoStepDelay = 0;
        } else {
            algoStepDelay = Math.Pow(2, -value);  
        }
    }
}
