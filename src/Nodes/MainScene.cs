namespace rushhour.src.Nodes;

using Godot;
using rushhour.src.Model;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MainScene : Control {
	[Export] public GameBoard gameBoard = null!;

	public static MainScene Instance {get; private set;} = null!;

	Random random = new Random();

	double time = 0;

	private RHGameState? _current;


	BacktrackingSolver solver = null!;
	public override void _Ready(){
		Instance = this;

		string version = System.Environment.Version.ToString();
		GD.Print("🚀 C# is working!");
		GD.Print($"System .NET Version: {version}");
		
		// Let's also change the background color to prove it's running
		RenderingServer.SetDefaultClearColor(Colors.Black);

		var (title, lvl) = Levels.LoadLevel(5);

		GD.Print(title);
		lvl.PrintState();

		// solver = new BacktrackingSolver(new DistanceHeuristic());
		// solver = new BacktrackingSolver(new FreeSpacesHeuristic());
		solver = new BacktrackingSolver(new MoverHeuristic());

		solver.PathChange += OnPathChange;
		solver.NewCurrent += OnNewCurrent;
		solver.NewCurrent += gameBoard.DisplayState;
		solver.DiscoveredEdges += OnDiscoveredEdges;

		// We have to create the first vertex
		GetOrCreateVertex(lvl, null);

		solver.Start(lvl);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public async override void _Process(double delta) {
		time += delta;
		if (time < 1) {	
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

	public Vertex GetOrCreateVertex(RHGameState state, Vertex? parent) {
		if (Vertex.Dict.TryGetValue(state, out Vertex? vertex)) {
			// GD.Print("Vertex already exists");
			return vertex;
		}

		// GD.Print("Creating vertex");

		vertex = Vertex.Creator.Instantiate<Vertex>();
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
		AddChild(vertex);
		vertex.AddToGroup("Vertices");
		Vertex.Dict[state] = vertex;
		return vertex;
	}

	public Edge GetOrCreateEdge(StateMove move) {
		if (Edge.Dict.TryGetValue(move, out Edge? edge)) {
			return edge;
		}

		edge = Edge.Creator.Instantiate<Edge>();
		edge.Init(
			Vertex.Dict[move.From], 
			Vertex.Dict[move.To], 
			move
		);
		AddChild(edge);
		edge.AddToGroup("Edges");
		Edge.Dict[move] = edge;

		return edge;
	}	

	public void OnPathChange(object? sender, PathChangeArgs args) {
		Edge.Dict[args.move].UpdateColor(args.onPath); 
	}

	public void OnNewCurrent(object? sender, RHGameState newCurrent) {
		if (_current == newCurrent) return;
		if (_current is not null) {
			Vertex.Dict[_current].UpdateColor(false);
		}

		Vertex.Dict[newCurrent].UpdateColor(true);
		_current = newCurrent;
	}

	public void OnDiscoveredEdges(object? sender, IEnumerable<StateMove> edges) {
		// Assume list is not empty

		// assume the vertex extended already exists, find it
		Vertex from = Vertex.Dict[edges.First().From];

		foreach (var edge in edges) {
			GetOrCreateVertex(edge.To, from);
			GetOrCreateEdge(edge);
		}
	}
}
