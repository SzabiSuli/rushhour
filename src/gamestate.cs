namespace rushhour;

using System.Collections.Generic;

public class GameState
{
    public List<Piece> Pieces { get; }
    public int BoardWidth { get; }
    public int BoardHeight { get; }

    public GameState(List<Piece> pieces, int width, int height)
    {
        // Implementation hidden
    }

    public override int GetHashCode()
    {
        // Implementation hidden
        return 0;
    }

    public override bool Equals(object obj)
    {
        // Implementation hidden
        return false;
    }

    public bool IsSolved()
    {
        // Implementation hidden
        return false;
    }
}
