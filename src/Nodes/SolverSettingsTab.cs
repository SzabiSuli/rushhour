namespace rushhour.src.Nodes;

using System;
using Godot;
using rushhour.src.Model;

public partial class SolverSettingsTab : VBoxContainer {
    [Export] public OptionButton algoOption = null!;
    [Export] public SpinBox tabuSizeSpin = null!;
    [Export] public OptionButton heuristicOption = null!;
    [Export] public CheckBox randomBox = null!;
    [Export] public Button applyButton = null!;

    
    private int _heuristicSelected;
    private float _randomFactor;
    private int _algoOptionSelected;
    private int _tabuSize;

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
        
        ApplySettings();
    }

    public void ApplySettings() {
        _heuristicSelected = heuristicOption.Selected;
        _randomFactor = randomBox.ButtonPressed ? 1f : 0f;
        _algoOptionSelected = algoOption.Selected;
        _tabuSize = (int)tabuSizeSpin.Value;
    }

    public void OnApplyButtonPressed() {
        ApplySettings();
        AlgoPlayer.Instance.ResetSolver();
        TabCont.CurrentTab = 2;
    }

    public void OnAlgoOptionChanged(long index) {
        tabuSizeSpin.Visible = index == 0;
    }

    public Solver GetSolver() {
        Heuristic h = heuristicOption.Selected switch {
            0 => new DistanceHeuristic(),
            1 => new FreeSpacesHeuristic(),
            2 => new MoverHeuristic(),
            _ => throw new Exception("Invalid heuristic option selected")
        };
        Solver s;
        switch (algoOption.Selected) {
            case 0:             
                s = new TabuSolver(h, _tabuSize, _randomFactor);
                break;
            case 1:             
                s = new BacktrackingSolver(h, _randomFactor);
                break;
            case 2:
                if (h is not MonotoneHeuristic mh) {
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
