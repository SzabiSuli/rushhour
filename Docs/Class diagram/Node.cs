
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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


public partial class Edge : MeshInstance3D
{
    // Physics constants
    public const int springLength = 5;
    public const double optimalIntervalLowerBound = springLength * 0.9;
    public const double optimalIntervalUpperBound = springLength * 1.1;
    public const float springForce = 10;

    // Init must be called for initialization
    public Vertex From { get; set; } = null!;
    public Vertex To { get; set; } = null!;
    public StateMove MoveUsed { get; set; } = null!;
    public const String scenePath = "res://scenes/edge.tscn";
    public static PackedScene Creator { get; } = 
        ResourceLoader.Load<PackedScene>(scenePath);

    public static Dictionary<StateMove, Edge> Dict { get; } = new();

    private ImmediateMesh _mesh = new ImmediateMesh(); 

    public static Edge GetOrCreate(StateMove move) {
        if (Dict.TryGetValue(move, out Edge? edge)) {
            // GD.Print("returning already existing edge");
            return edge;
        }

        // GD.Print("createing edge");

        edge = Creator.Instantiate<Edge>();
        edge.Init(
            Vertex.Dict[move.From], 
            Vertex.Dict[move.To], 
            move
        );
        MainScene.Instance.GraphScene.AddChild(edge);
        edge.AddToGroup("Edges");
        Dict[move] = edge;

        return edge;
    }

    public static void OnNewEdge(object? sender, StateMove edge) {
        // assume the vertex extended already exists, find it
        Vertex from = Vertex.Dict[edge.From];
        Vertex.GetOrCreate(edge.To, from);
        
        GetOrCreate(edge);
    }
    
    public static void OnDiscoveredEdges(object? sender, IEnumerable<StateMove> edges) {
        if (!edges.Any()) {
            return;
        }

        foreach (var edge in edges) {
            if (Vertex.Dict.TryGetValue(edge.To, out Vertex? v)) {
                Edge.GetOrCreate(edge);
            }
        }
    }

    public static void OnPathChange(object? sender, PathChangeArgs args) {
        Dict[args.move].UpdateColor(args.onPath); 
    }

    public void Init(Vertex form, Vertex to, StateMove moveUsed){
        From = form;
        To = to;
        MoveUsed = moveUsed;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready(){
        Mesh = _mesh;
        MaterialOverride = new StandardMaterial3D() {
            AlbedoColor = Colors.White,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            // Metallic = 0.5f,
            // Roughness = 0.5f
        };
    }

    // Called every physics frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta) {
        // TODO don't update this every frame
		UpdateLine(From.Position, To.Position);

		ApplySpringForce();
	}

	public void UpdateLine(Vector3 from, Vector3 to) {
		_mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        _mesh.SurfaceAddVertex(from);
        _mesh.SurfaceAddVertex(to);
        _mesh.SurfaceEnd();
	}

	public void UpdateColor(bool onPath) {
		// change color to yellow
		// TODO maybe make new material instead?
		if (onPath) {
			MaterialOverride.Set("albedo_color", Colors.Yellow);
		} else {
			MaterialOverride.Set("albedo_color", Colors.White);
		}
	}

	public void ApplySpringForce() {
		Vector3 distanceVector = To.Position - From.Position;
		var length = distanceVector.Length();
		if (optimalIntervalLowerBound < length && length < optimalIntervalUpperBound) {
			// Spring is close to the optimal lenght, skip applying force for performance
			return;
		}
		Vector3 force = distanceVector * ((length - springLength) / length) * springForce;

		From.ApplyCentralForce(force);
		To.ApplyCentralForce(-force);
	}
}

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
public partial class MainScene : Control {
    [Export] public GameBoard gameBoard = null!;
    [Export] public Node3D GraphScene = null!;

    public static MainScene Instance {get; private set;} = null!;

    Random random = new Random();

    double time = 0;

