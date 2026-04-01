using rushhour.src.Nodes.Nodes3D;

using System;
using System.Collections.Generic;
using Godot;
using rushhour.src.Model;

public partial class GraphScene : Node3D
{
    public static GraphScene Instance {get; private set;} = null!;

    public GraphScene() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of GraphScene!");
        }
    }

    public async override void _PhysicsProcess(double delta) {
        // Rebuild the Barnes-Hut OctTree once per physics update for repulsion forces
        var vertexNodes = GetTree().GetNodesInGroup("Vertices");
        var vertexList = new List<Vertex>();
        foreach (var v in vertexNodes) vertexList.Add((Vertex)v);
        OctTree.BuildAndSetCurrent(vertexList);
    }

    public void Setup(RHGameState initial) {
        Clear();

        // create the initial state
        Vertex.GetOrCreate(initial, null);
    }

    public void Clear() {
        foreach (Node child in GetChildren()) {
            if (child is Edge || child is Vertex) {
                child.QueueFree();
            }
        }
        Edge.Dict.Clear();
        Vertex.Dict.Clear();
        Vertex.current = null;
    }
}
