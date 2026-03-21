using System.Runtime.CompilerServices;

namespace rushhour.src.Nodes;

using Godot;
using rushhour.src.Model;
using System;

public partial class GameBoard : Sprite2D
{
    public static readonly Vector2 tileSize = new Vector2(24,24);
    public static readonly Vector2 spriteSize = tileSize * 8;

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
    }



    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        // TODO don't call this every frame
		RescaleToParent();
	}

	public void OnVertexClicked(object? sender, RHGameState state) => OnManualMove(state);
	public void MakeManualMove(Move move) => OnManualMove(Current.WithMove(move));
	
	public void OnManualMove(RHGameState state) {
		mode = BoardMode.MANUAL;
		manualCurrent = state;
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

public abstract partial class VehicleNode : Sprite2D {

    public const string arrowScenePath = "res://scenes/arrow.tscn";

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
		CreateArrows();
		UpdateArrows(state);
	}

	public Arrow forwardArrow = null!;
	public Arrow backwardArrow = null!;

	public void CreateArrows() {
		forwardArrow = ArrowCreator.Instantiate<Arrow>();
		forwardArrow.Init(Direction.Up, Placement.Piece.Length);
		AddChild(forwardArrow);
		backwardArrow = ArrowCreator.Instantiate<Arrow>();
		backwardArrow.Init(Direction.Down, Placement.Piece.Length);
		AddChild(backwardArrow);
	}

	public void UpdateArrows(RHGameState state) {
		var fwArrowPos = Placement.Position + Placement.FacingDirection.GetVector();
		fwArrowPos.Deconstruct(out int fwX, out int fwY);
		var bwArrowPos = Placement.Position - Placement.FacingDirection.GetVector() * Placement.Piece.Length;
		bwArrowPos.Deconstruct(out int bwX, out int bwY);
		
		backwardArrow.IsActive = 
			0 <= bwX && bwX < 6 && 0 <= bwY && bwY < 6 
			&& (state.BoardGrid[bwX, bwY] == -1);
		
		forwardArrow.IsActive = 
			0 <= fwX && fwX < 6 && 0 <= fwY && fwY < 6 
			&& (state.BoardGrid[fwX, fwY] == -1);
	}

	public void Move(Direction relative) {
		Direction abs = Placement.FacingDirection;
		if (relative == Direction.Down) {
			abs = abs.GetOpposite();
		}
		GetParent<GameBoard>().MakeManualMove(new Move{PieceIndex = pieceIndex, Dir = abs});
	}

	public abstract void SetSprite(int index);
}

public enum BoardMode {
	MANUAL,
	ALGO
}
