namespace rushhour.src.Nodes;

using Godot;
using rushhour.src.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

public partial class MainScene : Control {
	// Called when the node enters the scene tree for the first time.

	// 1. Export a PackedScene variable so you can drag-and-drop your .tscn file in the Inspector
    // [Export]
    public PackedScene NodeCreator = ResourceLoader.Load<PackedScene>("res://scenes/vertex.tscn");
	Random random = new Random();

	double time = 0;

	BacktrackingSolver solver;
	public override void _Ready(){
		string version = System.Environment.Version.ToString();
		GD.Print("🚀 C# is working!");
		GD.Print($"System .NET Version: {version}");
		
		// Let's also change the background color to prove it's running
		RenderingServer.SetDefaultClearColor(Colors.MediumPurple);

		RHGameState lvl = Levels.Level0();
		lvl.PrintState();

		// solver = new HillClimberSolver(new DistanceHeuristic(), lvl);
		solver = new BacktrackingSolver(new DistanceHeuristic(), lvl);

		

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
		if (time < 3) {	
			return;
		} else {
			time = 0;
			Node2D vertex = (Node2D)NodeCreator.Instantiate();
			GD.Print(vertex);
			AddChild(vertex);
			vertex.Position = new Vector2(random.Next(100,500), random.Next(100,500));
		}
		// if (!solver.Terminated){
		// 	solver.Current.PrintState();
		// 	solver.Step();
		// }
	}
}
