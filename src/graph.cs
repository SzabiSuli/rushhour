namespace rushhour;

using Godot;
using System.Collections.Generic;

public class GraphNode
{
    // Position for displaying on the gui
    public Vector2 Position { get; set; }

    public GameState State { get; }
    public List<GraphEdge> Edges { get; set; } = new List<GraphEdge>();
    public GraphNode Parent { get; set; }

    public GraphNode(Vector2 position, GameState state)
    {
        Position = position;
        State = state;
    }


    public void AddEdge(GraphEdge edge)
    {
        Edges.Add(edge);
    }

    public override bool Equals(object obj)
    {
        // Check for null or different types
        if (obj == null || GetType() != obj.GetType())
            return false;

        GraphNode other = (GraphNode)obj;
        return State.Equals(other.State);
    }

    public override int GetHashCode()
    {
        return State.GetHashCode();
    }
}

public class StateGraph
{
    public Dictionary<int, GraphNode> Nodes { get; } = new Dictionary<int, GraphNode>();

    public void AddState(GameState state)
    {
        // Implementation hidden
    }
}