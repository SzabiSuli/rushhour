namespace rushhour.src.Model;

using Godot;

public class PlacedRHPiece {
    public RushHourPiece Piece { get; init; }

    /// Position of the front of the car
    public Vector2I Position { get; init; }
    public Direction FacingDirection { get; init; }

    public PlacedRHPiece(RushHourPiece piece, Vector2I position, Direction facingDirection){
        Piece = piece;
        Position = position;
        FacingDirection = facingDirection;
    }
    public PlacedRHPiece WithMove(Direction dir){
        Vector2I newPosition = Position;
        switch (dir){
            case Direction.Up:
                newPosition = Position + new Vector2I(0, -1);
                break;
            case Direction.Down:
                newPosition = Position + new Vector2I(0, 1);
                break;
            case Direction.Left:
                newPosition = Position + new Vector2I(-1, 0);
                break;
            case Direction.Right:
                newPosition = Position + new Vector2I(1, 0);
                break;
        }
        return new PlacedRHPiece(Piece, newPosition, FacingDirection);
    }
}
public abstract class Piece {
    // public int Id { get; set; }
    
    /// Position of the front of the car
    // public Vector2I Position { get; set; }
    // public Direction FacingDirection { get; set; }

    public abstract int Width { get; }
    public abstract int Length { get; }

    public abstract bool CanMoveLengthwise { get; }
    public abstract bool CanMoveSideways { get; }
}

public abstract class RushHourPiece : Piece {
    public override bool CanMoveLengthwise => true;
    public override bool CanMoveSideways => false;
}

class Car : RushHourPiece {
    public override int Length => 2;
    public override int Width => 1;
}

class MainCar : Car { }

class Bus : RushHourPiece {
    public override int Length => 3;
    public override int Width => 1;
}

class Blocker : Piece {
    public override int Length => 1;
    public override int Width => 1;
    public override bool CanMoveLengthwise => false;
    public override bool CanMoveSideways => false;
}
