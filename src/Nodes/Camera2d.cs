namespace rushhour.src.Nodes;

using Godot;
using System;

public partial class Camera2d : Camera2D
{

	
	private const double _minZoom = 0.01f;
	private const double _maxZoom = 100.0f;
	private const double _zoomSpeed = 5.0f;
	private const double _zoomIncrement = 0.1f;

	private double _targetZoom = 1.0f;

	private const double _dragSpeed = 10.0f;

	private Vector2 _targetPosition;

	private int _mapSize;
	private int _mapSizePixels;

	public override void _Ready()
	{
		int middle = _mapSizePixels / 2;
		Position = new Vector2(0, 0);
		_targetPosition = Position;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseEvent)
		{
			if (mouseEvent.ButtonMask == MouseButtonMask.Right)
			{
				_targetPosition -= mouseEvent.Relative / Zoom;
				SetPhysicsProcess(true);
			}
		}
		if (@event is InputEventMouseButton buttonEvent)
		{
			if (buttonEvent.IsPressed())
			{
				if (buttonEvent.ButtonIndex == MouseButton.WheelUp)
				{
					_ZoomOut();
				}
				else if (buttonEvent.ButtonIndex == MouseButton.WheelDown)
				{
					_ZoomIn();
				}
			}
		}
	}

	private void _ZoomIn()
	{
		_targetZoom = Math.Max(_targetZoom - _zoomIncrement, _minZoom);
		SetPhysicsProcess(true);
	}

	private void _ZoomOut()
	{
		_targetZoom = Math.Min(_targetZoom + _zoomIncrement, _maxZoom);
		SetPhysicsProcess(true);
	}

	public override void _PhysicsProcess(double delta)
	{
		Zoom = Zoom.Lerp(Vector2.One * (float)_targetZoom, (float)(_zoomSpeed * delta));

		// get the width and height of the camera with its actual zoom
		// Vector2 cameraSize = GetViewport().GetVisibleRect().Size / Zoom;

		Position = Position.Lerp(_targetPosition, (float)(_dragSpeed * delta));

		SetPhysicsProcess(!(Mathf.IsEqualApprox(Zoom.X, _targetZoom) && Position.IsEqualApprox(_targetPosition)));
	}
}
