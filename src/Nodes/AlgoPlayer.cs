namespace rushhour.src.Nodes;


using System;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.Board;
using rushhour.src.Nodes.Nodes3D;

public partial class AlgoPlayer : VBoxContainer {
    [Export] public HSlider slider = null!;
    [Export] public Button playPauseButton = null!;
    [Export] public Label playPauseLabel = null!;
    [Export] public Button stepButton = null!;
    [Export] public Button restartButton = null!;


    public double algoStepDelay = 0.5;
    public double timeSinceLastStep = 0;
    public bool running = false;

    public Solver solver = null!;

    public RHGameState? initialState;


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

        playPauseButton.Toggled += OnPlayPauseButtonToggled;
        stepButton.Pressed += OnStepButtonPressed;
        restartButton.Pressed += OnRestartButtonPressed;

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (solver.Status != SolverStatus.Running) return;
        if (!running) return;
        timeSinceLastStep += delta;
        
        if (timeSinceLastStep < algoStepDelay) return;
        timeSinceLastStep = 0;
        
        solver.Step();
    }

    public void LoadLevel(RHGameState level) {
        initialState = level;

        SetupControlButtons();

        UnSubFromSolver();
        
        solver = SolverSettingsTab.Instance.GetNewSolver();
        SubToSolver();

        MainGameBoard.Instance.Setup(level);

        GraphScene.Instance.Setup(level);

        solver.Start(level);

        // start paused
        running = false;

        // Switch to game board tab
        TabCont.CurrentTab = 0;
    }

    public void SetupControlButtons() {
        playPauseButton.Disabled = false;
        // set button to paused
        playPauseButton.ButtonPressed = false;
        playPauseLabel.Text = "Start";
        stepButton.Disabled = false;
        restartButton.Disabled = false;
    }

    public void SubToSolver() {
        solver.NewEdge += Edge.OnNewEdge;
        solver.PathChange += Edge.OnPathChange;
        solver.NewCurrent += Vertex.OnNewCurrent;
        solver.NewCurrent += MainGameBoard.Instance.OnNewAlgoCurrent;
        solver.Terminated += OnSolverTerminated;
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

    public void OnPlayPauseButtonToggled(bool playing) {
        running = playing;
        if (playing) {
            MainGameBoard.Instance.Mode = BoardMode.ALGO;
        }
    }

    public void OnStepButtonPressed() {
        solver.Step();
        MainGameBoard.Instance.Mode = BoardMode.ALGO;

        // set algo mode to paused
        playPauseButton.ButtonPressed = false;
    }

    public void OnRestartButtonPressed() {
        if (initialState is null) {
            throw new Exception("Can't restart with no level loaded");
        }

        // TODO update path highligh

        UnSubFromSolver();

        solver = SolverSettingsTab.Instance.GetSolver();
        SubToSolver();

        // TODO refine this

        MainGameBoard.Instance.AlgoCurrent = initialState;
        MainGameBoard.Instance.Mode = BoardMode.ALGO;

        solver.Start(initialState);

        SetupControlButtons();
    }

    public void OnSolverTerminated(object? sender, SolverStatus status) {
        playPauseButton.Disabled = true;
        stepButton.Disabled = true;
    }
}
