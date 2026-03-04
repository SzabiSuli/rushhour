using Godot;
using System;

public partial class Camera3d : Camera3D
{
	// =========================
	// Camera movement settings
	// =========================
	[ExportCategory("Camera movement")]
	[Export] public float CameraSpeed { get; set; } = 20.0f;
	[Export] public float CameraZoomSpeed { get; set; } = 20.0f;
	[Export] public float CameraZoomMin { get; set; } = 10.0f;
	[Export] public float CameraZoomMax { get; set; } = 50.0f;

	// =========================
	// Edge scrolling settings
	// =========================
	[ExportCategory("Edge scrolling")]
	[Export] public float EdgeScrollMargin { get; set; } = 20.0f;
	[Export] public float EdgeScrollSpeed { get; set; } = 15.0f;

	// =========================
	// Rotation (MMB) settings
	// =========================
	[ExportCategory("Rotation")]
	[Export] public float YawSensitivity { get; set; } = 0.50f;
	[Export] public float PitchSensitivity { get; set; } = 0.18f;
	[Export] public float MaxStepDeg { get; set; } = 3.0f;
	[Export] public float PitchMinDeg { get; set; } = 10.0f;
	[Export] public float PitchMaxDeg { get; set; } = 80.0f;
	[Export] public bool CaptureMouseOnMmb { get; set; } = false;

	// =========================
	// Runtime state
	// =========================
	public Vector3 OrbitCenter { get; set; } = Vector3.Zero;
	public float OrbitDistance { get; set; } = 25.0f;
	public float CurrentHeight { get; set; } = 20.0f;
	public float OrbitRadius { get; set; } = 20.0f;

	private bool _isMmbRotating = false;
	private float _yaw = 0.0f;
	private float _pitch = 0.8f; // radians (~45° initial)

	public const float farDistance = 2000.0f;

	public override void _Ready()
	{
		float pmin = Mathf.DegToRad(PitchMinDeg);
		float pmax = Mathf.DegToRad(PitchMaxDeg);
		_pitch = Mathf.Clamp(_pitch, pmin, pmax);
		UpdateCameraPosition();
	}

	public override void _Process(double delta)
	{
		var movement = Vector3.Zero;

		// Keyboard movement (uses default ui_* actions)
		if (Input.IsActionPressed("ui_right"))
			movement.X += 1;
		if (Input.IsActionPressed("ui_left"))
			movement.X -= 1;
		if (Input.IsActionPressed("ui_up"))
			movement.Z -= 1;
		if (Input.IsActionPressed("ui_down"))
			movement.Z += 1;
		
		// Shift boost
		float speedMultiplier = Input.IsActionPressed("ui_shift") ? 2.0f : 1.0f;

		// Move orbit center in camera's yaw frame
		if (movement.Length() > 0.0f)
		{
			movement = movement.Normalized().Rotated(Vector3.Up, _yaw);
			OrbitCenter += movement * CameraSpeed * speedMultiplier * (float)delta;
			UpdateCameraPosition();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Mouse wheel zoom (changes orbit_distance)
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				// OrbitDistance = Mathf.Max(CameraZoomMin, OrbitDistance - CameraZoomSpeed * (float)GetProcessDeltaTime());
				if (Size >= 20) {
					Size -= 10;
				}
				UpdateCameraPosition();
			}
			else if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				// OrbitDistance = Mathf.Min(CameraZoomMax, OrbitDistance + CameraZoomSpeed * (float)GetProcessDeltaTime());
				Size += 10;
				UpdateCameraPosition();
			}

			// Start/stop rotate+tilt with Middle Mouse
			if (mouseButton.ButtonIndex == MouseButton.Right)
			{
				_isMmbRotating = mouseButton.Pressed;
				if (CaptureMouseOnMmb)
				{
					Input.MouseMode = mouseButton.Pressed
						? Input.MouseModeEnum.Captured
						: Input.MouseModeEnum.Visible;
				}
			}
		}
		// Delta-based rotation while dragging with MMB
		else if (@event is InputEventMouseMotion mouseMotion && _isMmbRotating)
		{
			Vector2I vp = GetViewport().GetWindow().Size;
			float vmin = Mathf.Min(vp.X, vp.Y);
			float dt = (float)GetProcessDeltaTime();
			float sixtyFps = 60.0f * dt;

			// px -> normalized fraction of screen -> radians, scaled by dt
			float dx = (mouseMotion.Relative.X / vmin) * YawSensitivity * Mathf.Tau * sixtyFps;
			float dy = (mouseMotion.Relative.Y / vmin) * PitchSensitivity * Mathf.Tau * sixtyFps;

			// Safety cap per event
			float maxStep = Mathf.DegToRad(MaxStepDeg);
			dx = Mathf.Clamp(dx, -maxStep, maxStep);
			dy = Mathf.Clamp(dy, -maxStep, maxStep);

			_yaw -= dx;
			_pitch += dy; // flip for inverted tilt

			float pmin = Mathf.DegToRad(PitchMinDeg);
			float pmax = Mathf.DegToRad(PitchMaxDeg);
			_pitch = Mathf.Clamp(_pitch, pmin, pmax);

			UpdateCameraPosition();
		}
	}

	// =========================
	// Helpers
	// =========================
	private void UpdateCameraPosition()
	{
		// Spherical direction from yaw/pitch
		var dir = new Vector3(
			Mathf.Sin(_yaw) * Mathf.Cos(_pitch),
			Mathf.Sin(_pitch),
			Mathf.Cos(_yaw) * Mathf.Cos(_pitch)
		).Normalized();

		Position = OrbitCenter + dir * farDistance;
		LookAt(OrbitCenter, Vector3.Up);

		// Derived values (useful if other systems read them)
		CurrentHeight = farDistance * Mathf.Sin(_pitch);
		OrbitRadius = farDistance * Mathf.Cos(_pitch);
	}
}
