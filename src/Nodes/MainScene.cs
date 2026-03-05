namespace rushhour.src.Nodes;

using Godot;
using rushhour.src.Model;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MainScene : Control {
	// Called when the node enters the scene tree for the first time.

	// 1. Export a PackedScene variable so you can drag-and-drop your .tscn file in the Inspector
	// [Export]

	public PackedScene VertexCreator = ResourceLoader.Load<PackedScene>("res://scenes/vertex.tscn");
	public PackedScene EdgeCreator = ResourceLoader.Load<PackedScene>("res://scenes/edge.tscn");
	Random random = new Random();

	double time = 0;

	private RHGameState? _current;


	BacktrackingSolver solver = null!;
	public override void _Ready(){
		string version = System.Environment.Version.ToString();
		GD.Print("🚀 C# is working!");
		GD.Print($"System .NET Version: {version}");
		
		// Let's also change the background color to prove it's running
		RenderingServer.SetDefaultClearColor(Colors.Black);

		// RHGameState lvl = Levels.Level0();
		RHGameState lvl = Levels.Level1();
		// RHGameState lvl = Levels.TestLevel();
		// RHGameState lvl = Levels.TestLevel2();
		// RHGameState lvl = Levels.TestLevel3();
		lvl.PrintState();

		// solver = new HillClimberSolver(new DistanceHeuristic(), lvl);
		solver = new BacktrackingSolver(new DistanceHeuristic(), this);

		solver.PathChange += OnPathChange;
		solver.NewCurrent += OnNewCurrent;
		solver.DiscoveredEdges += OnDiscoveredEdges;

		solver.Start(lvl);

		

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
		// Rebuild the Barnes-Hut OctTree once per frame for repulsion forces
		var vertexNodes = GetTree().GetNodesInGroup("Vertices");
		var vertexList = new List<Vertex>();
		foreach (var v in vertexNodes) vertexList.Add((Vertex)v);
		OctTree.BuildAndSetCurrent(vertexList);

		time += delta;
		if (time < 0.01) {	
			return;
		} else {
			time = 0;
			// Node2D vertex = (Node2D)VertexCreator.Instantiate();
			// GD.Print(vertex);
			// AddChild(vertex);
			// vertex.Position = new Vector2(random.Next(100,500), random.Next(100,500));
		}
		if (!solver.Terminated){
			// solver.Current?.PrintState();
			solver.Step();
		}
	}

	public Dictionary<RHGameState, Vertex> VertexDict { get; } = new();
	// public Dictionary<(RHGameState, RHGameState), Edge> EdgeDict { get; } = new();
	
	// TODO move this to Edge as a static method and use VertexDict to get the vertices
	public Dictionary<StateMove, Edge> EdgeDict { get; } = new();

	public Vertex GetOrCreateVertex(RHGameState state, Vertex? parent) {
		if (VertexDict.TryGetValue(state, out Vertex? vertex)) {
			// GD.Print("Vertex already exists");
			return vertex;
		}

		// GD.Print("Creating vertex");

		vertex = VertexCreator.Instantiate<Vertex>();
		vertex.Init(state);

		// TODO set position based on heuristic value
		if (parent != null) {
			// Place the vertex outwards
			// TODO tweak this

			Vector3 randUnitVector = new Vector3(
				GD.Randf() - 0.5f,
				GD.Randf() - 0.5f,
				GD.Randf() - 0.5f
			).Normalized();

			var outwardUnit = parent.Position.Normalized();

			if (randUnitVector.Dot(outwardUnit) < 0)
				randUnitVector = -randUnitVector;


			vertex.Position = parent.Position + randUnitVector * Edge.springLength;
		} else {
			vertex.Position = Vector3.Zero;		
		}

		// TODO add label with state info
		// vertex.GetNode<Label>("Label").Text = state.ToString();
		AddChild(vertex);
		vertex.AddToGroup("Vertices");
		VertexDict[state] = vertex;
		return vertex;
	}

	// public Edge GetOrCreateEdge(RHGameState from, RHGameState to, Move moveUsed) {
	// 	Edge edge = EdgeCreator.Instantiate<Edge>();
	// 	edge.Init(VertexDict[from], VertexDict[to], moveUsed);
	// 	AddChild(edge);
	// 	return edge;
	// }	

	// Looks like a hash code error is not creating the edges
	// TODO CONTINUE HERE 
	public Edge GetOrCreateEdge(StateMove move) {
		if (EdgeDict.TryGetValue(move, out Edge? edge)) {
			// GD.Print("Edge already exists");
			return edge;
		}
		// GD.Print("Creating edge");


		edge = EdgeCreator.Instantiate<Edge>();
		edge.Init(
			VertexDict[move.From], 
			VertexDict[move.To], 
			move
		);
		AddChild(edge);
		edge.AddToGroup("Edges");
		EdgeDict[move] = edge;

		return edge;
	}	

	public void OnPathChange(object? sender, PathChangeArgs args) {
		EdgeDict[args.move].UpdateColor(args.onPath); 
	}

	public void OnNewCurrent(object? sender, RHGameState newCurrent) {
		if (_current == newCurrent) return;
		if (_current is not null) {
			VertexDict[_current].UpdateColor(false);
		}

		VertexDict[newCurrent].UpdateColor(true);
		_current = newCurrent;
	}

	public void OnDiscoveredEdges(object? sender, List<StateMove> edges) {
		if (edges.Count == 0) return;

		// assume the vertex extended already exists, find it
		Vertex from = VertexDict[edges.First().From];

		foreach (var edge in edges) {
			// TODO unify the methods
			Vertex to = GetOrCreateVertex(edge.To, from);
			GetOrCreateEdge(edge);
		}

		// TODO godot node creation logic
		// move to MainScene

		// CurrentRoute.Add(new (state, neighbours.Select(x => x.Item2).ToList()));
		// Vertex from = MainScene.GetOrCreateVertex(state, parent);
		// foreach (var move_and_state in neighbours){
		// 	Vertex to = MainScene.GetOrCreateVertex(move_and_state.Item2, from);
		// 	MainScene.GetOrCreateEdge(new StateMove(
		// 		from.GameState,
		// 		to.GameState,
		// 		new Move() // TODO create correct move
		// 	));
		// }
	}
}
