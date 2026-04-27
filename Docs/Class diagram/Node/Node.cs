namespace  rushhour.src.Nodes.Board;

using System;
using Godot;
using rushhour.src.Model;

public partial class Arrow : Area2D
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

public partial class BusNode : VehicleNode
{
    public override void SetSprite(int index) {
        if (index < 0 || index > 4) {
            throw new ArgumentException("Sprite index for bus must be between 0 and 4.");
        }
        int ts = (int)GameBoard.tileSize.X;

        RegionRect = new Rect2(ts * (index % 2), 3 * ts * (index / 2), ts, 3 * ts);
    }
}
namespace rushhour.src.Nodes.Board;

using System;
using Godot;

public partial class CarNode : VehicleNode
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
using rushhour.src.Model;

public partial class GameBoard : Sprite2D
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
namespace rushhour.src.Nodes.Board;

using Godot;
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
namespace rushhour.src.Nodes.Nodes3D;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.UI;

namespace rushhour.src.Nodes.Nodes3D;

// using System;
// using Godot;

// public partial class Camera3d : Camera3D {
//     // =========================
//     // Camera movement settings
//     // =========================
//     [ExportCategory("Camera movement")]
//     [Export] public float CameraSpeed { get; set; } = 1.0f;
//     [Export] public float CameraZoomSpeed { get; set; } = 20.0f;
//     [Export] public float CameraZoomMin { get; set; } = 10.0f;
//     [Export] public float CameraZoomMax { get; set; } = 50.0f;

//     // =========================
//     // Rotation (RMB) settings
//     // =========================
//     [ExportCategory("Rotation")]
//     [Export] public float YawSensitivity { get; set; } = 0.5f;
//     [Export] public float PitchSensitivity { get; set; } = 0.5f;
//     [Export] public bool CaptureMouseOnRmb { get; set; } = false;

// 	public static Camera3d Instance {get; private set;} = null!;
//     public Camera3d() {
//         if (Instance is null) {
//             Instance = this;
//         } else {
//             throw new Exception("Should only create one instance of Camera3d!");
//         }
//     }

//     // =========================
//     // Runtime state
//     // =========================
// 	private const float transitionSpeed = 10f;
// 	private Vector3 _orbitCenter = Vector3.Zero;
//     private Vector3 _targetOrbitCenter = Vector3.Zero;
// 	public Vector3 TargetOrbitCenter {
// 		get => followTarget?.Position ?? _targetOrbitCenter;
// 		set {
// 			followTarget = null;
// 			_targetOrbitCenter = value;
// 		}
// 	}
//     public float OrbitDistance { get; set; } = 4000.0f;
// 	public Vertex? followTarget;
// 	private Vector3 _offsetDirection = new Vector3(0, 1, 0);
// 	private Vector3 _upVector = new Vector3(0, 0, -1);

//     private bool _isRmbRotating = false;

// 	// With a Size of 10 a 256px sprite is 64px on the screen
// 	public float ZoomFactor => 2.5f / Size;
//     public override void _Process(double delta) {
//         var movement = Vector3.Zero;

//         if (Input.IsKeyPressed(Key.D))
//             movement.X += 1;
//         if (Input.IsKeyPressed(Key.A))
//             movement.X -= 1;
//         if (Input.IsKeyPressed(Key.W))
//             movement.Y += 1;
//         if (Input.IsKeyPressed(Key.S))
//             movement.Y -= 1;
//         if (Input.IsKeyPressed(Key.R))
//             movement.Z += 1;
//         if (Input.IsKeyPressed(Key.F))
//             movement.Z -= 1;
        
//         // Shift boost
//         float speedMultiplier = Input.IsKeyPressed(Key.Shift) ? 4.0f : 1.0f;

//         // Move orbit center relative to the camera's view
// 		if (movement != Vector3.Zero) {
// 			movement = movement.Normalized();
// 			// Use the Basis of the node relative to the world
// 			Vector3 worldMovement = GlobalTransform.Basis * movement * Size;
// 			TargetOrbitCenter += worldMovement * CameraSpeed * speedMultiplier * (float)delta;
// 		}

// 		_orbitCenter = _orbitCenter.Lerp(TargetOrbitCenter, (float)delta * transitionSpeed);
// 		Position = _orbitCenter + _offsetDirection * OrbitDistance;
// 		LookAt(_orbitCenter, _upVector);
// 	}

// 	public override void _UnhandledInput(InputEvent @event) {
// 		// Mouse wheel zoom (changes orbit_distance)
// 		if (@event is InputEventMouseButton mouseButton) {
// 			if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelUp) {
// 				if (Size >= 20) {
// 					Size -= 10;
// 				}
// 			} else if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelDown) {
// 				if (Size <= 990){
// 					Size += 10;
// 				}
// 			}

// 			// Start/stop rotate+tilt with M2
// 			if (mouseButton.ButtonIndex == MouseButton.Right) {
// 				_isRmbRotating = mouseButton.Pressed;
// 				if (CaptureMouseOnRmb) {
// 					Input.MouseMode = mouseButton.Pressed
// 						? Input.MouseModeEnum.Captured
// 						: Input.MouseModeEnum.Visible;
// 				}
// 			}
// 		}
// 		// Delta-based rotation while dragging with M2
// 		else if (@event is InputEventMouseMotion mouseMotion && _isRmbRotating) {
// 			Vector2I vp = GetViewport().GetWindow().Size;
// 			float vmin = Mathf.Min(vp.X, vp.Y);
// 			float dt = (float)GetProcessDeltaTime();
// 			float sixtyFps = 60.0f * dt;

// 			// px -> normalized fraction of screen -> radians, scaled by dt
// 			float dx = (mouseMotion.Relative.X / vmin) * YawSensitivity * Mathf.Tau * sixtyFps;
// 			float dy = (mouseMotion.Relative.Y / vmin) * PitchSensitivity * Mathf.Tau * sixtyFps;

// 			// Local left axis
// 			Vector3 leftAxis = _offsetDirection.Cross(_upVector).Normalized();
			
