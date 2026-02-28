using System.Runtime.CompilerServices;

namespace rushhour.src.Nodes;

using Godot;
using rushhour.src.Model;
using System;
using System.Collections.Generic;

public partial class Vertex : Area2D
{

	public const int repulsionForce = 1000000;
	public const int influenceRadius = 1000;
	public const int maxVelocity = 1000;
	public const double dampingFactor = 0.25;

	public Vector2 Velocity = Vector2.Zero;
	public RHGameState GameState { get; set; } = null!;

	public void Init(RHGameState gameState) {
		GameState = gameState;
	}


	// TODO might not even need this, edges can apply the forces
	public HashSet<Vertex> Neighbors = new HashSet<Vertex>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		this.InputEvent += OnInputEvent;
	}

	private void OnInputEvent(Node viewport, InputEvent inputEvent, long shapeIdx) {
		if (inputEvent is InputEventMouseButton mouseEvent) {
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed) {
				GD.Print("State of clicked vertex:");
				GameState.PrintState();
			} 
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		// TODO use spacial optimization
		// Apply squared repulsion force for all vertices
		// get all vertices from Vertices group
		var vertices = GetTree().GetNodesInGroup("Vertices");
		foreach (var v in vertices) {
			if (v == this) continue;
			var vertex = (Vertex)v;
			ApplyRepulsionForce(vertex, delta);
		}

		// Clamp velocity to prevent instability
		if (Velocity.Length() > maxVelocity) {
			Velocity = Velocity.Normalized() * maxVelocity;
		}

		// Apply movement
		Position += Velocity * (float)delta;

		// damping with according to delta time
		Velocity *= (float)Math.Pow(dampingFactor, delta);
	}

	public void ApplyRepulsionForce(Vertex other, double delta) {
		var distanceVector = other.Position - Position;
		if (distanceVector.Length() > influenceRadius) {
			// Applied force would be very minimal, skip calculation for performance
			return;
		}
		var force = distanceVector.Normalized() / distanceVector.LengthSquared() * (-repulsionForce);
		var deltaVelocity = force * (float)delta;
		Velocity += deltaVelocity;
	}

}
