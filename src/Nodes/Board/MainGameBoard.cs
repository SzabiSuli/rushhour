namespace rushhour.src.Nodes.Board;

using System;
using System.Data.Common;
using System.Linq;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.Nodes3D;

public partial class MainGameBoard : GameBoard
{
	[Export] public Button manualButton = null!;
	[Export] public Button algoButton = null!;

	private BoardMode _mode = BoardMode.ALGO;

    public BoardMode Mode {
		get => _mode;
		set {
			_mode = value;
			// we always update the board (if we can), the performance loss is negalble
			UpdateBoard(); 
		}
	}

    // TODO change this if we want to run multiple algorithms at once
    // which might be a bit out of scope for this project
    private RHGameState? _algoCurrent;
    public RHGameState? AlgoCurrent {
		get => _algoCurrent;
		set {
			_algoCurrent = value;
			if (Mode == BoardMode.ALGO) {
				UpdateBoard();
			}
		}	
	}
    
	private RHGameState? _manualCurrent;
    public RHGameState? ManualCurrent {
		get => _manualCurrent;
		set {
			_manualCurrent = value;
			manualButton.SetPressedNoSignal(true);
			algoButton.SetPressedNoSignal(false);
			Mode = BoardMode.MANUAL;
		}
	}

	public static MainGameBoard Instance {get; private set;} = null!;

    public MainGameBoard() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of MainGameBoard!");
        }
    }

    public override RHGameState Current {get {
            if (Mode == BoardMode.MANUAL) {
                if (ManualCurrent == null) {
                    throw new Exception("No manual state set");
                }
                return ManualCurrent;
            } else {
                if (AlgoCurrent == null) {
                    throw new Exception("No algo state set");
                }
                return AlgoCurrent;
            }
        }
    }  

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        Vertex.VertexClicked += OnVertexClicked;
		manualButton.ButtonGroup.Pressed += OnModeButtonPressed;
    }

	public void OnModeButtonPressed(BaseButton button) {
		if (button == manualButton) {
			ManualCurrent = AlgoCurrent;
		} else if (button == algoButton) {
			Mode = BoardMode.ALGO;
		} else {
			throw new Exception("Unkown button pressed");
		}
	}

	public void OnVertexClicked(object? sender, RHGameState state) => ManualCurrent = state;
	public void OnNewAlgoCurrent(object? sender, RHGameState state) => AlgoCurrent = state;
	
	// TODO manual move from algo mode does not work
	public void MakeManualMove(Move move) {
		StateMove stateMove = new StateMove(Current, Current.WithMove(move), move);
		ManualCurrent = stateMove.To;
		Edge.OnNewEdge(this, stateMove);
	}

	public override void Setup(RHGameState initial) {
		manualButton.Disabled = false;
		algoButton.Disabled = false;

		base.Setup(initial);

		foreach (VehicleNode child in GetChildren().Cast<VehicleNode>()) {
			child.CreateArrows();
			child.UpdateArrows(initial);
		}
		AlgoCurrent = initial;
	}

	public void UpdateBoard() {
		RHGameState state = Current;
		for (int i = 0; i < state.PlacedPieces.Length; i++) {
			VehicleNode v = GetChild<VehicleNode>(i);
			v.Placement = state.PlacedPieces[i];
			v.UpdateArrows(state);
		}
	}
}

public enum BoardMode {
	MANUAL,
	ALGO
}
