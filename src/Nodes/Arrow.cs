namespace  rushhour.src.Nodes;

using src.Model;
using Godot;
using System;

public partial class Arrow : Area2D
{

    public bool IsActive
    {
        get => Visible && InputPickable;
        set
        {
            Visible = value;
            InputPickable = value;
            
            // Optional: If you want to stop it from bumping into other physics objects too:
            CollisionShape.Disabled = !value;
        }
    }

    public Sprite2D Sprite => GetChild<Sprite2D>(1);
    public CollisionPolygon2D CollisionShape => GetNode<CollisionPolygon2D>("CollisionPolygon2D");
    public Direction direction;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        this.InputEvent += OnInputEvent;
    }

    public void Init(Direction d, int vehicleLength) {
        switch (d) {
            case Direction.Up:
                Position = new Vector2(0, -GameBoard.tileSize.Y);
                break;
            case Direction.Down:
                Position = new Vector2(0, GameBoard.tileSize.Y * vehicleLength);
                Sprite.FlipV = true;
                break;
            default:
                throw new ArgumentException("Arrow direction can only be up or down");
        }
        direction = d;
    }

    

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx) {
        if (@event is InputEventMouseButton mouseEvent) {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed) {
                HandleClick();
            }
        }
    }

    public void HandleClick() {
        GetParent<VehicleNode>().Move(direction);
    }

}