// 			// Pitch rotation
// 			Basis pitchRotation = new Basis(leftAxis, dy);
// 			_offsetDirection = pitchRotation * _offsetDirection;
// 			_upVector = pitchRotation * _upVector;
			
// 			// Yaw rotation
// 			Basis yawRotation = new Basis(_upVector, -dx);
// 			_offsetDirection = yawRotation * _offsetDirection;
// 			_upVector = yawRotation * _upVector;
			
// 			_offsetDirection = _offsetDirection.Normalized();
// 			_upVector = _upVector.Normalized();
			
// 			// Re-orthogonalize to prevent drift over time
// 			leftAxis = _offsetDirection.Cross(_upVector).Normalized();
// 			_upVector = leftAxis.Cross(_offsetDirection).Normalized();
// 		}
// 	}
// }

public class Edge {
    // Physics constants
    public const int springLength = 1;
    public const double optimalIntervalLowerBound = springLength * 0.9;
    public const double optimalIntervalUpperBound = springLength * 1.1;
    public const float springForce = 100;

    // Instance data
    public Vertex From { get; init; }
    public Vertex To { get; init; }
    public StateMove MoveUsed { get; init; }

    // Static registry
    public static Dictionary<StateMove, Edge> Dict { get; } = new();

    // Priority effects as enum ints in set
    private HashSet<EdgeEffect> _effects = new();
    public void AddEffect(EdgeEffect e) => _effects.Add(e);
    public void RemoveEffect(EdgeEffect e) => _effects.Remove(e);
    public void SetEffect(EdgeEffect e, bool active) {
        if (active) AddEffect(e); else RemoveEffect(e);
    }
    public void ClearEffects() => _effects.Clear();
    public EdgeEffect? Effect => _effects.Any() ? _effects.Min() : null;
    public bool Hidden => HideButton.Instance.ButtonPressed && Effect == null;


    public Color GetColor() => Effect switch {
        EdgeEffect.SolutionEdge => new Color(0, 1, 0, 1f),
        EdgeEffect.AlgoEdge     => new Color(1, 1, 0, 0.5f),
        _                       => new Color(1, 1, 1, 0.1f),
    };

    // Construction

    public Edge(Vertex from, Vertex to, StateMove moveUsed) {
        From = from;
        To = to;
        MoveUsed = moveUsed;
    }

    public static Edge GetOrCreate(StateMove move) {
        if (Dict.TryGetValue(move, out Edge? edge)) {
            return edge;
        }

        edge = new Edge(
            Vertex.Dict[move.From], 
            Vertex.Dict[move.To], 
            move
        );
        Dict[move] = edge;
        return edge;
    }

    // Physics
    public void ApplySpringForce() {
        Vector3 distanceVector = To.Position - From.Position;
        var length = distanceVector.Length();
        if (optimalIntervalLowerBound < length && length < optimalIntervalUpperBound) {
            // Spring is close to the optimal length, skip for performance
            return;
        }
        Vector3 force = distanceVector * ((length - springLength) / length) * springForce;

        From.ApplyPendingForce(force);
        To.ApplyPendingForce(-force);
    }

    // Static event handlers for solver integration

    public static void OnNewEdge(object? sender, StateMove edge) {
        // assume the vertex where we moved from already exists, 
        // find it
        Vertex from = Vertex.Dict[edge.From];

        // if the vertex we want to visit 
        // is already created and connected with its neighbours, skip it.
        if (Vertex.Dict.TryGetValue(edge.To, out Vertex? to)) {
            return;
        }

        Vertex.GetOrCreate(edge.To, from);
        GetOrCreate(edge);

        // Connect remaining edges to existing neighbors
        IEnumerable<StateMove> stateMoves = edge.To.GetPossibleMoves().Select(
            move => new StateMove(edge.To, edge.To.WithMove(move), move)
        );

        foreach (StateMove stateMove in stateMoves) {
            if (Vertex.Dict.TryGetValue(stateMove.To, out Vertex? v)) {
                Edge.GetOrCreate(stateMove);
            }
        }
    }

    public static void OnPathChange(object? _, PathChangeArgs args) {
        Dict[args.move].SetEffect(EdgeEffect.AlgoEdge, args.onPath);
    } 
}

public enum EdgeEffect {
    // listed from highest priority to lowest
    SolutionEdge = 0,
    AlgoEdge = 1
}
namespace rushhour.src.Nodes.Nodes3D;

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class EdgeDrawer : MultiMeshInstance3D {
    private ImmediateMesh _mesh = null!;

    public override void _Ready() {
        _mesh = new ImmediateMesh();

        var meshInstance = new MeshInstance3D {
            Mesh = _mesh,
            // VertexColorUseAsAlbedo lets us set per-edge colors via SurfaceSetColor
            MaterialOverride = new StandardMaterial3D() {
                AlbedoColor = Colors.White,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                VertexColorUseAsAlbedo = true,
            }
        };
        AddChild(meshInstance);
    }

    // Rebuilds all edge line geometry from current vertex positions and edge colors.
    // Called once per physics frame by GraphScene.
    public void UpdateVisuals() {
        _mesh.ClearSurfaces();

        IEnumerable<Edge> visableEdges = Edge.Dict.Values.Where(e => !e.Hidden);
        if (!visableEdges.Any()) return;

        _mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        foreach (Edge edge in visableEdges) {
            Color color = edge.GetColor();
            _mesh.SurfaceSetColor(color);
            _mesh.SurfaceAddVertex(edge.From.Position);
            _mesh.SurfaceAddVertex(edge.To.Position);
        }
        _mesh.SurfaceEnd();
    }
}
using rushhour.src.Nodes.Nodes3D;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using rushhour.src.Model;

public partial class GraphScene : Node3D {
    public static GraphScene Instance {get; private set;} = null!;

    // Child drawer nodes (set in _Ready from scene tree)
    private VertexDrawer _vertexDrawer = null!;
    private EdgeDrawer _edgeDrawer = null!;

