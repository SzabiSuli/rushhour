namespace rushhour.src.Nodes;

using Godot;
using rushhour.src.Model;
using System;
using System.Collections.Generic;

public partial class MainScene : Control {
	// Called when the node enters the scene tree for the first time.

	// 1. Export a PackedScene variable so you can drag-and-drop your .tscn file in the Inspector
    // [Export]

	public PackedScene VertexCreator = ResourceLoader.Load<PackedScene>("res://scenes/vertex.tscn");
    public PackedScene EdgeCreator = ResourceLoader.Load<PackedScene>("res://scenes/edge.tscn");
	Random random = new Random();

	double time = 0;

	BacktrackingSolver solver;
	public override void _Ready(){
		string version = System.Environment.Version.ToString();
		GD.Print("🚀 C# is working!");
		GD.Print($"System .NET Version: {version}");
		
		// Let's also change the background color to prove it's running
		RenderingServer.SetDefaultClearColor(Colors.MediumPurple);

		// RHGameState lvl = Levels.Level0();
		RHGameState lvl = Levels.TestLevel();
		lvl.PrintState();

		// solver = new HillClimberSolver(new DistanceHeuristic(), lvl);
		solver = new BacktrackingSolver(new DistanceHeuristic(), lvl, this);

		

		// RHGameState other = lvl;

		// foreach (var move in lvl.GetPossibleMoves()){
		// 	other = lvl.WithMove(move);
		// 	// do something with newState
		// 	other.PrintState();
		// 	break;
		// }

		// other.PlacedPieces[2].Position += new Vector2I(0, -1);

		// GD.Print(lvl.PlacedPieces[1].Position); // Should print the original position
		// GD.Print(other.PlacedPieces[1].Position); // Should print the updated position

		// lvl.PrintState();
		// other.PrintState();



	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public async override void _Process(double delta) {
		time += delta;
		if (time < 0.0000001) {	
			return;
		} else {
			time = 0;
			// Node2D vertex = (Node2D)VertexCreator.Instantiate();
			// GD.Print(vertex);
			// AddChild(vertex);
			// vertex.Position = new Vector2(random.Next(100,500), random.Next(100,500));
		}
		if (!solver.Terminated){
			solver.Current.PrintState();
			solver.Step();
		}
	}

	public Dictionary<RHGameState, Vertex> VertexDict { get; } = new();
	// public Dictionary<(RHGameState, RHGameState), Edge> EdgeDict { get; } = new();
	public Dictionary<(Vertex, Vertex), Edge> EdgeDict { get; } = new();

	public int i = 0;
	public int j = 0;

	public Vertex GetOrCreateVertex(RHGameState state) {
        if (VertexDict.TryGetValue(state, out Vertex vertex)) {
			GD.Print("Vertex already exists");
			return vertex;
		}

        vertex = VertexCreator.Instantiate<Vertex>();
        // TODO set position based on heuristic value
        // vertex.Position = new Vector2(random.Next(0, 1000), Random.Shared.Next(0, 500));
		vertex.Position = new Vector2(i * 150, j * 150);
		if (i >= 5) {
			i = 0;
			j++;
		} else {
			i++;
		}

        // TODO add label with state info
        // vertex.GetNode<Label>("Label").Text = state.ToString();
        AddChild(vertex);
		// TODO check it does not exist
		VertexDict[state] = vertex;
        return vertex;
    }

	// public Edge GetOrCreateEdge(RHGameState from, RHGameState to, Move moveUsed) {
	// 	Edge edge = EdgeCreator.Instantiate<Edge>();
	// 	edge.Init(VertexDict[from], VertexDict[to], moveUsed);
	// 	AddChild(edge);
	// 	return edge;
	// }	
	public Edge GetOrCreateEdge(Vertex from, Vertex to, Move moveUsed) {
		if (EdgeDict.TryGetValue((from, to), out Edge edge) || EdgeDict.TryGetValue((to, from), out edge)) {
			GD.Print("Edge already exists");
			return edge;
		}
		edge = EdgeCreator.Instantiate<Edge>();
		edge.Init(from, to, moveUsed);
		AddChild(edge);
		EdgeDict[(from, to)] = edge;
		return edge;
	}	



}