    Solver solver = null!;
    public override void _Ready(){
        Instance = this;

        string version = System.Environment.Version.ToString();
        GD.Print("🚀 C# is working!");
        GD.Print($"System .NET Version: {version}");
        
        // Let's also change the background color to prove it's running
        RenderingServer.SetDefaultClearColor(Colors.Black);

        var (title, lvl) = Levels.LoadLevel(6);

        GD.Print(title);
        lvl.PrintState();

        // solver = new BacktrackingSolver(new DistanceHeuristic());
        // solver = new BacktrackingSolver(new FreeSpacesHeuristic());
        // solver = new BacktrackingSolver(new MoverHeuristic(), 10);
        solver = new AcGraphSolver(new MoverHeuristic());

        solver.NewEdge += Edge.OnNewEdge;
        solver.PathChange += Edge.OnPathChange;
        solver.DiscoveredEdges += Edge.OnDiscoveredEdges;
        solver.NewCurrent += Vertex.OnNewCurrent;
        solver.NewCurrent += gameBoard.DisplayState;

        // We have to create the first vertex
        Vertex.GetOrCreate(lvl, null);

        solver.Start(lvl);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public async override void _Process(double delta) {
        time += delta;
        if (time < 0.1) {	
            return;
        } else {
            time = 0;
        }
        if (solver.Status == SolverStatus.Running){
            // solver.Current?.PrintState();
            solver.Step();
        }
    }

    public async override void _PhysicsProcess(double delta) {
        // Rebuild the Barnes-Hut OctTree once per physics update for repulsion forces
        var vertexNodes = GetTree().GetNodesInGroup("Vertices");
        var vertexList = new List<Vertex>();
        foreach (var v in vertexNodes) vertexList.Add((Vertex)v);
        OctTree.BuildAndSetCurrent(vertexList);
    }
}

/// <summary>
/// Barnes-Hut OctTree for O(n log n) repulsion force approximation in 3D.
/// Rebuilt every frame from the current vertex positions.
/// </summary>
public static class OctTree
{
    /// <summary>
    /// Barnes-Hut approximation parameter. Higher values = faster but less accurate.
    /// Typical range: 0.5 (accurate) to 1.5 (fast). 0.8 is a good default.
    /// </summary>
    public const float Theta = 1f;

    /// <summary>
    /// The OctTree built for the current frame. Set by BuildAndSetCurrent().
    /// </summary>
    private static OctTreeNode? _current;

    public static OctTreeNode? GetCurrent() => _current;

    /// <summary>
    /// Builds the OctTree from the given vertices and stores it as the current tree.
    /// Should be called once per frame in MainScene._Process(), before vertices process.
    /// </summary>
    public static void BuildAndSetCurrent(List<Vertex> vertices)
    {
        if (vertices.Count == 0)
        {
            _current = null;
            return;
        }

        _current = Build(vertices);
    }

    /// <summary>
    /// Builds an OctTree from a list of vertices.
    /// </summary>
    public static OctTreeNode Build(List<Vertex> vertices)
    {
        // Compute bounding box enclosing all vertices with padding
        Vector3 min = vertices[0].Position;
        Vector3 max = vertices[0].Position;

        for (int i = 1; i < vertices.Count; i++)
        {
            Vector3 pos = vertices[i].Position;
            min = min.Min(pos);
            max = max.Max(pos);
        }

        // TODO is this necessary?
        // Add small padding so no vertex sits exactly on the boundary
        Vector3 padding = (max - min) * 0.01f + Vector3.One * 0.1f;
        min -= padding;
        max += padding;

        // Make the bounding box cubic (equal side lengths) for uniform subdivision
        float maxSide = Mathf.Max(max.X - min.X, Mathf.Max(max.Y - min.Y, max.Z - min.Z));
        Vector3 center = (min + max) / 2f;
        Vector3 halfExtent = Vector3.One * (maxSide / 2f);
        min = center - halfExtent;
        max = center + halfExtent;

        OctTreeNode root = new OctTreeNode(min, max);

        foreach (var vertex in vertices)
        {
            Insert(root, vertex);
        }

        return root;
    }

    /// <summary>
    /// Inserts a vertex into the OctTree, subdividing as needed.
    /// </summary>
    private static void Insert(OctTreeNode node, Vertex vertex)
    {
        if (node.Count == 0)
        {
            // Empty node — place the vertex here as a leaf
            node.Body = vertex;
            node.CenterOfMass = vertex.Position;
            node.Count = 1;
            return;
        }

        if (node.IsLeaf)
        {
            // This leaf already has a body — subdivide
            Vertex existing = node.Body!;
            node.Body = null;

            // Re-insert the existing body into a child
            int existingOctant = node.GetOctant(existing.Position);
            node.EnsureChild(existingOctant);
            Insert(node.Children[existingOctant]!, existing);

            // Insert the new body into a child
            int newOctant = node.GetOctant(vertex.Position);
            node.EnsureChild(newOctant);
            Insert(node.Children[newOctant]!, vertex);

            // Update this node's aggregate data
			node.CenterOfMass = (existing.Position + vertex.Position) / 2f;
			node.Count = 2;
			return;
		}

		// Internal node — insert into the appropriate child
		int octant = node.GetOctant(vertex.Position);
		node.EnsureChild(octant);
		Insert(node.Children[octant]!, vertex);

		// Update aggregate: running center of mass and count
		node.CenterOfMass = (node.CenterOfMass * node.Count + vertex.Position) / (node.Count + 1);
		node.Count++;
	}