    public GraphScene() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of GraphScene!");
        }
    }

    // Max distance in pixels from a vertex centre to register as a click
    // deliberitely make radius twice as big as the sprite, to allow bigger vertices to be selected precisely,
    // we allow unit sized vertieces have a larger detection radius
    private float PickRadiusPx => VertexDrawer.spriteScale * VertexDrawer.spriteSizePx * Camera3d.Instance.ZoomFactor;

    // Use frame skips to keep ui responsive:
    // until 1000 vertices use no frame skips
    // from 1000 to 8000 lineary go up to 5 frame skips
    private int FramesToSkip => Math.Clamp((Vertex.Dict.Count - 1000) * 5 / 7000, 0, 5);
    private int _skippedFrames = 0;

    public override void _Ready() {
        RenderingServer.SetDefaultClearColor(Colors.Black);

        _vertexDrawer = GetNode<VertexDrawer>("VertexDrawer");
        _edgeDrawer = GetNode<EdgeDrawer>("EdgeDrawer");
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left) return;
        if (!mb.Pressed) return;

        Vertex? hit = PickVertex(mb.Position);
        if (hit != null) {
            Vertex.FireVertexClicked(this, hit.GameState);
            GetViewport().SetInputAsHandled();
        }
    }

    // Projects every vertex to screen space and returns the nearest one
    // within PickRadiusPx pixels of the given screen position, or null.
    // O(V) but but only needed on click
    private Vertex? PickVertex(Vector2 screenPos) {
        Camera3D cam = Camera3d.Instance;
        Vertex? best = null;
        float bestDistSq = PickRadiusPx * PickRadiusPx;

        foreach (Vertex v in Vertex.Dict.Values) {
            // Skip transparent (hidden) vertices
            if (v.Hidden) continue;

            // is_position_behind returns true when the point is behind the camera
            if (cam.IsPositionBehind(v.Position)) continue;

            Vector2 projected = cam.UnprojectPosition(v.Position);
            float distSq = screenPos.DistanceSquaredTo(projected);
            if (distSq < bestDistSq) {
                bestDistSq = distSq;
                best = v;
            }
        }

        return best;
    }

    public override void _PhysicsProcess(double delta) {
        // skip a physics frame if too many vertices are present, to keep the ui responsible
        if (_skippedFrames < FramesToSkip) {
            _skippedFrames++;
            return;
        }

        _skippedFrames = 0;

        // Stage 1: OctTree Build
        OctTreeNode? tree = OctTreeNode.Build(Vertex.Dict.Values);

        // Stage 2: Force Computation (parallel)

        // Vertex repulsion via Barnes-Hut
        var vertices = Vertex.Dict.Values.ToArray();
        var edges = Edge.Dict.Values.ToArray();

        Parallel.ForEach(vertices, v => {
            if (tree != null) {
                var force = tree.ComputeForce(v);
                v.ApplyPendingForce(force);
            }
        });

        // Edge spring forces
        Parallel.ForEach(edges, e => {
            e.ApplySpringForce();
        });

        // Stage 3: Position Integration (parallel)
        Parallel.ForEach(vertices, v => {
            v.Integrate(delta);
        });


        // Stage 4: Visual updates via multi mesh
        _vertexDrawer.UpdateVisuals();
        _edgeDrawer.UpdateVisuals();

    }

    public void Setup(RHGameState initial) {
        Clear();

        // Create the initial vertex
        Vertex v = Vertex.GetOrCreate(initial, null);
        v.AddEffect(VertexEffect.Initial);
        Camera3d.Instance.followTarget = v;
    }

    public void Clear() {
        Edge.Dict.Clear();
        Vertex.Dict.Clear();
    }

    public void ClearPathHighligh() {
        foreach (Edge edge in Edge.Dict.Values) {
            edge.ClearEffects();
        }

        foreach (Vertex vertex in Vertex.Dict.Values) {
            vertex.ClearEffects();
        }
    }
}
namespace rushhour.src.Nodes.Nodes3D;

using System.Collections.Generic;
using System.Linq;
using Godot;

// A node in the Barnes-Hut OctTree, representing a cubic region of 3D space.
public class OctTreeNode {
    // Each node must be either a:
    //      Leaf - Body contains a Vertex, Children are all null
    //      Node - Body is null, Children has at least one non null nodes


    // Barnes-Hut approximation parameter. Higher values = faster but less accurate.
    // Typical range: 0.5 (accurate) to 1.5 (fast). 1 is a good default.
    public const float theta = 1f;

    public Vector3 BoundsMin;

    public Vector3 BoundsMax;

    // 8 children, one per octant. Null if the octant is empty or this is a leaf.
    // Octant indexing: bit 0 = X axis, bit 1 = Y axis, bit 2 = Z axis.
    // 0 = smaller than midpoint, 1 = greater than midpoint for each axis.
    public OctTreeNode?[] Children = new OctTreeNode?[8];

    public Vector3 CenterOfMass;

    public int Count;

    public Vertex? Body;

    public bool IsLeaf => Body != null;

    // Construct methods
    // Constructor is private because we need to insert a node after initialising
    private OctTreeNode(Vector3 boundsMin, Vector3 boundsMax) {
        BoundsMin = boundsMin;
        BoundsMax = boundsMax;
        CenterOfMass = Vector3.Zero;
        Count = 0;
        Body = null;
    }

    public static OctTreeNode? Build(IEnumerable<Vertex> vertices) {
        // Don't build a tree if no vertices are passed through
        if (!vertices.Any()) return null;

        // Compute bounding box enclosing all vertices with padding
        Vector3 min = Vector3.Inf;
        Vector3 max = -Vector3.Inf;

        Vector3 sum = Vector3.Zero;

        foreach (Vertex vertex in vertices) {
            Vector3 pos = vertex.Position;
            min = min.Min(pos);
            max = max.Max(pos);
            sum += pos;
        }
        
        // Make the center the center of mass to make a balanced Tree
        Vector3 center = sum / vertices.Count();

        // Make the bounding box cubic (equal side lengths) for uniform subdivision
        float maxDistance = new[] {
            center.X - min.X,
            center.Y - min.Y,
            center.Z - min.Z,
            max.X - center.X,
            max.Y - center.Y,
            max.Z - center.Z,
        }.Max();

        Vector3 halfExtent = Vector3.One * maxDistance;
        min = center - halfExtent;
        max = center + halfExtent;

        OctTreeNode root = new OctTreeNode(min, max);

        foreach (var vertex in vertices) {
            root.Insert(vertex);
        }

        return root;
    }

