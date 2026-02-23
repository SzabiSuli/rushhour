namespace rushhour.src.Model;

public abstract class Heuristic{
    public abstract int Evaluate(RHGameState state);
}

// TODO make this static or something
public class DistanceHeuristic : Heuristic {
    public override int Evaluate(RHGameState state) {
        return 5 - state.PlacedPieces[0].Position.X;
    }
}