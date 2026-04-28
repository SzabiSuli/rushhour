namespace rushhour.src.Nodes.Board;

using System;
using System.Linq;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.Nodes3D;
using rushhour.src.Nodes.UI;

public partial class MainGameBoard : GameBoard
{
	[Export] public Button manualButton = null!;
	[Export] public Button algoButton = null!;
    [Export] public CheckButton followAlgoButton = null!;

	private BoardMode _mode = BoardMode.ALGO;

    public BoardMode Mode {
		get => _mode;
		set {
			if (_mode == value) return;

			_mode = value;

			if (_mode == BoardMode.ALGO) {
				ManualCurrent = null;
			}

			manualButton.SetPressedNoSignal(_mode == BoardMode.MANUAL);
			algoButton.SetPressedNoSignal(_mode == BoardMode.ALGO);

			// we always update the board (if we can), the performance loss is negalble
			UpdateBoard(); 
		}
	}

    // change this if we want to run multiple algorithms at once
    // which might be a bit out of scope for this project
    private RHGameState? _algoCurrent;
    public RHGameState? AlgoCurrent {
		get => _algoCurrent;
		set {
			_algoCurrent = value;
			
			if (_algoCurrent == null) return;

			if (followAlgoButton.ButtonPressed) {
				Camera3d.Instance.followTarget = Vertex.Dict[_algoCurrent];
			}
			if (Mode != BoardMode.ALGO) return;

			UpdateBoard();
		}	
	}
    
	private RHGameState? _manualCurrent;
    public RHGameState? ManualCurrent {
		get => _manualCurrent;
		set {
			if (_manualCurrent == value) return;
			if (_manualCurrent != null) {
				// Update previous manual current effect
				Vertex.Dict[_manualCurrent].RemoveEffect(VertexEffect.ManualCurrent);
			}
			_manualCurrent = value;
			UpdateBoard();

			if (_manualCurrent == null) return;
		
			// Update new manual current effect
			Vertex.Dict[_manualCurrent].AddEffect(VertexEffect.ManualCurrent);
			// Put the camera's center to the new manual current
			Camera3d.Instance.followTarget = Vertex.Dict[_manualCurrent];
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

    public override void _Process(double delta) {
        // For some reason this does not work when called in _Ready 
		// or any of its parents' _Ready
		// or any events like VisibilityChanged or TabChanged
		RescaleToParent();
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
	
	public void MakeManualMove(Move move) {
		StateMove stateMove = new StateMove(Current, Current.WithMove(move), move);
		// Create vertex first, 
		// so ManualCurrent effect can be applied to the vertex
		Edge.OnNewEdge(this, stateMove);
		ManualCurrent = stateMove.To;
	}

	public override void Setup(RHGameState initial) {
		manualButton.Disabled = false;
		algoButton.Disabled = false;

		base.Setup(initial);

		foreach (VehicleNode child in GetChildren().Cast<VehicleNode>()) {
			child.CreateArrows();
			child.UpdateArrows(initial);
		}

		// Use private fields to avoid triggering manual mode and board update.
		_algoCurrent = initial;
		
		// Board gets udpated here, it will also be updated in solver.Start
		Mode = BoardMode.ALGO;
	}

	public void UpdateBoard() {
		RHGameState state = Current;
		for (int i = 0; i < state.PlacedPieces.Count; i++) {
			VehicleNode v = GetChild<VehicleNode>(i);
			v.Placement = state.PlacedPieces[i];
			v.UpdateArrows(state);
		}
		StatusContainer.Instance.UpdateHeuristicLabel(state);
	}
}

public enum BoardMode {
	MANUAL,
	ALGO
}
