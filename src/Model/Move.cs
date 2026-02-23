namespace rushhour.src.Model;


public struct Move {
    public int PieceIndex { get; init; }
    public Direction Dir { get; init; }
}

public enum Direction {
    Up,
    Down,
    Left,
    Right
}