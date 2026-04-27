namespace rushhour.src.Model;

using System.Collections.Generic;
using System.Text;
using System.Linq;
using Godot;

public abstract class GameState {
    // public List<Piece> Pieces { get; protected set;}
    // public abstract int BoardWidth { get; protected set;}
    // public abstract int BoardHeight { get; protected set;}

    // public GameState(List<Piece> pieces, int width, int height){
    //     Pieces = pieces;
    //     BoardWidth = width;
    //     BoardHeight = height;
    // }

    // public abstract bool IsSolved();
}

// Immutable object
public class RHGameState : GameState {
    // PlacedPiece at index 0 has to be the main car
    public IReadOnlyList<PlacedRHPiece> PlacedPieces { get; init;}
    private const int maxPieces = 16;
    public int BoardWidth => 6;
    public int BoardHeight => 6;
    // public PlacedRHPiece MainCar {get; protected set;}
    private readonly int[,] _boardGrid;
    public int this[int x, int y] => _boardGrid[x, y];
    public Vector2I ExitPosition => new Vector2I(BoardWidth - 1, 2);

    private readonly int _hashCode;


    public RHGameState(PlacedRHPiece[] placedPieces) : base(){
        if (placedPieces.Length > maxPieces){
            throw new System.Exception($"Too many pieces: {placedPieces.Length} (max {maxPieces})");
        }
        PlacedPieces = placedPieces; // Does NOT copy the array
        if (PlacedPieces.Count == 0 || PlacedPieces[0].Piece is not MainCar){
            throw new System.Exception($"First piece placedPieces must be the main car");
        }

        // initialize board grid with all numbers set to -1
        _boardGrid = new int[BoardWidth, BoardHeight];
        for (int x = 0; x < BoardWidth; x++){
            for (int y = 0; y < BoardHeight; y++){
                _boardGrid[x, y] = -1;
            }
        }
        // verify pieces and fill board reference grid
        // foreach (var placedPiece in placedPieces){
        for (int i = 0; i < placedPieces.Length; i++){
            var placedPiece = placedPieces[i];
            int x = placedPiece.Position.X;
            int y = placedPiece.Position.Y;
            for (int j = 0; j < placedPiece.Piece.Length; j++){
                switch (placedPiece.FacingDirection){
                    case Direction.Up:
                        y = placedPiece.Position.Y + j;
                        break;
                    case Direction.Down:
                        y = placedPiece.Position.Y - j;
                        break;
                    case Direction.Left:
                        x = placedPiece.Position.X + j;
                        break;
                    case Direction.Right:
                        x = placedPiece.Position.X - j;
                        break;
                }
                if (x < 0 || x >= BoardWidth || y < 0 || y >= BoardHeight){
                    throw new System.Exception($"Piece is out of bounds at position ({x}, {y})");
                }
                if (_boardGrid[x, y] != -1){
                    throw new System.Exception($"Piece overlaps with existing piece at position ({x}, {y})");
                }
                _boardGrid[x, y] = i;
            }
        }

        _hashCode = CalculateHashCode();
    }

    public bool IsSolved() => PlacedPieces[0].Position == ExitPosition;
    
    public override int GetHashCode() => _hashCode;

    private int CalculateHashCode() {
        int hash = 17;
        for (int i = 0; i < PlacedPieces.Count; i++){
            PlacedRHPiece piece = PlacedPieces[i];
            // hash = hash * 31 + i.GetHashCode();
            hash = hash * 31 + piece.Position.GetHashCode();
        }
        return hash;
    }

    public override bool Equals(object? obj){
        if (obj is not RHGameState other) return false;
        if (GetHashCode() != other.GetHashCode()) return false;
        if (PlacedPieces.Count != other.PlacedPieces.Count) return false;
        for (int i = 0; i < PlacedPieces.Count; i++){
            if (PlacedPieces[i].Position != other.PlacedPieces[i].Position){
                return false;
            }
        }
        return true;
    }

    public static bool operator ==(RHGameState? left, RHGameState? right) {
        // Check for null on both sides
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;

        return left.Equals(right);
    }

    public static bool operator !=(RHGameState? left, RHGameState? right) {
        return !(left == right);
    }

    public IEnumerable<Move> GetPossibleMoves(){
        for (int i = 0; i < PlacedPieces.Count; i++){
            var placedPiece = PlacedPieces[i];
            switch (placedPiece.FacingDirection){
                case Direction.Up:
                    if (placedPiece.Position.Y - 1 >= 0 && _boardGrid[placedPiece.Position.X, placedPiece.Position.Y - 1] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Up};
                    }
                    if (placedPiece.Position.Y + placedPiece.Piece.Length < BoardHeight && _boardGrid[placedPiece.Position.X, placedPiece.Position.Y + placedPiece.Piece.Length] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Down};
                    }
                    break;
                case Direction.Down:
                    if (placedPiece.Position.Y + 1 < BoardHeight && _boardGrid[placedPiece.Position.X, placedPiece.Position.Y + 1] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Down};
                    }
                    if (placedPiece.Position.Y - placedPiece.Piece.Length >= 0 && _boardGrid[placedPiece.Position.X, placedPiece.Position.Y - placedPiece.Piece.Length] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Up};
                    }
                    break;
                case Direction.Left:
                    if (placedPiece.Position.X - 1 >= 0 && _boardGrid[placedPiece.Position.X - 1, placedPiece.Position.Y] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Left};
                    }
                    if (placedPiece.Position.X + placedPiece.Piece.Length < BoardWidth && _boardGrid[placedPiece.Position.X + placedPiece.Piece.Length, placedPiece.Position.Y] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Right};
                    }
                    break;
                case Direction.Right:
                    if (placedPiece.Position.X + 1 < BoardWidth && _boardGrid[placedPiece.Position.X + 1, placedPiece.Position.Y] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Right};
                    }
                    if (placedPiece.Position.X - placedPiece.Piece.Length >= 0 && _boardGrid[placedPiece.Position.X - placedPiece.Piece.Length, placedPiece.Position.Y] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Left};
                    }
                    break;
            }
        }
        yield break;
    }

    public IEnumerable<StateMove> GetPossibleStateMoves() => GetPossibleMoves().Select(
        move => new StateMove(this, this.WithMove(move), move)
    );

    public RHGameState WithMove(Move move){
        // create a shallow copy of the placed pieces array
        PlacedRHPiece[] newPlacedPieces = PlacedPieces.ToArray();
        // copy the piece to move and update its position
        var movedPiece = newPlacedPieces[move.PieceIndex].WithMove(move.Dir);
        newPlacedPieces[move.PieceIndex] = movedPiece;

        return new RHGameState(newPlacedPieces);
    }

    public void PrintState(){
        StringBuilder sb = new StringBuilder();
        for (int y = 0; y < BoardHeight; y++){
            for (int x = 0; x < BoardWidth; x++){
                var i = _boardGrid[x, y];
                if (i == -1) {
                    sb.Append(". ");
                } else {
                    sb.Append($"{i} ");
                }
            }
            sb.AppendLine();
        }
        GD.Print(sb.ToString());
    }    
}
