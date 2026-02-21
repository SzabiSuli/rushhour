using Godot;
using rushhour.src;
using System;

public partial class Node2d : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		string version = System.Environment.Version.ToString();
		GD.Print("🚀 C# is working!");
		GD.Print($"System .NET Version: {version}");
		
		// Let's also change the background color to prove it's running
		RenderingServer.SetDefaultClearColor(Colors.MediumPurple);

		RHGameState lvl = Levels.Level1();
		lvl.PrintState();
		
		foreach (var move in lvl.GetPossibleMoves()){
			var newState = lvl.WithMove(move);
			// do something with newState
			newState.PrintState();
		}

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
