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

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        // TODO don't call this every frame
		RescaleToParent();
	}

	public void DisplayState(object? sender, RHGameState state) {
		RemovePieces();
		BuildBoard(state);
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
			
		pieceNode.Position = tileSize * 1.5f + placedPiece.Position * tileSize;

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
	public abstract void SetSprite(int index);
}
