namespace rushhour.src.Nodes;

using Godot;
using rushhour.src.Model;
using System;

public partial class GameBoard : Sprite2D
{
    public static readonly Vector2 tileSize = new Vector2(24,24);
    public static readonly Vector2 spriteSize = tileSize * 8;

	[Export] public Button manualButton = null!;
	[Export] public Button algoButton = null!;

    // TODO make these exported?
    public const string carScenePath = "res://scenes/car.tscn";
    public const string busScenePath = "res://scenes/bus.tscn";

    public static PackedScene CarCreator { get; } = 
        ResourceLoader.Load<PackedScene>(carScenePath);
    public static PackedScene BusCreator { get; } = 
        ResourceLoader.Load<PackedScene>(busScenePath);

    public BoardMode mode = BoardMode.ALGO;

    // TODO change this if we want to run multiple algorithms at once
    // which might be a bit out of scope for this project
    public static RHGameState? algoCurrent;
    public RHGameState? manualCurrent;

    public RHGameState Current {get {
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
		UpdateBoard(Current);
	}


    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        // TODO don't call this every frame
		RescaleToParent();
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
		UpdateBoard(state);
	}

	public void OnNewAlgoCurrent(object? sender, RHGameState state) {
		algoCurrent = state;
		if (mode == BoardMode.ALGO) {
			UpdateBoard(state);
		}
	}

	public void Setup(RHGameState initial) {
		mode = BoardMode.ALGO;
		algoCurrent = initial;
		RemovePieces();
		BuildBoard(initial);
		OnNewAlgoCurrent(this, initial);
	}

	// TODO use Current?
	public void UpdateBoard(RHGameState state) {
		for (int i = 0; i < state.PlacedPieces.Length; i++) {
			VehicleNode v = GetChild<VehicleNode>(i);
			v.Placement = state.PlacedPieces[i];
			v.UpdateArrows(state);
		}
	}

	public void RemovePieces() {
		foreach (var child in GetChildren()) {
			child.QueueFree();
		}
	}
	public void BuildBoard(RHGameState state) {
		int carCount = 0;
		int busCount = 0;

		for (int i = 0; i < state.PlacedPieces.Length; i++) {
			PlacedRHPiece placedPiece = state.PlacedPieces[i];
			VehicleNode v = PutOnBoard(placedPiece, i);
			
			if (v is CarNode) {
				v.SetSprite(carCount);
				carCount++; 
			} else {
				v.SetSprite(busCount);
				busCount++;
			}
		}
	}


	private VehicleNode PutOnBoard(PlacedRHPiece placedPiece, int pieceIndex) {
		VehicleNode pieceNode = (placedPiece.Piece is Car ? CarCreator : BusCreator).Instantiate<VehicleNode>();
		AddChild(pieceNode);
		pieceNode.Init(placedPiece, pieceIndex, Current);
		

		switch (placedPiece.FacingDirection) {
			case Direction.Up:
				pieceNode.RotationDegrees = 0;
				break;
			case Direction.Down:
				pieceNode.RotationDegrees = 180;
				break;
			case Direction.Left:
				pieceNode.RotationDegrees = 270;
				break;
			case Direction.Right:
				pieceNode.RotationDegrees = 90;
				break;
		}
		return pieceNode;
	}



	public void RescaleToParent() {
		var parent = GetParent().GetParent();
		if (parent is not Control parentControl) {
            throw new Exception("Gameboard is missing it's parent!");
		}
		Scale = parentControl.Size / spriteSize;
	}
}

public enum BoardMode {
	MANUAL,
	ALGO
}
