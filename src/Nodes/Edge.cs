namespace rushhour.src.Nodes;

using Godot;
using System;
using rushhour.src.Model;


public partial class Edge : Line2D
{
	// Physics constants
	public const int springLength = 100;
	public const double optimalIntervalLowerBound = springLength * 0.9;
	public const double optimalIntervalUpperBound = springLength * 1.1;
	public const float springForce = 1;

	// Init must be called for initialization
	public Vertex From { get; set; } = null!;
	public Vertex To { get; set; } = null!;
	public Move MoveUsed { get; set; }

	public void Init(Vertex form, Vertex to, Move moveUsed){
		From = form;
		To = to;
		MoveUsed = moveUsed;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready(){

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		// TODO don't update this every frame
		Points = [From.Position, To.Position];

		ApplySpringForce(delta);
	}

	public void ApplySpringForce(double delta) {
		var distanceVector = To.Position - From.Position;
		var length = distanceVector.Length();
		if (optimalIntervalLowerBound < length && length < optimalIntervalUpperBound) {
			// Spring is close to the optimal lenght, skip applying force for performance
			return;
		}
		var force = distanceVector * ((length - springLength) / length) * springForce;
		var deltaVelocity = force * (float)delta;
		From.Velocity += deltaVelocity;
		To.Velocity -= deltaVelocity;		
	}
}
