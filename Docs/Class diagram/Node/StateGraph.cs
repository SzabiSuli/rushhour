using rushhour.src.Nodes.Nodes3D;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using rushhour.src.Model;

public class GraphScene : Node3D {
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

public class VertexDrawer : MultiMeshInstance3D {
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

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.UI;


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

public class EdgeDrawer : MultiMeshInstance3D {
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