	/// <summary>
	/// Computes the approximate repulsion force on a target vertex using the Barnes-Hut criterion.
	/// Returns the total force vector to be applied.
	/// </summary>
	public static Vector3 ComputeForce(OctTreeNode node, Vertex target, float theta)
	{
		if (node.Count == 0)
			return Vector3.Zero;

		// Leaf node with a single body
		if (node.IsLeaf)
		{
			if (node.Body == target)
				return Vector3.Zero;

			return ComputeDirectForce(target.Position, node.CenterOfMass, 1);
		}

		// Internal node — check Barnes-Hut criterion
		Vector3 diff = node.CenterOfMass - target.Position;
		float distance = diff.Length();

		if (distance < 0.001f)
		{
			// Too close to center of mass — recurse into children to avoid division issues
			return RecurseChildren(node, target, theta);
		}

		float cellSize = node.BoundsMax.X - node.BoundsMin.X; // Cubic, so any axis works
		float ratio = cellSize / distance;

		if (ratio < theta)
		{
			// Cell is far enough — approximate the entire cluster as one body
			return ComputeDirectForce(target.Position, node.CenterOfMass, node.Count);
		}

		// Cell is too close — recurse into children
		return RecurseChildren(node, target, theta);
	}

	private static Vector3 RecurseChildren(OctTreeNode node, Vertex target, float theta)
	{
		Vector3 totalForce = Vector3.Zero;
		for (int i = 0; i < 8; i++)
		{
			if (node.Children[i] != null)
			{
				totalForce += ComputeForce(node.Children[i]!, target, theta);
			}
		}
		return totalForce;
	}

	/// <summary>
	/// Computes the direct repulsion force from a body (or cluster of bodies) at sourcePos
	/// on a target at targetPos. Uses the same inverse-square law as Vertex.ApplyRepulsionForce.
	/// The mass parameter scales the force for clusters.
	/// </summary>
	private static Vector3 ComputeDirectForce(Vector3 targetPos, Vector3 sourcePos, int mass)
	{
		Vector3 distanceVector = sourcePos - targetPos;
		float distSq = distanceVector.LengthSquared();

		if (distSq > Vertex.influenceRadius * Vertex.influenceRadius)
		{
			// Beyond influence radius — skip (matches existing early-out)
			return Vector3.Zero;
		}

		if (distSq < 0.0001f)
		{
			// Prevent division by zero for overlapping vertices
			return Vector3.Zero;
		}

		float dist = Mathf.Sqrt(distSq);
		Vector3 direction = distanceVector / dist;

		// F = -repulsionForce * direction / distance² * mass
        // Negative because it's repulsion (away from source)
        return direction / distSq * (-Vertex.repulsionForce * mass);
    }
}

/// <summary>
/// A node in the Barnes-Hut OctTree, representing a cubic region of 3D space.
/// </summary>
public class OctTreeNode
{
    /// <summary>Minimum corner of the axis-aligned bounding box.</summary>
    public Vector3 BoundsMin;

    /// <summary>Maximum corner of the axis-aligned bounding box.</summary>
    public Vector3 BoundsMax;

    /// <summary>
    /// 8 children, one per octant. Null if the octant is empty or this is a leaf.
    /// Octant indexing: bit 0 = X axis, bit 1 = Y axis, bit 2 = Z axis.
    /// 0 = below midpoint, 1 = above midpoint for each axis.
    /// </summary>
    public OctTreeNode?[] Children = new OctTreeNode?[8];

    /// <summary>Weighted center of mass of all bodies in this subtree.</summary>
    public Vector3 CenterOfMass;

    /// <summary>Number of bodies contained in this subtree.</summary>
    public int Count;

    /// <summary>The single vertex stored here, if this is a leaf node. Null for internal nodes.</summary>
    public Vertex? Body;

    /// <summary>True if this node is a leaf (has a body and no children).</summary>
    public bool IsLeaf => Body != null;

    public OctTreeNode(Vector3 boundsMin, Vector3 boundsMax)
    {
        BoundsMin = boundsMin;
        BoundsMax = boundsMax;
        CenterOfMass = Vector3.Zero;
        Count = 0;
        Body = null;
    }

