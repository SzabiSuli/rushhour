namespace rushhour.src.Nodes.Nodes3D;

using System;
using Godot;
// TODO cite this under MIT License
// https://godotengine.org/asset-library/asset/3638

public partial class Camera3d : Camera3D {
    // =========================
    // Camera movement settings
    // =========================
    [ExportCategory("Camera movement")]
    [Export] public float CameraSpeed { get; set; } = 1.0f;
    [Export] public float CameraZoomSpeed { get; set; } = 20.0f;
    [Export] public float CameraZoomMin { get; set; } = 10.0f;
    [Export] public float CameraZoomMax { get; set; } = 50.0f;

    // =========================
    // Rotation (RMB) settings
    // =========================
    [ExportCategory("Rotation")]
    [Export] public float YawSensitivity { get; set; } = 0.5f;
    [Export] public float PitchSensitivity { get; set; } = 0.5f;
    [Export] public bool CaptureMouseOnRmb { get; set; } = false;

	public static Camera3d Instance {get; private set;} = null!;
    public Camera3d() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of Camera3d!");
        }
    }

    // =========================
    // Runtime state
    // =========================
	private const float transitionSpeed = 10f;
	private Vector3 _orbitCenter = Vector3.Zero;
    private Vector3 _targetOrbitCenter = Vector3.Zero;
	public Vector3 TargetOrbitCenter {
		get => followTarget?.Position ?? _targetOrbitCenter;
		set {
			followTarget = null;
			_targetOrbitCenter = value;
		}
	}
    public float OrbitDistance { get; set; } = 4000.0f;
	public Vertex? followTarget;
	private Vector3 _offsetDirection;
	private Vector3 _upVector;

    private bool _isRmbRotating = false;
    private float _yaw = 0.0f;
    private float _pitch = Mathf.Pi / 2;

	// With a Size of 10 a 256px sprite is 64px on the screen
	public float ZoomFactor => 2.5f / Size;

    public override void _Ready() => UpdateHelperVectors();
    public override void _Process(double delta) {
        var movement = Vector3.Zero;

        if (Input.IsKeyPressed(Key.D))
            movement.X += 1;
        if (Input.IsKeyPressed(Key.A))
            movement.X -= 1;
        if (Input.IsKeyPressed(Key.W))
            movement.Y += 1;
        if (Input.IsKeyPressed(Key.S))
            movement.Y -= 1;
        if (Input.IsKeyPressed(Key.R))
            movement.Z += 1;
        if (Input.IsKeyPressed(Key.F))
            movement.Z -= 1;
        
        // Shift boost
        float speedMultiplier = Input.IsKeyPressed(Key.Shift) ? 4.0f : 1.0f;

        // Move orbit center relative to the camera's view
		if (movement != Vector3.Zero) {
			movement = movement.Normalized();
			// Use the Basis of the node relative to the world
			Vector3 worldMovement = GlobalTransform.Basis * movement * Size;
			TargetOrbitCenter += worldMovement * CameraSpeed * speedMultiplier * (float)delta;
		}

		_orbitCenter = _orbitCenter.Lerp(TargetOrbitCenter, (float)delta * transitionSpeed);
		Position = _orbitCenter + _offsetDirection * OrbitDistance;
		LookAt(_orbitCenter, _upVector);
	}

	public override void _UnhandledInput(InputEvent @event) {
		// Mouse wheel zoom (changes orbit_distance)
		if (@event is InputEventMouseButton mouseButton) {
			if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelUp) {
				if (Size >= 20) {
					Size -= 10;
				}
			} else if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelDown) {
				Size += 10;
			}

			// Start/stop rotate+tilt with M2
			if (mouseButton.ButtonIndex == MouseButton.Right) {
				_isRmbRotating = mouseButton.Pressed;
				if (CaptureMouseOnRmb) {
					Input.MouseMode = mouseButton.Pressed
						? Input.MouseModeEnum.Captured
						: Input.MouseModeEnum.Visible;
				}
			}
		}
		// Delta-based rotation while dragging with M2
		else if (@event is InputEventMouseMotion mouseMotion && _isRmbRotating) {
			Vector2I vp = GetViewport().GetWindow().Size;
			float vmin = Mathf.Min(vp.X, vp.Y);
			float dt = (float)GetProcessDeltaTime();
			float sixtyFps = 60.0f * dt;

			// px -> normalized fraction of screen -> radians, scaled by dt
			float dx = (mouseMotion.Relative.X / vmin) * YawSensitivity * Mathf.Tau * sixtyFps;
			float dy = (mouseMotion.Relative.Y / vmin) * PitchSensitivity * Mathf.Tau * sixtyFps;

			_yaw -= dx;
			_pitch += dy; // flip for inverted tilt

			UpdateHelperVectors();
		}
	}

	// =========================
	// Helpers
	// =========================
	private void UpdateHelperVectors() {
		// Spherical direction from yaw/pitch
		_offsetDirection = new Vector3(
			Mathf.Sin(_yaw) * Mathf.Cos(_pitch),
			Mathf.Sin(_pitch),
			Mathf.Cos(_yaw) * Mathf.Cos(_pitch)
		).Normalized();

		_upVector = new Vector3(
			-Mathf.Sin(_yaw) * Mathf.Sin(_pitch),
			Mathf.Cos(_pitch),
			-Mathf.Cos(_yaw) * Mathf.Sin(_pitch)
		).Normalized();
	}
}
