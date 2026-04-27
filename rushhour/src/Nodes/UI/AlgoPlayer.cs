namespace rushhour.src.Nodes.UI;

using System;
using System.Linq;
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


    public const double minAlgoStepdelay = 0.001; 
    public const int maxStepCount = 1024; 
    private double _algoStepDelay = 0.5;
    private double _timeSinceLastStep = 0;
    public bool Running { get; private set; } = false;

    public RHSolver Solver { get; set; } = null!;

    public RHGameState? InitialState { get; private set; }


    public TabCont TabCont => GetParent<VBoxContainer>().GetParent<TabCont>();

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
        restartButton.Pressed += ResetSolver;

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (Solver.Status != SolverStatus.Running && Solver.Status != SolverStatus.Discovering) return;
        if (!Running) return;
        _timeSinceLastStep += delta;
        
        if (_timeSinceLastStep < _algoStepDelay) return;
        
        // If time between updates gets too large, execute some extra steps 
        int stepCount = Math.Clamp((int)(_timeSinceLastStep / _algoStepDelay), 1, maxStepCount);
        
        _timeSinceLastStep = 0;

        Solver.Step(stepCount);
    }

    public void LoadLevel(RHGameState level) {
        InitialState = level;

        SetupControlButtons(false);

        UnSubFromSolver();
        
        Solver = SolverSettingsTab.Instance.GetSolver();
        SubToSolver();

        MainGameBoard.Instance.Setup(level);

        GraphScene.Instance.Setup(level);

        Solver.Start(level);

        StatusContainer.Instance.SetSolutionLength(null);

        // start paused
        Running = false;

        // Switch to solver settings tab
        TabCont.CurrentTab = 1;
    }

    public void SetupControlButtons(bool startPlaying) {
        playPauseButton.Disabled = false;
        stepButton.Disabled = false;
        restartButton.Disabled = false;
        
        playPauseButton.ButtonPressed = startPlaying;
        if (!startPlaying) {
            playPauseLabel.Text = "Start";
        }

        SolverSettingsTab.Instance.bfsSearchButton.Disabled = false;
        SolverSettingsTab.Instance.dfsSearchButton.Disabled = false;
    }

    public void SubToSolver() {
        Solver.NewEdge += Edge.OnNewEdge;
        Solver.PathChange += Edge.OnPathChange;
        Solver.PathChange += Vertex.OnPathChange;
        Solver.NewCurrent += Vertex.OnNewCurrent;
        Solver.NewCurrent += MainGameBoard.Instance.OnNewAlgoCurrent;
        Solver.NewCurrent += OnNewCurrent;
        Solver.Terminated += OnSolverTerminated;
    }


    public void UnSubFromSolver() {
        Solver.NewEdge -= Edge.OnNewEdge;
        Solver.PathChange -= Edge.OnPathChange;
        Solver.PathChange -= Vertex.OnPathChange;
        Solver.NewCurrent -= Vertex.OnNewCurrent;
        Solver.NewCurrent -= MainGameBoard.Instance.OnNewAlgoCurrent;
        Solver.NewCurrent -= OnNewCurrent;
        Solver.Terminated -= OnSolverTerminated;
    }
    public void OnNewCurrent(object? s, RHGameState _) {
        int c = Solver.StepCount;
        StatusContainer.Instance.SetStepCount(c);
        // The solver enters Running status after calling Start on it, so bypass this by checking the step count.
        StatusContainer.Instance.SetStatusLabel(Solver.Status, c);
    }
    public void OnSliderValueChanged(double value) {
        if (value == slider.MaxValue) {
            _algoStepDelay = minAlgoStepdelay;
        } else {
            _algoStepDelay = Math.Pow(2, -value);  
        }
    }

    public void OnPlayPauseButtonToggled(bool playing) {
        Running = playing;
        if (playing) {
            MainGameBoard.Instance.Mode = BoardMode.ALGO;
        }
    }

    public void OnStepButtonPressed() {
        Solver.Step();
        MainGameBoard.Instance.Mode = BoardMode.ALGO;

        // set algo mode to paused
        playPauseButton.ButtonPressed = false;
    }

    public void ResetSolver() => ResetSolver(SolverSettingsTab.Instance.GetSolver());

    public void ResetSolver(RHSolver newSolver, bool startPlaying = false) {
        if (InitialState is null) {
            throw new Exception("Can't restart with no level loaded");
        }

        // TODO maybe filter by active edges,
        // add a field to those.
        GraphScene.Instance.ClearPathHighligh();

        UnSubFromSolver();

        Solver = newSolver;
        SubToSolver();

        StatusContainer.Instance.SetSolutionLength(null);

        MainGameBoard.Instance.AlgoCurrent = InitialState;
        MainGameBoard.Instance.Mode = BoardMode.ALGO;

        Solver.Start(InitialState);

        SetupControlButtons(startPlaying);

        // Switch to the AlgoPlayer tab
        TabCont.CurrentTab = 2;
    }

    public void OnSolverTerminated(object? sender, SolverStatus status) {
        playPauseButton.Disabled = true;
        stepButton.Disabled = true;
        StatusContainer.Instance.SetStatusLabel(status);

        if (status == SolverStatus.Solved) {
            var solutionPath = Solver.GetSolutionPath();

            StatusContainer.Instance.SetSolutionLength(solutionPath.Count());

            foreach (StateMove stateMove in solutionPath) {
                Edge.Dict[stateMove].AddEffect(EdgeEffect.SolutionEdge);
            }
        }
    }
}