    /// <summary>
    /// Determines which octant (0-7) a position falls into relative to this node's center.
	/// </summary>
	public int GetOctant(Vector3 position)
	{
		Vector3 center = (BoundsMin + BoundsMax) / 2f;
		int octant = 0;
		if (position.X >= center.X) octant |= 1;
		if (position.Y >= center.Y) octant |= 2;
		if (position.Z >= center.Z) octant |= 4;
		return octant;
	}

	/// <summary>
    /// Creates the child node for the given octant if it doesn't exist yet.
    /// </summary>
    public void EnsureChild(int octant)
    {
        if (Children[octant] != null) return;

        Vector3 center = (BoundsMin + BoundsMax) / 2f;
        Vector3 childMin = BoundsMin;
        Vector3 childMax = center;

        if ((octant & 1) != 0) { childMin.X = center.X; childMax.X = BoundsMax.X; }
        if ((octant & 2) != 0) { childMin.Y = center.Y; childMax.Y = BoundsMax.Y; }
        if ((octant & 4) != 0) { childMin.Z = center.Z; childMax.Z = BoundsMax.Z; }

        Children[octant] = new OctTreeNode(childMin, childMax);
    }
}

public partial class Vertex : RigidBody3D
{
    public const int repulsionForce = 1000;
    public const int influenceRadius = 1000;
    public readonly Vector3 maxVelocity = Vector3.One * 100;
    public readonly Vector3 negMaxVelocity = Vector3.One * (-100);
    public RHGameState GameState { get; set; } = null!;
    public const String scenePath = "res://scenes/vertex.tscn";
    public static PackedScene Creator {get;} = ResourceLoader.Load<PackedScene>(scenePath);
    public static Dictionary<RHGameState, Vertex> Dict { get; } = new();
    private static RHGameState? _current;



    public static Vertex GetOrCreate(RHGameState state, Vertex? parent) {
        if (Dict.TryGetValue(state, out Vertex? vertex)) {
            // GD.Print("Vertex already exists");
            return vertex;
        }

        // GD.Print("Creating vertex");

        vertex = Creator.Instantiate<Vertex>();
        vertex.Init(state);

        if (parent == null) {
            vertex.Position = Vector3.Zero;
        } else {
            var outwardUnit = parent.Position.Normalized();

            // Place the vertex outwards
            // TODO tweak this
            Vector3 randUnitVector = new Vector3(
                GD.Randf() - 0.5f,
                GD.Randf() - 0.5f,
                GD.Randf() - 0.5f
            ).Normalized();

            if (randUnitVector.Dot(outwardUnit) < 0) {
                randUnitVector = -randUnitVector;
            }
            vertex.Position = parent.Position + randUnitVector * Edge.springLength;
        }

        // TODO add label with state info
        // vertex.GetNode<Label>("Label").Text = state.ToString();
        MainScene.Instance.GraphScene.AddChild(vertex);
        vertex.AddToGroup("Vertices");
        Dict[state] = vertex;
        return vertex;
    }

    public static void OnNewCurrent(object? sender, RHGameState newCurrent) {
        if (_current == newCurrent) return;
        if (_current is not null) {
            Dict[_current].UpdateColor(false);
        }

        Dict[newCurrent].UpdateColor(true);
        _current = newCurrent;
    }

    public void Init(RHGameState gameState) {
        GameState = gameState;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        this.InputEvent += OnInputEvent;
    }

    private void OnInputEvent(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx) {
        // Check if the event is a mouse button click
        if (@event is InputEventMouseButton mouseEvent) {
            // Specifically look for the Left Mouse Button being pressed
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed) {
                GD.Print("Object clicked at: " + eventPosition);
                HandleClick();
            }
        }
    }

    private void HandleClick() {
        GameState.PrintState();
    }

    // Put direct manipulation of Velocity here,
    // as this executes in sync with applying momentum.
    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        // Clamp velocity to prevent instability
        state.LinearVelocity = state.LinearVelocity.Clamp(negMaxVelocity, maxVelocity);

        base._IntegrateForces(state);
    }

    public override void _PhysicsProcess(double delta) {
        // Barnes-Hut approximation via OctTree (O(n log n) instead of O(n²))
        var tree = OctTree.GetCurrent();
        if (tree != null) {
            var force = OctTree.ComputeForce(tree, this, OctTree.Theta);
            ApplyCentralForce(force);
        }
    }

    public void UpdateColor(bool isCurrent) {
        var sprite = GetChild<Sprite3D>(1);
        if (isCurrent) {
            sprite.Modulate = Colors.Green;
        } else {
            sprite.Modulate = Colors.White;
        }
    }
}
