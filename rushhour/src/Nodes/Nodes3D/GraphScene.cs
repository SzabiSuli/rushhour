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
    private const float PickRadiusPx = 40f;

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
        // Stage 1: OctTree Build
        OctTree.BuildAndSetCurrent(Vertex.Dict.Values);
        var tree = OctTree.GetCurrent();

        // Stage 2: Force Computation (parallel)

        // Vertex repulsion via Barnes-Hut
        var vertices = Vertex.Dict.Values.ToArray();
        var edges = Edge.Dict.Values.ToArray();

        Parallel.ForEach(vertices, v => {
            if (tree != null) {
                var force = OctTree.ComputeForce(tree, v, OctTree.Theta);
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
