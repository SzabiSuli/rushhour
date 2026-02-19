namespace rushhour;
using System.Collections.Generic;

public class Board
{
    public GameState CurrentState { get; private set; }

    public bool TryMove(Move move)
    {
        // Implementation hidden
        return false;
    }

    public IEnumerable<Move> GetPossibleMoves(GameState state)
    {
        // Implementation hidden
        yield break;
    }

    public GameState ApplyMove(GameState state, Move move)
    {
        // Implementation hidden
        return null;
    }

    public void LoadLevel(int levelIndex)
    {
        // Implementation hidden
    }
}
