namespace rushhour.src.Nodes;

using Godot;
using System;
using rushhour.src.Model;


public partial class Edge : Line2D
{

	public Vertex From { get; init; }
	public Vertex To { get; init; }
	public Move MoveUsed { get; init; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready(){


	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		// TODO don't update this every frame
		Vector2 fromPos = From.Position;
		Vector2 toPos = To.Position;
		Points = new Vector2[] { fromPos, toPos };
	}
}
