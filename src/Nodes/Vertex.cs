using System.Runtime.CompilerServices;

namespace rushhour.src.Nodes;

using Godot;
using rushhour.src.Model;
using System;
using System.Collections.Generic;

public partial class Vertex : Area3D
{

	public const int repulsionForce = 1000;
	public const int influenceRadius = 1000;
	public const int maxVelocity = 1000;
	public const double dampingFactor = 0.25;

	public Vector3 Velocity = Vector3.Zero;
	public RHGameState GameState { get; set; } = null!;

	public void Init(RHGameState gameState) {
		GameState = gameState;
	}


	// TODO might not even need this, edges can apply the forces
	// public HashSet<Vertex> Neighbors = new HashSet<Vertex>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		this.InputEvent += OnInputEvent;
	}

	// private void OnInputEvent(Node viewport, InputEvent @event, int shapeIdx) {
	// 	if (@event is InputEventMouseButton mouseEvent) {
	// 		if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left) {
	// 			GameState.OnVertexClicked(this);
	// 		}
	// 	}
	// }

	private void OnInputEvent(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx)
	{
		// Check if the event is a mouse button click
		if (@event is InputEventMouseButton mouseEvent)
		{
			// Specifically look for the Left Mouse Button being pressed
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
			{
				GD.Print("Object clicked at: " + eventPosition);
				HandleClick();
			}
		}
	}

	private void HandleClick()
	{
		GameState.PrintState();
	}

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

	public void UpdateColor(bool isCurrent) {
		var sprite = GetChild<Sprite3D>(1);
		if (isCurrent) {
			sprite.Scale = Vector3.One * 2;
		} else {
			sprite.Scale = Vector3.One;
		}
	}
}
