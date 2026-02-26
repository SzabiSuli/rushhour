using System.Runtime.CompilerServices;

namespace rushhour.src.Nodes;

using Godot;
using System;
using System.Collections.Generic;

public partial class Vertex : Area2D
{

	public Vector2 Velocity = Vector2.Zero;

	// TODO reconsider if the initial state should be fixed
	public bool IsFixed = false;

	public HashSet<Vertex> Neighbors = new HashSet<Vertex>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {

		if (IsFixed) return;

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
				Velocity -= distanceVector * distanceVector.Length() / 10000 * (float)delta;
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


		Position += Velocity * (float)delta;
		// damping with according to delta time
		Velocity *= (float)Math.Pow(0.25, delta);

	}
}
