using System.Runtime.CompilerServices;

namespace rushhour.src.Nodes;

using Godot;
using rushhour.src.Model;
using System;
using System.Collections.Generic;

public partial class Vertex : Area2D
{

	public Vector2 Velocity = Vector2.Zero;
	public RHGameState GameState { get; set; }

	// TODO reconsider if the initial state should be fixed
	public bool IsFixed = false;

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
				// IsFixed = true;

			} 
			// else if (mouseEvent.ButtonIndex == MouseButton.Left && !mouseEvent.Pressed) {
			// 	IsFixed = false;
			// }
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {

		// if (IsFixed) return;

		// vertex squared repulsion
		// get all vertices from Vertices group
		var vertices = GetTree().GetNodesInGroup("Vertices");
		foreach (var v in vertices) {
			if (v == this) continue;
			var vertex = (Vertex)v;
			var distanceVector = vertex.Position - Position;
			if (distanceVector.Length() < 1000) {
				// TODO tweak this
				// Position -= distanceVector.Normalized() / 10;
				var f = distanceVector.Normalized() / distanceVector.LengthSquared() * 1000000 * (float)delta;
				// GD.Print($"Applying force {f} to vertex at position {Position}");
				Velocity -= f;

				// TODO TESTING make force cubed
				// Velocity -= distanceVector * distanceVector.LengthSquared() / 1000000 * (float)delta;
			}
		}

		// edge linear spring force
		foreach (var v in Neighbors) {
			var distanceVector = v.Position - Position;
			int desiredDistance = 100; // TODO tweak this
			if (distanceVector.Length() < desiredDistance - 10 || distanceVector.Length() > desiredDistance + 10) {
				// Position += distanceVector.Normalized() * (distanceVector.Length() - desiredDistance) / 10;
				Velocity += distanceVector * ((distanceVector.Length() - desiredDistance) / distanceVector.Length()) * (float)delta;
			}
		}

		// Clamp velocity to prevent instability
		if (Velocity.Length() > 500) {
			Velocity = Velocity.Normalized() * 500;
		}
		Position += Velocity * (float)delta;
		// damping with according to delta time
		Velocity *= (float)Math.Pow(0.25, delta);

	}
}