    // Structure methods
    private void Insert(Vertex vertex) {
        if (Count == 0) {
            // Empty node - place the vertex here as a leaf
            Body = vertex;
            CenterOfMass = vertex.Position;
            Count = 1;
            return;
        }

        if (IsLeaf) {
            // This leaf already has a body - subdivide
            Vertex existing = Body!;
            Body = null;

            // Re-insert the existing body into a child
            int existingOctant = GetOctant(existing.Position);
            EnsureChild(existingOctant);
            Children[existingOctant]!.Insert(existing);

            // Insert the new body into a child
            int newOctant = GetOctant(vertex.Position);
            EnsureChild(newOctant);
            Children[newOctant]!.Insert(vertex);

            // Update this node's aggregate data
			CenterOfMass = (existing.Position + vertex.Position) / 2f;
			Count = 2;
			return;
		}

		// Internal node - insert into the appropriate child
		int octant = GetOctant(vertex.Position);
		EnsureChild(octant);
		Children[octant]!.Insert(vertex);

		// Update aggregate: running center of mass and count
		CenterOfMass = (CenterOfMass * Count + vertex.Position) / (Count + 1);
		Count++;
	}

    // Determines which octant (0-7) a position falls into relative to this node's center.
	public int GetOctant(Vector3 position) {
		Vector3 center = (BoundsMin + BoundsMax) / 2f;
		int octant = 0;
		if (position.X >= center.X) octant += 1;
		if (position.Y >= center.Y) octant += 2;
		if (position.Z >= center.Z) octant += 4;
		return octant;
	}

    // Creates the child node for the given octant if it doesn't exist yet.
    public void EnsureChild(int octant) {
        if (Children[octant] != null) return;

        Vector3 center = (BoundsMin + BoundsMax) / 2f;
        Vector3 childMin = BoundsMin;
        Vector3 childMax = center;

        if ((octant & 1) != 0) { childMin.X = center.X; childMax.X = BoundsMax.X; }
        if ((octant & 2) != 0) { childMin.Y = center.Y; childMax.Y = BoundsMax.Y; }
        if ((octant & 4) != 0) { childMin.Z = center.Z; childMax.Z = BoundsMax.Z; }

        Children[octant] = new OctTreeNode(childMin, childMax);
    }

    // Force methods
	// Computes the approximate repulsion force on a target vertex using the Barnes-Hut criterion.
	// Returns the total force vector to be applied.
	public Vector3 ComputeForce(Vertex target) {
		if (Count == 0)
			return Vector3.Zero;

		// Leaf node with a single body
		if (IsLeaf) {
			if (Body == target) {
				return Vector3.Zero;
            }

			return ComputeDirectForce(target.Position);
		}

		// Internal node - check Barnes-Hut criterion
		Vector3 diff = CenterOfMass - target.Position;
		float distance = diff.Length();

		if (distance < 0.001f) {
			// Too close to center of mass - recurse into children to avoid division issues
			return SumChildForces(target);
		}

		float cellSize = BoundsMax.X - BoundsMin.X; // Cubic, so any axis works
		float ratio = cellSize / distance;

		if (ratio < theta) {
			// Cell is far enough - approximate the entire cluster as one body
			return ComputeDirectForce(target.Position);
		}

		// Cell is too close - recurse into children
		return SumChildForces(target);
	}

	private  Vector3 SumChildForces(Vertex target) {
		Vector3 totalForce = Vector3.Zero;
		for (int i = 0; i < 8; i++) {
			if (Children[i] != null) {
				totalForce += Children[i]!.ComputeForce(target);
			}
		}
		return totalForce;
	}

	// Computes the direct repulsion force from a body (or cluster of bodies) at sourcePos
	// on a target at targetPos. Uses the same inverse-square law as Vertex.ApplyRepulsionForce.
	// The mass parameter scales the force for clusters.
	private Vector3 ComputeDirectForce(Vector3 targetPos) {
		Vector3 distanceVector = CenterOfMass - targetPos;
		float distSq = distanceVector.LengthSquared();

		if (distSq > Vertex.influenceRadius * Vertex.influenceRadius) {
			// Beyond influence radius - skip (matches existing early-out)
			return Vector3.Zero;
		}

		if (distSq < 0.0001f) {
			// Prevent division by zero for overlapping vertices
			return Vector3.Zero;
		}

		float dist = Mathf.Sqrt(distSq);
		Vector3 direction = distanceVector / dist;

		// F = -repulsionForce * direction / distance² * mass
        // Negative because it's repulsion (away from source)
        return direction / distSq * (-Vertex.repulsionForce * Count);
    }
}
namespace rushhour.src.Nodes.Nodes3D;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.UI;

public class Vertex {
    // Physics constants
    public const int repulsionForce = 1000;
    public const int influenceRadius = 1000;
    public const float linearDamp = 10.0f;
    public const float maxSpeed = 100f;
    public const float spawnDistanceFactor = 3;

    // Instance data
    public RHGameState GameState { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; private set; } = Vector3.Zero;

    // Thread safe force accumulation
    private Vector3 _pendingForces = Vector3.Zero;
    private readonly object _forcesLock = new();


    // Static registry
    public static Dictionary<RHGameState, Vertex> Dict { get; } = new();

    // Events
    public static event EventHandler<RHGameState>? VertexClicked;
    public static void FireVertexClicked(object sender, RHGameState state) =>
        VertexClicked?.Invoke(sender, state);


    private static Vertex? _algoCurrent = null;
    public static Vertex? AlgoCurrent {
        get => _algoCurrent;
        private set {
            if (_algoCurrent == value) return;
            Vertex? prevCurrent = _algoCurrent;
            _algoCurrent = value;

            _algoCurrent?.SetEffect(VertexEffect.AlgoCurrent, true);
            prevCurrent?.SetEffect(VertexEffect.AlgoCurrent, false);
        }
    }
    public bool IsAlgoCurrent => this == AlgoCurrent;

