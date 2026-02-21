namespace rushhour.src;


public struct Move
{
    public int PieceIndex { get; set; }
    public Direction Dir { get; set; }
}

public enum Direction
{
    Up,
    Down,
    Left,
    Right
}