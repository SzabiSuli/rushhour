namespace rushhour.src;

using Godot;
using rushhour.src;
using System;

public partial class Node2d : Node2D
{
	// Called when the node enters the scene tree for the first time.

	HillClimberSolver solver;
	public override void _Ready(){
		string version = System.Environment.Version.ToString();
		GD.Print("🚀 C# is working!");
		GD.Print($"System .NET Version: {version}");
		
		// Let's also change the background color to prove it's running
		RenderingServer.SetDefaultClearColor(Colors.MediumPurple);

		RHGameState lvl = Levels.Level0();
		lvl.PrintState();

		solver = new HillClimberSolver(new DistanceHeuristic(), lvl);

		

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
	public override void _Process(double delta) {
		if (!solver.Terminated){
			solver.Current.PrintState();
			solver.Step();
		}
	}
}