    private int _connectedAlgoEdges = 0;
    public int ConnectedAlgoEdges {
        get => _connectedAlgoEdges;
        set {
            if (value < 0) throw new ArgumentException("ConnectedAlgoEdges can't be negative!");
            _connectedAlgoEdges = value;
            SetEffect(VertexEffect.OnAlgoPath, value > 0);
        } 
    }

    // Priority effect handling with int enum in set

    private HashSet<VertexEffect> _effects = new();
    public void AddEffect(VertexEffect e) => _effects.Add(e);
    public void RemoveEffect(VertexEffect e) => _effects.Remove(e);
    public void SetEffect(VertexEffect e, bool active) {
        if (active) AddEffect(e); else RemoveEffect(e);
    }
    public void ClearEffects() {
        _effects.RemoveWhere(ve => ve == VertexEffect.OnAlgoPath);
        ConnectedAlgoEdges = 0;
    }
    public VertexEffect? Effect => _effects.Any() ? _effects.Min() : null;
    public bool Hidden => HideButton.Instance.ButtonPressed && Effect == null;

    public Color GetColor() => Effect switch {
        VertexEffect.ManualCurrent => Colors.Red,
        VertexEffect.Solved        => Colors.Green,
        VertexEffect.Initial       => Colors.RoyalBlue,
        VertexEffect.AlgoCurrent   => Colors.Orange,
        VertexEffect.OnAlgoPath    => new Color(1, 1, 0, 0.5f),
        _                          => new Color(1, 1, 1, 0.5f),
    };

    public float GetScale() => Effect switch {
        VertexEffect.ManualCurrent => 2f,
        VertexEffect.Solved        => 2f,
        VertexEffect.Initial       => 2f,
        VertexEffect.AlgoCurrent   => 2f,
        VertexEffect.OnAlgoPath    => 1.5f,
        _                          => 1f,
    };

    // Construction
    public Vertex(RHGameState gameState) {
        GameState = gameState;
        // Initialize with solved effect if applicable
        SetEffect(VertexEffect.Solved, gameState.IsSolved());
    }

    public static Vertex GetOrCreate(RHGameState state, Vertex? parent) {
        if (Dict.TryGetValue(state, out Vertex? vertex)) {
            return vertex;
        }

        vertex = new Vertex(state);

        if (parent == null) {
            vertex.Position = Vector3.Zero;
        } else {
            var outwardUnit = parent.Position.Normalized();

            Vector3 randUnitVector = new Vector3(
                GD.Randf() - 0.5f,
                GD.Randf() - 0.5f,
                GD.Randf() - 0.5f
            ).Normalized();

            if (randUnitVector.Dot(outwardUnit) < 0) {
                randUnitVector = -randUnitVector;
            }
            vertex.Position = parent.Position + randUnitVector * Edge.springLength * spawnDistanceFactor;
        }

        Dict[state] = vertex;
        return vertex;
    }

    // Force accumulation (thread-safe)
    public void ApplyPendingForce(Vector3 force) {
        lock (_forcesLock) {
            _pendingForces += force;
        }
    }

    // Apply movement
    public void Integrate(double delta) {
        // Assume a mass of 1 unit
        Velocity += _pendingForces * (float)delta;
        _pendingForces = Vector3.Zero;

        // Apply linear damping
        Velocity *= (float)Math.Max(0, 1.0 - linearDamp * delta);

        // Clamp velocity
        Vector3 maxVel = Vector3.One * maxSpeed;
        Velocity = Velocity.Clamp(-maxVel, maxVel);

        // Integrate position
        Position += Velocity * (float)delta;
    }

    // Static event handlers for solver integration

    public static void OnNewCurrent(object? sender, RHGameState newCurrent) {
        Dict.TryGetValue(newCurrent, out Vertex? v);
        AlgoCurrent = v;
    } 

    public static void OnPathChange(object? _, PathChangeArgs args) {
        int diff = args.onPath ? 1 : -1;
        Dict[args.move.To].ConnectedAlgoEdges += diff;
        Dict[args.move.From].ConnectedAlgoEdges += diff;
    } 
}


public enum VertexEffect {
    // Listed from highest priority to lowest
    ManualCurrent = 0,
    Solved = 1,
    Initial = 2,
    AlgoCurrent = 3,
    OnAlgoPath = 4
}
namespace rushhour.src.Nodes.Nodes3D;

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class VertexDrawer : MultiMeshInstance3D {
    public const int MaxInstances = 20000;
    public const int spriteSizePx = 256;
    public const float spriteScale = 1f;

    public override void _Ready() {
        var quadMesh = new QuadMesh {
            Size = new Vector2(spriteScale, spriteScale)
        };

        // No BillboardMode here - we build the facing transform manually so
        // that per-instance scale is not overwritten by the billboard pass.
        MaterialOverride = new StandardMaterial3D() {
            AlbedoColor = Colors.White,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled, // visible from both sides
            AlbedoTexture = ResourceLoader.Load<Texture2D>("res://assets/circle.png"),
            VertexColorUseAsAlbedo = true,
        };

        Multimesh = new MultiMesh {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            InstanceCount = MaxInstances,
            VisibleInstanceCount = 0,
            Mesh = quadMesh
        };
    }

    // Updates all vertex instance transforms, colors, and scales.
    // Manually builds a camera-facing (billboard) basis per instance so that
    // scale from GetScale() is correctly preserved in the final transform.
    public void UpdateVisuals() {
        // Get the camera-facing basis once per frame.
        // The camera's basis columns are: X=right, Y=up, Z=back (towards camera).
        // A quad facing the camera needs its local X/Y to align with camera right/up.
        Basis cameraBasis = Camera3d.Instance.GlobalTransform.Basis;

        IEnumerable<Vertex> visableVertices = Vertex.Dict.Values.Where(v => !v.Hidden);
        Multimesh.VisibleInstanceCount = visableVertices.Count();

        int i = 0;
        foreach (Vertex v in visableVertices) {
            float scale = v.GetScale();
            // Scale the facing basis uniformly - this is what billboard mode prevents us doing
            Basis scaledBasis = cameraBasis.Scaled(Vector3.One * scale);
            Multimesh.SetInstanceTransform(i, new Transform3D(scaledBasis, v.Position));
            Multimesh.SetInstanceColor(i, v.GetColor());
            i++;
        }
    }
}
// UI
namespace rushhour.src.Nodes.UI;

