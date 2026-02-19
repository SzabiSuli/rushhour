namespace rushhour;

abstract class Piece
{
    public int Id { get; set; }
    public Coordinate Position { get; set; }
    public Direction FacingDirection { get; set; }

    public abstract int Width { get; }
    public abstract int Length { get; }

    public abstract bool CanMoveLengthwise { get; }
    public abstract bool CanMoveSideways { get; }
}

abstract class RushHourPiece : Piece
{
    public override bool CanMoveLengthwise => true;
    public override bool CanMoveSideways => false;
}

class Car : RushHourPiece
{
    public override int Length => 2;
    public override int Width => 1;
}

class MainCar : Car { }

class Bus : RushHourPiece
{
    public override int Length => 3;
    public override int Width => 1;
}

class Blocker : Piece
{
    public override int Length => 1;
    public override int Width => 1;
    public override bool CanMoveLengthwise => false;
    public override bool CanMoveSideways => false;
}