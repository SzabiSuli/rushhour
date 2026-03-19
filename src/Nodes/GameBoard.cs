namespace rushhour.src.Nodes;

using Godot;
using rushhour.src.Model;
using System;

public partial class GameBoard : Sprite2D
{
    public static readonly Vector2 tileSize = new Vector2(24,24);
    public static readonly Vector2 spriteSize = tileSize * 8;

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
					throw new Exception();
				}
				return manualCurrent;
			} else {
				if (algoCurrent == null) {
					throw new Exception();
				}
				return algoCurrent;
			}
		}
	}  

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
		Vertex.VertexClicked += OnVertexClicked;
	}

	public void DiscoverMoves() {
		foreach (VehicleNode v in GetChildren()) {
			v.AddArrows(Current);
		}
	}

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        // TODO don't call this every frame
		RescaleToParent();
	}

	public void OnVertexClicked(object? sender, RHGameState state) {
		mode = BoardMode.MANUAL;
		UpdateBoard(state);
	}

	public void OnNewAlgoCurrent(object? sender, RHGameState state) {
		algoCurrent = state;
		if (mode == BoardMode.ALGO) {
			UpdateBoard(state);
		}
	}

	public void Setup(RHGameState state) {
		RemovePieces();
		BuildBoard(state);
	}

	public void UpdateBoard(RHGameState state) {
		for (int i = 0; i < state.PlacedPieces.Length; i++) {
			VehicleNode v = GetChild<VehicleNode>(i);
			v.PlaceTo(state.PlacedPieces[i].Position);
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

		foreach (var placedPiece in state.PlacedPieces) {
			VehicleNode v = PutOnBoard(placedPiece);
			
			if (v is CarNode) {
				v.SetSprite(carCount);
				carCount++; 
			} else {
				v.SetSprite(busCount);
				busCount++;
			}
		}
	}


	private VehicleNode PutOnBoard(PlacedRHPiece placedPiece) {
		VehicleNode pieceNode = (placedPiece.Piece is Car ? CarCreator : BusCreator).Instantiate<VehicleNode>();
		pieceNode.placement  = placedPiece;
			
		// pieceNode.Position = tileSize * 1.5f + placedPiece.Position * tileSize;
		pieceNode.PlaceTo(placedPiece.Position);

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
		AddChild(pieceNode);
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

public abstract partial class VehicleNode : Sprite2D {

	public PlacedRHPiece placement = null!;

	public void PlaceTo(Vector2I pos) {
		Position = GameBoard.tileSize * 1.5f + pos * GameBoard.tileSize;
	}
	public Sprite2D? forwardArrow;
	public Sprite2D? backwardArrow;

	public void AddArrows(RHGameState state){}




	public abstract void SetSprite(int index);
}

public enum BoardMode {
	MANUAL,
	ALGO
}