using System;
using System.Linq;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.Board;
using rushhour.src.Nodes.Nodes3D;

public partial class AlgoPlayer : VBoxContainer {
    [Export] public HSlider slider = null!;
    [Export] public Button playPauseButton = null!;
    [Export] public Label playPauseLabel = null!;
    [Export] public Button stepButton = null!;
    [Export] public Button restartButton = null!;


    public const double minAlgoStepdelay = 0.001; 
    public const int maxStepCount = 1024; 
    private double _algoStepDelay = 0.5;
    private double _timeSinceLastStep = 0;
    public bool Running { get; private set; } = false;

    public RHSolver Solver { get; set; } = null!;

    public RHGameState? InitialState { get; private set; }


    public TabCont TabCont => GetParent<VBoxContainer>().GetParent<TabCont>();

    public static AlgoPlayer Instance {get; private set;} = null!;

    public AlgoPlayer() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of AlgoPlayer!");
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        slider.ValueChanged += OnSliderValueChanged;

        playPauseButton.Toggled += OnPlayPauseButtonToggled;
        stepButton.Pressed += OnStepButtonPressed;
        restartButton.Pressed += ResetSolver;

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (Solver.Status != SolverStatus.Running && Solver.Status != SolverStatus.Discovering) return;
        if (!Running) return;
        _timeSinceLastStep += delta;
        
        if (_timeSinceLastStep < _algoStepDelay) return;
        
        // If time between updates gets too large, execute some extra steps 
        int stepCount = Math.Clamp((int)(_timeSinceLastStep / _algoStepDelay), 1, maxStepCount);
        
        _timeSinceLastStep = 0;

        Solver.Step(stepCount);
    }

    public void LoadLevel(RHGameState level) {
        InitialState = level;

        SetupControlButtons(false);

        UnSubFromSolver();
        
        Solver = SolverSettingsTab.Instance.GetSolver();
        SubToSolver();

        MainGameBoard.Instance.Setup(level);

        GraphScene.Instance.Setup(level);

        Solver.Start(level);

        StatusContainer.Instance.SetSolutionLength(null);

        // start paused
        Running = false;

        // Switch to solver settings tab
        TabCont.CurrentTab = 1;
    }

    public void SetupControlButtons(bool startPlaying) {
        playPauseButton.Disabled = false;
        stepButton.Disabled = false;
        restartButton.Disabled = false;
        
        playPauseButton.ButtonPressed = startPlaying;
        if (!startPlaying) {
            playPauseLabel.Text = "Start";
        }

        SolverSettingsTab.Instance.bfsSearchButton.Disabled = false;
        SolverSettingsTab.Instance.dfsSearchButton.Disabled = false;
    }

    public void SubToSolver() {
        Solver.NewEdge += Edge.OnNewEdge;
        Solver.PathChange += Edge.OnPathChange;
        Solver.PathChange += Vertex.OnPathChange;
        Solver.NewCurrent += Vertex.OnNewCurrent;
        Solver.NewCurrent += MainGameBoard.Instance.OnNewAlgoCurrent;
        Solver.NewCurrent += OnNewCurrent;
        Solver.Terminated += OnSolverTerminated;
    }


    public void UnSubFromSolver() {
        Solver.NewEdge -= Edge.OnNewEdge;
        Solver.PathChange -= Edge.OnPathChange;
        Solver.PathChange -= Vertex.OnPathChange;
        Solver.NewCurrent -= Vertex.OnNewCurrent;
        Solver.NewCurrent -= MainGameBoard.Instance.OnNewAlgoCurrent;
        Solver.NewCurrent -= OnNewCurrent;
        Solver.Terminated -= OnSolverTerminated;
    }
    public void OnNewCurrent(object? s, RHGameState _) {
        int c = Solver.StepCount;
        StatusContainer.Instance.SetStepCount(c);
        // The solver enters Running status after calling Start on it, so bypass this by checking the step count.
        StatusContainer.Instance.SetStatusLabel(Solver.Status, c);
    }
    public void OnSliderValueChanged(double value) {
        if (value == slider.MaxValue) {
            _algoStepDelay = minAlgoStepdelay;
        } else {
            _algoStepDelay = Math.Pow(2, -value);  
        }
    }

    public void OnPlayPauseButtonToggled(bool playing) {
        Running = playing;
        if (playing) {
            MainGameBoard.Instance.Mode = BoardMode.ALGO;
        }
    }

    public void OnStepButtonPressed() {
        Solver.Step();
        MainGameBoard.Instance.Mode = BoardMode.ALGO;

        // set algo mode to paused
        playPauseButton.ButtonPressed = false;
    }

    public void ResetSolver() => ResetSolver(SolverSettingsTab.Instance.GetSolver());

    public void ResetSolver(RHSolver newSolver, bool startPlaying = false) {
        if (InitialState is null) {
            throw new Exception("Can't restart with no level loaded");
        }

        // TODO maybe filter by active edges,
        // add a field to those.
        GraphScene.Instance.ClearPathHighligh();

        UnSubFromSolver();

        Solver = newSolver;
        SubToSolver();

        StatusContainer.Instance.SetSolutionLength(null);

        MainGameBoard.Instance.AlgoCurrent = InitialState;
        MainGameBoard.Instance.Mode = BoardMode.ALGO;

        Solver.Start(InitialState);

        SetupControlButtons(startPlaying);

        // Switch to the AlgoPlayer tab
        TabCont.CurrentTab = 2;
    }

    public void OnSolverTerminated(object? sender, SolverStatus status) {
        playPauseButton.Disabled = true;
        stepButton.Disabled = true;
        StatusContainer.Instance.SetStatusLabel(status);

        if (status == SolverStatus.Solved) {
            var solutionPath = Solver.GetSolutionPath();

            StatusContainer.Instance.SetSolutionLength(solutionPath.Count());

            foreach (StateMove stateMove in solutionPath) {
                Edge.Dict[stateMove].AddEffect(EdgeEffect.SolutionEdge);
            }
        }
    }
}
namespace rushhour.src.Nodes.UI;

