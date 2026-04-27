namespace rushhour.src.Nodes.UI;

using System;
using Godot;
using rushhour.src.Model;

public partial class SolverSettingsTab : VBoxContainer {
    [Export] public OptionButton algoOption = null!;
    [Export] public SpinBox tabuSizeSpin = null!;
    [Export] public OptionButton heuristicOption = null!;
    [Export] public CheckBox randomBox = null!;
    [Export] public Button applyButton = null!;
    [Export] public SpinBox searchCountBox = null!;
    [Export] public CheckButton unlimitedSearchButton = null!;
    [Export] public Button bfsSearchButton = null!;
    [Export] public Button dfsSearchButton = null!;

    
    private int _heuristicSelected;
    private float _randomFactor;
    private int _algoOptionSelected;
    private int _tabuSize;
    public int maxStatesToDiscover {get {
            if (unlimitedSearchButton.ButtonPressed) {
                return int.MaxValue;
            } else {
                return (int)searchCountBox.Value;
            }
        }
    }

    public TabCont TabCont => GetParent<TabCont>();

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
        algoOption.ItemSelected += OnAlgoOptionChanged;
        applyButton.Pressed += OnApplyButtonPressed;
        unlimitedSearchButton.Toggled += OnUnlimitedSearchButtonToggled;
        bfsSearchButton.Pressed += StartBfsSearcher;
        dfsSearchButton.Pressed += StartDfsSearcher;

        ApplySettings();
    }

    public void OnUnlimitedSearchButtonToggled(bool on) => searchCountBox.Editable = !on;
    public void StartBfsSearcher() {
        // assume a level is loaded
        AlgoPlayer.Instance.ResetSolver(new BFSDiscoverer(maxStatesToDiscover), true);
        Slider s = AlgoPlayer.Instance.slider;
        s.Value = s.MaxValue - 1; 
    }
    public void StartDfsSearcher() {
        // assume a level is loaded
        AlgoPlayer.Instance.ResetSolver(new DFSDiscoverer(maxStatesToDiscover), true);
        Slider s = AlgoPlayer.Instance.slider;
        s.Value = s.MaxValue - 1; 
    }

    public void ApplySettings() {
        _heuristicSelected = heuristicOption.Selected;
        _randomFactor = randomBox.ButtonPressed ? 1f : 0f;
        _algoOptionSelected = algoOption.Selected;
        _tabuSize = (int)tabuSizeSpin.Value;
    }

    public void OnApplyButtonPressed() {
        ApplySettings();
        if (AlgoPlayer.Instance.InitialState == null) {
            // if no level has been selected, 
            // switch to the levels tab, so the user selects a level
            TabCont.CurrentTab = 0;
        } else {
            AlgoPlayer.Instance.ResetSolver();
        }
    }

    public void OnAlgoOptionChanged(long index) {
        tabuSizeSpin.GetParent<HBoxContainer>().Visible = index == 0;
    }

    public RHSolver GetSolver() {
        Heuristic<RHGameState> h = heuristicOption.Selected switch {
            0 => new NullHeuristic(),
            1 => new DistanceHeuristic(),
            2 => new FreeSpacesHeuristic(),
            3 => new MoverHeuristic(),
            _ => throw new Exception("Invalid heuristic option selected")
        };
        RHSolver s;
        switch (algoOption.Selected) {
            case 0:             
                s = new TabuSolver(h, _tabuSize, _randomFactor);
                break;
            case 1:             
                s = new BacktrackingSolver(h, _randomFactor);
                break;
            case 2:
                if (h is not MonotoneHeuristic<RHGameState> mh) {
                    throw new ArgumentException("Monotone heuristic must be selected for AcGraphSolver!");
                }
                s = new AcGraphSolver(mh, _randomFactor);
                break;
            default:
                throw new Exception("Invalid algorithm option selected");
        }
        return s;
    }
}
