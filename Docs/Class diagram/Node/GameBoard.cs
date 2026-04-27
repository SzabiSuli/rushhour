namespace rushhour.src.Nodes.Board;

using System;
using Godot;
using rushhour.src.Model;

public class GameBoard : Sprite2D
{
    public static readonly Vector2 tileSize = new Vector2(24,24);
    public static readonly Vector2 spriteSize = tileSize * 8;


    // TODO make these exported?
    public const string carScenePath = "res://scenes/board/car.tscn";
    public const string busScenePath = "res://scenes/board/bus.tscn";

    public static PackedScene CarCreator { get; } = 
        ResourceLoader.Load<PackedScene>(carScenePath);
    public static PackedScene BusCreator { get; } = 
        ResourceLoader.Load<PackedScene>(busScenePath);

	private RHGameState _current = null!;

    public virtual RHGameState Current => _current;


    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        // TODO don't call this every frame
		RescaleToParent();
	}

	public virtual void Setup(RHGameState initial) {
		RemovePieces();
		BuildBoard(initial);
	}

	public void RemovePieces() {
		foreach (var child in GetChildren()) {
			child.Free();
		}
	}
	public void BuildBoard(RHGameState state) {
		int carCount = 0;
		int busCount = 0;

		for (int i = 0; i < state.PlacedPieces.Count; i++) {
			PlacedRHPiece placedPiece = state.PlacedPieces[i];
			VehicleNode v = PutOnBoard(placedPiece, i, state);
			
			if (v is CarNode) {
				v.SetSprite(carCount);
				carCount++; 
			} else {
				v.SetSprite(busCount);
				busCount++;
			}
		}
	}


	protected VehicleNode PutOnBoard(PlacedRHPiece placedPiece, int pieceIndex, RHGameState state) {
		VehicleNode pieceNode = (placedPiece.Piece is Car ? CarCreator : BusCreator).Instantiate<VehicleNode>();
		AddChild(pieceNode);
		pieceNode.Init(placedPiece, pieceIndex, state);
		

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

namespace rushhour.src.Nodes.Board;

using System;
using System.Linq;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.Nodes3D;
using rushhour.src.Nodes.UI;

public class MainGameBoard : GameBoard
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

    // TODO change this if we want to run multiple algorithms at once
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

namespace  rushhour.src.Nodes.Board;

using System;
using Godot;
using rushhour.src.Model;

public class Arrow : Area2D
{

    public bool IsActive
    {
        get => Visible && InputPickable;
        set
        {
            Visible = value;
            InputPickable = value;
            
            // Optional: If you want to stop it from bumping into other physics objects too:
            CollisionShape.Disabled = !value;
        }
    }

    public Sprite2D Sprite => GetChild<Sprite2D>(1);
    public CollisionPolygon2D CollisionShape => GetNode<CollisionPolygon2D>("CollisionPolygon2D");
    public Direction direction;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        this.InputEvent += OnInputEvent;
    }

    public void Init(Direction d, int vehicleLength) {
        switch (d) {
            case Direction.Up:
                Position = new Vector2(0, -GameBoard.tileSize.Y);
                break;
            case Direction.Down:
                Position = new Vector2(0, GameBoard.tileSize.Y * vehicleLength);
                Rotation = (float)Math.PI;
                break;
            default:
                throw new ArgumentException("Arrow direction can only be up or down");
        }
        direction = d;
    }

    

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx) {
        if (@event is InputEventMouseButton mouseEvent) {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed) {
                HandleClick();
            }
        }
    }

    public void HandleClick() {
        GetParent<VehicleNode>().Move(direction);
    }
}
namespace rushhour.src.Nodes.Board;

using System;
using Godot;

public class CarNode : VehicleNode
{
    public override void SetSprite(int index) {
        if (index < 0 || index > 18) {
            throw new ArgumentException("Sprite index for car must be between 0 and 11.");
        }
        int ts = (int)GameBoard.tileSize.X;

        if (index == 0) {
            RegionRect = new Rect2(ts, 6 * ts, ts, 2 * ts);
            return;
        }

        if (index > 9) {
            index -= 9;
        }

        RegionRect = new Rect2(ts * index, 8 * ts, ts, 2 * ts);
    }
}
namespace rushhour.src.Nodes.Board;

using System;
using Godot;

public class BusNode : VehicleNode
{
    public override void SetSprite(int index) {
        if (index < 0 || index > 4) {
            throw new ArgumentException("Sprite index for bus must be between 0 and 4.");
        }
        int ts = (int)GameBoard.tileSize.X;

        RegionRect = new Rect2(ts * (index % 2), 3 * ts * (index / 2), ts, 3 * ts);
    }
}
namespace rushhour.src.Nodes.UI;


using System;
using Godot;
using rushhour.src.Model;


public class LevelsGrid : GridContainer
{

    public const String levelButtonScenePath = "res://scenes/ui/tabs/load_level_button.tscn";


    public static PackedScene LevelButtonCreator { get; } = 
        ResourceLoader.Load<PackedScene>(levelButtonScenePath);

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        for (int i = 0; i < Levels.LevelCount; i++) {
            Level level = Levels.LoadLevel(i);
            LoadLevelButton llb = LevelButtonCreator.Instantiate<LoadLevelButton>();
            llb.Init(level);
            AddChild(llb);
            llb.button.Pressed += () => AlgoPlayer.Instance.LoadLevel(level.State);
        }
    }
}

namespace rushhour.src.Nodes.Board;

using Godot;
using rushhour.src.Model;

public abstract class VehicleNode : Sprite2D {

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
