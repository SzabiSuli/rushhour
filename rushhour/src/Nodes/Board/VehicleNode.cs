namespace rushhour.src.Nodes.Board;

using Godot;
using System;
using rushhour.src.Model;

public abstract partial class VehicleNode : Sprite2D {

    public const string arrowScenePath = "res://scenes/board/arrow.tscn";

    public static PackedScene ArrowCreator { get; } = 
        ResourceLoader.Load<PackedScene>(arrowScenePath);


	protected PlacedRHPiece _placement = null!;
	public PlacedRHPiece Placement { 
		get => _placement; 
		set {
			if (value == _placement) return;
			Position = GameBoard.tileSize * 1.5f + value.Position * GameBoard.tileSize;
			_placement = value;
		} 
	}

	public int pieceIndex;

	public void Init(PlacedRHPiece pp, int pieceIndex, RHGameState state) {
		this.Placement = pp;
		this.pieceIndex = pieceIndex;
	}

	public Arrow? forwardArrow = null;
	public Arrow? backwardArrow = null;

	public void CreateArrows() {
		forwardArrow = ArrowCreator.Instantiate<Arrow>();
		forwardArrow.Init(Direction.Up, Placement.Piece.Length);
		AddChild(forwardArrow);
		backwardArrow = ArrowCreator.Instantiate<Arrow>();
		backwardArrow.Init(Direction.Down, Placement.Piece.Length);
		AddChild(backwardArrow);
	}

	public void UpdateArrows(RHGameState state) {
		if (backwardArrow == null || forwardArrow == null) {
			throw new Exception("Update arrows should only be called on a VehicleNode belonging to MainGameBoard!");
		}
		
		var fwArrowPos = Placement.Position + Placement.FacingDirection.GetVector();
		fwArrowPos.Deconstruct(out int fwX, out int fwY);
		var bwArrowPos = Placement.Position - Placement.FacingDirection.GetVector() * Placement.Piece.Length;
		bwArrowPos.Deconstruct(out int bwX, out int bwY);
		
		backwardArrow.IsActive = 
			0 <= bwX && bwX < 6 && 0 <= bwY && bwY < 6 
			&& (state[bwX, bwY] == -1);
		
		forwardArrow.IsActive = 
			0 <= fwX && fwX < 6 && 0 <= fwY && fwY < 6 
			&& (state[fwX, fwY] == -1);
	}

	public void Move(Direction relative) {
		Direction abs = Placement.FacingDirection;
		if (relative == Direction.Down) {
			abs = abs.GetOpposite();
		}
		GetParent<MainGameBoard>().MakeManualMove(new Move{PieceIndex = pieceIndex, Dir = abs});
	}

	public abstract void SetSprite(int index);
}
