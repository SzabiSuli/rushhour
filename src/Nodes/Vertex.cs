namespace rushhour.src.Nodes;

using Godot;
using rushhour.src.Model;
using System;
using System.Collections.Generic;

public partial class Vertex : RigidBody3D
{

	public const int repulsionForce = 1000;
	public const int influenceRadius = 1000;
	public readonly Vector3 maxVelocity = Vector3.One * 100;
	public readonly Vector3 negMaxVelocity = Vector3.One * (-100);
	public RHGameState GameState { get; set; } = null!;
	public const String scenePath = "res://scenes/vertex.tscn";
	public static PackedScene Creator {get;} = ResourceLoader.Load<PackedScene>(scenePath);
	public static Dictionary<RHGameState, Vertex> Dict { get; } = new();
	

	public void Init(RHGameState gameState) {
		GameState = gameState;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		this.InputEvent += OnInputEvent;
	}

	private void OnInputEvent(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx) {
		// Check if the event is a mouse button click
		if (@event is InputEventMouseButton mouseEvent) {
			// Specifically look for the Left Mouse Button being pressed
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed) {
				GD.Print("Object clicked at: " + eventPosition);
				HandleClick();
			}
		}
	}

	private void HandleClick() {
		GameState.PrintState();
	}

	public override void _PhysicsProcess(double delta) {
		// Barnes-Hut approximation via OctTree (O(n log n) instead of O(n²))
		var tree = OctTree.GetCurrent();
		if (tree != null) {
			var force = OctTree.ComputeForce(tree, this, OctTree.Theta);
			ApplyCentralForce(force);
		}

		// Clamp velocity to prevent instability
		LinearVelocity.Clamp(negMaxVelocity, maxVelocity);
	}

	public void UpdateColor(bool isCurrent) {
		var sprite = GetChild<Sprite3D>(1);
		if (isCurrent) {
			sprite.Modulate = Colors.Green;
		} else {
			sprite.Modulate = Colors.White;
		}
	}
}
