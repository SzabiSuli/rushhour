namespace rushhour;

public struct Coordinate
{
    public int X { get; set; }
    public int Y { get; set; }
}

public struct Move
{
    public int PieceId { get; set; }
    public Direction Dir { get; set; }
}
