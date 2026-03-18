namespace rushhour.src.Nodes;

using rushhour.src.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;


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
