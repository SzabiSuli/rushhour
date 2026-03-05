namespace rushhour.src.Nodes;

using Godot;
using System;
using rushhour.src.Model;


public partial class Edge : MeshInstance3D
{
	// Physics constants
	public const int springLength = 5;
	public const double optimalIntervalLowerBound = springLength * 0.9;
	public const double optimalIntervalUpperBound = springLength * 1.1;
	public const float springForce = 10;

	// Init must be called for initialization
	public Vertex From { get; set; } = null!;
	public Vertex To { get; set; } = null!;
	public StateMove MoveUsed { get; set; } = null!;

	private ImmediateMesh _mesh = new ImmediateMesh(); 

	public void Init(Vertex form, Vertex to, StateMove moveUsed){
		From = form;
		To = to;
		MoveUsed = moveUsed;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready(){
		Mesh = _mesh;
		MaterialOverride = new StandardMaterial3D() {
			AlbedoColor = Colors.White,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			// Metallic = 0.5f,
			// Roughness = 0.5f
		};
	}

	// Called every physics frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta) {
		// TODO don't update this every frame
		UpdateLine(From.Position, To.Position);

		ApplySpringForce();
	}

	public void UpdateLine(Vector3 from, Vector3 to) {
		_mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        _mesh.SurfaceAddVertex(from);
        _mesh.SurfaceAddVertex(to);
        _mesh.SurfaceEnd();
	}

	public void UpdateColor(bool onPath) {
		// change color to yellow
		// TODO maybe make new material instead?
		if (onPath) {
			MaterialOverride.Set("albedo_color", Colors.Yellow);
		} else {
			MaterialOverride.Set("albedo_color", Colors.White);
		}
	}

	public void ApplySpringForce() {
		var distanceVector = To.Position - From.Position;
		var length = distanceVector.Length();
		if (optimalIntervalLowerBound < length && length < optimalIntervalUpperBound) {
			// Spring is close to the optimal lenght, skip applying force for performance
			return;
		}
		var force = distanceVector * ((length - springLength) / length) * springForce;

		// TODO make this thread safe
		From.ApplyCentralForce(force);
		To.ApplyCentralForce(-force);
	}
}
