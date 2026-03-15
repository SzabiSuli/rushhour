namespace rushhour.src.Model;

using System;

public abstract class Heuristic{
    public abstract int Evaluate(RHGameState state);
}


public abstract class NonNegiativeHeuristic : Heuristic {}
public abstract class AdmissibleHeuristic : NonNegiativeHeuristic {} 
public abstract class MonotoneHeuristic : AdmissibleHeuristic {}



public class DistanceHeuristic : MonotoneHeuristic {
    public override int Evaluate(RHGameState state) {
        return 5 - state.PlacedPieces[0].Position.X;
    }
}

public class FreeSpacesHeuristic : MonotoneHeuristic {
    // Adds distance of the main car and
    // how many cars are in its way
    
    public override int Evaluate(RHGameState state) {
        int distance = 5 - state.PlacedPieces[0].Position.X;
        int blocks = 0;

        for (int i = state.PlacedPieces[0].Position.X + 1; i < 6; i++) {
            if (state.BoardGrid[i, 2] != -1) {
                blocks++;
            }
        }

        return distance + blocks;
    }
}

public class MoverHeuristic : MonotoneHeuristic {
    // Adds distance of the main car and
    // how many moves it takes at least to push out each car of its way
    public override int Evaluate(RHGameState state) {
        int distance = 5 - state.PlacedPieces[0].Position.X;
        int moves = 0;

        for (int i = state.PlacedPieces[0].Position.X + 1; i < 6; i++) {
            int index = state.BoardGrid[i, 2]; 

            if (index != -1) {
                // In Rush hour we know 
                // the vehicle has to point up or down:
                
                int top;
                int bot;
                
                var pp = state.PlacedPieces[index];
                int pi = pp.Position.Y;
                int pl = pp.Piece.Length - 1;
                switch (pp.FacingDirection) {
                    case Direction.Up:
                        top = pi;
                        bot = pi + pl;
                        break; 
                    case Direction.Down:
                        top = pi - pl;
                        bot = pi;
                        break; 
                    default:
                        throw new ArgumentException($"Error when calculating heuristic: state is unsolvable, vehicle in the way {pp.Position}");
                }
                if (pp.Piece is Bus) {
                    int cost = 5 - bot;
                    // we can only place it down
                    for (int j = 3; j < 6; j++) {
                        // add 1 for each vehicle that's in the way of 
                        // putting the bus to the bottom
                        int tile = state.BoardGrid[i, j]; 
                        if (tile != -1 && tile != index) {
                            cost++;
                        }
                    }
                    moves += cost;
                } else {
                    int costToPushUp = top;
                    for (int j = 0; j < 2; j++) {
                        // add 1 for each vehicle that's in the way of 
                        // putting the bus to the bottom
                        int tile = state.BoardGrid[i, j]; 
                        if (tile != -1 && tile != index) {
                            costToPushUp++;
                        }
                    }

                    int costToPushDown = 4 - bot;
                    for (int j = 3; j < 5; j++) {
                        // add 1 for each vehicle that's in the way of 
                        // putting the bus to the bottom
                        int tile = state.BoardGrid[i, j]; 
                        if (tile != -1 && tile != index) {
                            costToPushDown++;
                        }
                    }

                    moves += Math.Min(costToPushDown, costToPushUp);
                }
            }
        }

        return distance + moves;
    }
}