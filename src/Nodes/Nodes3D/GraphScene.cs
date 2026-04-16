using rushhour.src.Nodes.Nodes3D;

using System;
using System.Linq;
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
        OctTree.BuildAndSetCurrent(Vertex.Dict.Values);
    }

    public void Setup(RHGameState initial) {
        Clear();

        // create the initial state
        Vertex v = Vertex.GetOrCreate(initial, null);
        v.AddEffect(VertexEffect.Initial);
    }

    public void Clear() {
        foreach (Edge edge in Edge.Dict.Values) {
            edge.Free();
        }
        foreach (Vertex vertex in Vertex.Dict.Values) {
            vertex.Free();
        }
        // Vertex.Current gets reset to null by the node, which was Current, when it gets deleted.
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
