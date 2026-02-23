namespace rushhour.src;

using Godot;
using System.Collections.Generic;
using System.Dynamic;
using rushhour.src.Model;

public class GraphNode {
	// Position for displaying on the gui
	// public Vector2 Position { get; set; }

	public GameState State { get; }
	public List<GraphEdge> Edges { get; set; } = new List<GraphEdge>();
	public GraphNode Parent { get; set; }

	public GraphNode(Vector2 position, GameState state) {
		// Position = position;
		State = state;
	}


	public void AddEdge(GraphEdge edge) {
		Edges.Add(edge);
	}

	public override bool Equals(object obj) {
		// Check for null or different types
		if (obj == null || GetType() != obj.GetType())
			return false;

		GraphNode other = (GraphNode)obj;
		return State.Equals(other.State);
	}

	public override int GetHashCode() {
		return State.GetHashCode();
	}
}

public class StateGraph {
	public Dictionary<RHGameState, GraphNode> Nodes { get; } = new ();
	// public HashSet<GraphNode> Nodes {get; } = new();

	public void AddState(RHGameState fromState, Move move) {
		RHGameState toState = fromState.WithMove(move);

		GraphNode fromNode = Nodes[fromState];

		// GraphNode toNode = new GraphNode(

		// )

		



		// GraphEdge edge = new GraphEdge {
		//     From = fromState,
		//     MoveUsed = move,
		//     To = to
		// }
	}
}
