namespace rushhour.src.Nodes;

using Godot;
using System;

public partial class GameBoard : Sprite2D
{
	public static readonly Vector2I tileSize = new Vector2I(24,24);
	public static readonly Vector2I spriteSize = tileSize * 8;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		// TODO don't call this every frame
		RescaleToParent();
	}

	public void RescaleToParent() {
		var parent = GetParent().GetParent();
		if (parent is not Control parentControl) {
			throw new Exception("Gameboard is missing it's parent!");
		}
		Scale = parentControl.Size / spriteSize;
	}
}