using Godot;
using System;

public partial class HideButton : CheckButton {
    public static HideButton Instance {get; private set;} = null!;
    public HideButton() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of AlgoPlayer!");
        }
    }
}
namespace rushhour.src.Nodes.UI;


using System;
using Godot;
using rushhour.src.Model;


public partial class LevelsGrid : GridContainer
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
namespace rushhour.src.Nodes.UI;

using System;
using System.ComponentModel;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.Board;

public partial class LoadLevelButton : PanelContainer
{

    [Export] public Button button = null!;
    [Export] public GameBoard gameBoard = null!;
    [Export] public Label label = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        
    }

    public void Init(Level level) {
        label.Text = level.Title;
        gameBoard.Setup(level.State);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
namespace rushhour.src.Nodes.UI;


using System;
using Godot;

public partial class PlayPauseButton : Button
{

    [Export] CompressedTexture2D playIcon = null!;
    [Export] CompressedTexture2D pauseIcon = null!;
    [Export] Label playPauseLabel = null!;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        Toggled += OnToggled;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
    }


    public void OnToggled(bool playing) {
        Icon = playing ? pauseIcon : playIcon;
        playPauseLabel.Text = playing ? "Pause" : "Play";
    }
}
namespace rushhour.src.Nodes.UI;

using System;
using Godot;
using rushhour.src.Model;

public partial class SolverSettingsTab : VBoxContainer {
    [Export] public OptionButton algoOption = null!;
    [Export] public SpinBox tabuSizeSpin = null!;
    [Export] public OptionButton heuristicOption = null!;
    [Export] public CheckBox randomBox = null!;
    [Export] public Button applyButton = null!;
    [Export] public SpinBox searchCountBox = null!;
    [Export] public CheckButton unlimitedSearchButton = null!;
    [Export] public Button bfsSearchButton = null!;
    [Export] public Button dfsSearchButton = null!;

    
    private int _heuristicSelected;
    private float _randomFactor;
    private int _algoOptionSelected;
    private int _tabuSize;
    public int maxStatesToDiscover {get {
            if (unlimitedSearchButton.ButtonPressed) {
                return int.MaxValue;
            } else {
                return (int)searchCountBox.Value;
            }
        }
    }

    public TabCont TabCont => GetParent<TabCont>();

    public static SolverSettingsTab Instance {get; private set;} = null!;

    public SolverSettingsTab() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of SolverSettingsTab!");
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        algoOption.ItemSelected += OnAlgoOptionChanged;
        applyButton.Pressed += OnApplyButtonPressed;
        unlimitedSearchButton.Toggled += OnUnlimitedSearchButtonToggled;
        bfsSearchButton.Pressed += StartBfsSearcher;
        dfsSearchButton.Pressed += StartDfsSearcher;

        ApplySettings();
    }

    public void OnUnlimitedSearchButtonToggled(bool on) => searchCountBox.Editable = !on;
    public void StartBfsSearcher() {
        // assume a level is loaded
        AlgoPlayer.Instance.ResetSolver(new BFSDiscoverer(maxStatesToDiscover), true);
        Slider s = AlgoPlayer.Instance.slider;
        s.Value = s.MaxValue - 1; 
    }
    public void StartDfsSearcher() {
        // assume a level is loaded
        AlgoPlayer.Instance.ResetSolver(new DFSDiscoverer(maxStatesToDiscover), true);
        Slider s = AlgoPlayer.Instance.slider;
        s.Value = s.MaxValue - 1; 
    }

    public void ApplySettings() {
        _heuristicSelected = heuristicOption.Selected;
        _randomFactor = randomBox.ButtonPressed ? 1f : 0f;
        _algoOptionSelected = algoOption.Selected;
        _tabuSize = (int)tabuSizeSpin.Value;
    }

    public void OnApplyButtonPressed() {
        ApplySettings();
        if (AlgoPlayer.Instance.InitialState == null) {
            // if no level has been selected, 
            // switch to the levels tab, so the user selects a level
            TabCont.CurrentTab = 0;
        } else {
            AlgoPlayer.Instance.ResetSolver();
        }
    }

    public void OnAlgoOptionChanged(long index) {
        tabuSizeSpin.GetParent<HBoxContainer>().Visible = index == 0;
    }

    public RHSolver GetSolver() {
        Heuristic<RHGameState> h = heuristicOption.Selected switch {
            0 => new NullHeuristic(),
            1 => new DistanceHeuristic(),
            2 => new FreeSpacesHeuristic(),
            3 => new MoverHeuristic(),
            _ => throw new Exception("Invalid heuristic option selected")
        };
        RHSolver s;
        switch (algoOption.Selected) {
            case 0:             
                s = new TabuSolver(h, _tabuSize, _randomFactor);
                break;
            case 1:             
                s = new BacktrackingSolver(h, _randomFactor);
                break;
            case 2:
                if (h is not MonotoneHeuristic<RHGameState> mh) {
                    throw new ArgumentException("Monotone heuristic must be selected for AcGraphSolver!");
                }
                s = new AcGraphSolver(mh, _randomFactor);
                break;
            default:
                throw new Exception("Invalid algorithm option selected");
        }
        return s;
    }
}
namespace rushhour.src.Nodes.UI;

using System;
using Godot;
using rushhour.src.Model;

public partial class SolverSettingsTab : VBoxContainer {
    [Export] public OptionButton algoOption = null!;
    [Export] public SpinBox tabuSizeSpin = null!;
    [Export] public OptionButton heuristicOption = null!;
    [Export] public CheckBox randomBox = null!;
    [Export] public Button applyButton = null!;
    [Export] public SpinBox searchCountBox = null!;
    [Export] public CheckButton unlimitedSearchButton = null!;
    [Export] public Button bfsSearchButton = null!;
    [Export] public Button dfsSearchButton = null!;

    
    private int _heuristicSelected;
    private float _randomFactor;
    private int _algoOptionSelected;
    private int _tabuSize;
    public int maxStatesToDiscover {get {
            if (unlimitedSearchButton.ButtonPressed) {
                return int.MaxValue;
            } else {
                return (int)searchCountBox.Value;
            }
        }
    }

