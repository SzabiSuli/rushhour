namespace rushhour.src.Nodes;


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
    public double algoStepDelay = 0.5;
    public double timeSinceLastStep = 0;
    public bool running = false;

    public Solver solver = null!;

    public RHGameState? initialState;


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
        if (solver.Status != SolverStatus.Running && solver.Status != SolverStatus.Discovering) return;
        if (!running) return;
        timeSinceLastStep += delta;
        
        if (timeSinceLastStep < algoStepDelay) return;
        
        // If time between updates gets too large, execute some extra steps 
        int stepCount = Math.Clamp((int)(timeSinceLastStep / algoStepDelay), 1, maxStepCount);
        
        timeSinceLastStep = 0;

        solver.Step(stepCount);
    }

    public void LoadLevel(RHGameState level) {
        initialState = level;

        SetupControlButtons(false);

        UnSubFromSolver();
        
        solver = SolverSettingsTab.Instance.GetSolver();
        SubToSolver();

        MainGameBoard.Instance.Setup(level);

        GraphScene.Instance.Setup(level);

        solver.Start(level);

        StatusContainer.Instance.SetSolutionLength(null);

        // start paused
        running = false;

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
        solver.NewEdge += Edge.OnNewEdge;
        solver.PathChange += Edge.OnPathChange;
        solver.PathChange += Vertex.OnPathChange;
        solver.NewCurrent += Vertex.OnNewCurrent;
        solver.NewCurrent += MainGameBoard.Instance.OnNewAlgoCurrent;
        solver.NewCurrent += OnNewCurrent;
        solver.Terminated += OnSolverTerminated;
    }


    public void UnSubFromSolver() {
        solver.NewEdge -= Edge.OnNewEdge;
        solver.PathChange -= Edge.OnPathChange;
        solver.PathChange -= Vertex.OnPathChange;
        solver.NewCurrent -= Vertex.OnNewCurrent;
        solver.NewCurrent -= MainGameBoard.Instance.OnNewAlgoCurrent;
        solver.NewCurrent -= OnNewCurrent;
        solver.Terminated -= OnSolverTerminated;
    }
    public void OnNewCurrent(object? s, RHGameState _) {
        int c = solver.StepCount;
        StatusContainer.Instance.SetStepCount(c);
        // The solver enters Running status after calling Start on it, so bypass this by checking the step count.
        StatusContainer.Instance.SetStatusLabel(solver.Status, c);
    }
    public void OnSliderValueChanged(double value) {
        if (value == slider.MaxValue) {
            algoStepDelay = minAlgoStepdelay;
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

    public void ResetSolver() => ResetSolver(SolverSettingsTab.Instance.GetSolver());

    public void ResetSolver(Solver newSolver, bool startPlaying = false) {
        if (initialState is null) {
            throw new Exception("Can't restart with no level loaded");
        }

        // TODO maybe filter by active edges,
        // add a field to those.
        GraphScene.Instance.ClearPathHighligh();

        UnSubFromSolver();

        solver = newSolver;
        SubToSolver();

        StatusContainer.Instance.SetSolutionLength(null);

        MainGameBoard.Instance.AlgoCurrent = initialState;
        MainGameBoard.Instance.Mode = BoardMode.ALGO;

        solver.Start(initialState);

        SetupControlButtons(startPlaying);

        // Switch to the AlgoPlayer tab
        TabCont.CurrentTab = 2;
    }

    public void OnSolverTerminated(object? sender, SolverStatus status) {
        playPauseButton.Disabled = true;
        stepButton.Disabled = true;
        StatusContainer.Instance.SetStatusLabel(status);

        if (status == SolverStatus.Solved) {
            var solutionPath = solver.GetSolutionPath();

            StatusContainer.Instance.SetSolutionLength(solutionPath.Count());

            foreach (StateMove stateMove in solutionPath) {
                Edge.Dict[stateMove].AddEffect(EdgeEffect.SolutionEdge);
            }
        }
    }
}
