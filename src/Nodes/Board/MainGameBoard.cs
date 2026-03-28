namespace rushhour.src.Nodes.Board;

using System;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.Nodes3D;

public partial class MainGameBoard : GameBoard
{
	[Export] public Button manualButton = null!;
	[Export] public Button algoButton = null!;

    public BoardMode mode = BoardMode.ALGO;

    // TODO change this if we want to run multiple algorithms at once
    // which might be a bit out of scope for this project
    public static RHGameState? algoCurrent;
    public RHGameState? manualCurrent;

    public override RHGameState Current {get {
            if (mode == BoardMode.MANUAL) {
                if (manualCurrent == null) {
                    throw new Exception("No manual state set");
                }
                return manualCurrent;
            } else {
                if (algoCurrent == null) {
                    throw new Exception("No algo state set");
                }
                return algoCurrent;
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
			if (mode == BoardMode.MANUAL) return;
			manualCurrent = algoCurrent;
			mode = BoardMode.MANUAL;
		} else if (button == algoButton) {
			if (mode == BoardMode.ALGO) return;
			mode = BoardMode.ALGO;
		} else {
			throw new Exception("Unkown button pressed");
		}
		UpdateBoard();
	}

	public void OnVertexClicked(object? sender, RHGameState state) => OnManualMove(state);
	public void MakeManualMove(Move move) {
		StateMove stateMove = new StateMove(Current, Current.WithMove(move), move);
		OnManualMove(stateMove.To);
		Edge.OnNewEdge(this, stateMove);
	}
	
	public void OnManualMove(RHGameState state) {
		mode = BoardMode.MANUAL;
		manualCurrent = state;
		manualButton.ButtonPressed = true;
		UpdateBoard();
	}

	public void OnNewAlgoCurrent(object? sender, RHGameState state) {
		algoCurrent = state;
		if (mode == BoardMode.ALGO) {
			UpdateBoard();
		}
	}

	public override void Setup(RHGameState initial) {
		mode = BoardMode.ALGO;
		algoCurrent = initial;
		base.Setup(initial);

		foreach (VehicleNode child in GetChildren()) {
			child.CreateArrows();
			child.UpdateArrows(initial);
		}
		OnNewAlgoCurrent(this, initial);
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