    public TabCont TabCont => GetParent<TabCont>();

    public static SolverSettingsTab Instance {get; private set;} = null!;

    public SolverSettingsTab() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of SolverSettingsTab!");
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        algoOption.ItemSelected += OnAlgoOptionChanged;
        applyButton.Pressed += OnApplyButtonPressed;
        unlimitedSearchButton.Toggled += OnUnlimitedSearchButtonToggled;
        bfsSearchButton.Pressed += StartBfsSearcher;
        dfsSearchButton.Pressed += StartDfsSearcher;

        ApplySettings();
    }

    public void OnUnlimitedSearchButtonToggled(bool on) => searchCountBox.Editable = !on;
    public void StartBfsSearcher() {
        // assume a level is loaded
        AlgoPlayer.Instance.ResetSolver(new BFSDiscoverer(maxStatesToDiscover), true);
        Slider s = AlgoPlayer.Instance.slider;
        s.Value = s.MaxValue - 1; 
    }
    public void StartDfsSearcher() {
        // assume a level is loaded
        AlgoPlayer.Instance.ResetSolver(new DFSDiscoverer(maxStatesToDiscover), true);
        Slider s = AlgoPlayer.Instance.slider;
        s.Value = s.MaxValue - 1; 
    }

    public void ApplySettings() {
        _heuristicSelected = heuristicOption.Selected;
        _randomFactor = randomBox.ButtonPressed ? 1f : 0f;
        _algoOptionSelected = algoOption.Selected;
        _tabuSize = (int)tabuSizeSpin.Value;
    }

    public void OnApplyButtonPressed() {
        ApplySettings();
        if (AlgoPlayer.Instance.InitialState == null) {
            // if no level has been selected, 
            // switch to the levels tab, so the user selects a level
            TabCont.CurrentTab = 0;
        } else {
            AlgoPlayer.Instance.ResetSolver();
        }
    }

    public void OnAlgoOptionChanged(long index) {
        tabuSizeSpin.GetParent<HBoxContainer>().Visible = index == 0;
    }

    public RHSolver GetSolver() {
        Heuristic<RHGameState> h = heuristicOption.Selected switch {
            0 => new NullHeuristic(),
            1 => new DistanceHeuristic(),
            2 => new FreeSpacesHeuristic(),
            3 => new MoverHeuristic(),
            _ => throw new Exception("Invalid heuristic option selected")
        };
        RHSolver s;
        switch (algoOption.Selected) {
            case 0:             
                s = new TabuSolver(h, _tabuSize, _randomFactor);
                break;
            case 1:             
                s = new BacktrackingSolver(h, _randomFactor);
                break;
            case 2:
                if (h is not MonotoneHeuristic<RHGameState> mh) {
                    throw new ArgumentException("Monotone heuristic must be selected for AcGraphSolver!");
                }
                s = new AcGraphSolver(mh, _randomFactor);
                break;
            default:
                throw new Exception("Invalid algorithm option selected");
        }
        return s;
    }
}
namespace rushhour.src.Nodes.UI;


using Godot;
using rushhour.src.Model;
using System;

public partial class StatusContainer : VBoxContainer
{
    public static StatusContainer Instance {get; private set;} = null!;
    public StatusContainer() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of StatusContainer!");
        }
    }

    public Label StepCountLabel => GetChild<Label>(0);
    public Label SolverStatusLabel => GetChild<Label>(1);
    public Label SolutionLengthLabel => GetChild<Label>(2);
    public Label HeuristicLabel => GetChild<Label>(3);
    
    
    // Called when the node enters the scene tree for the first time.

    public void SetStatusLabel(SolverStatus? status, int? stepCount = null) {
        switch (status) {
            case null:
                SolverStatusLabel.Text = "Solver status: -";
                break;
            case SolverStatus.NotStarted:
                SolverStatusLabel.Text = "Solver status: Initiated";
                break;
            case SolverStatus.Running:
                if (stepCount == 0) {
                    SolverStatusLabel.Text = "Solver status: Initiated";
                } else {
                    SolverStatusLabel.Text = "Solver status: Running";
                }
                break;
            case SolverStatus.Solved:
                SolverStatusLabel.Text = "Solver status: Solved";
                break;
            case SolverStatus.NoSolution:
                SolverStatusLabel.Text = "Solver status: No solution";
                break;
            case SolverStatus.Terminated:
                SolverStatusLabel.Text = "Solver status: Stuck in dead end";
                break;
            case SolverStatus.Discovering:
                SolverStatusLabel.Text = "Discover status: Discovering states";
                break;
            case SolverStatus.DiscoverEndAllFound:
                SolverStatusLabel.Text = "Discover status: All states discovered";
                break;
            case SolverStatus.DiscoverEndLimitReached:
                SolverStatusLabel.Text = "Discover status: State limit reached";
                break;
        }
    }

    public void SetStepCount(int? stepCount) {
        StepCountLabel.Text = $"Step count: {stepCount?.ToString() ?? "-"}";
    }

    public void SetSolutionLength(int? solutionLength) {
        SolutionLengthLabel.Text = $"Solution length: {solutionLength?.ToString() ?? "-"}";
    }

    public void SetHeuristicLabel(int? heuristicScore) {
        HeuristicLabel.Text = $"Heuristic score: {heuristicScore?.ToString() ?? "-"}";
    }
    public void UpdateHeuristicLabel(RHGameState state) {
        SetHeuristicLabel(AlgoPlayer.Instance.Solver?.Heuristic.Evaluate(state));
    }
}
namespace rushhour.src.Nodes.UI;

using Godot;

public partial class TabCont : TabContainer {

    public SolverSettingsTab SolverSettingsTab => GetChild<SolverSettingsTab>(1);
    public AlgoPlayer AlgoPlayer => GetChild<VBoxContainer>(2).GetChild<AlgoPlayer>(0);

    public override void _Ready() {
        AlgoPlayer.Solver = SolverSettingsTab.GetSolver();
    }
}
