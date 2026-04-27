
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


public abstract class Heuristic<TState>{
    public abstract int Evaluate(TState state);
}


public abstract class NonNegiativeHeuristic<TState> : Heuristic<TState> {}
public abstract class AdmissibleHeuristic<TState> : NonNegiativeHeuristic<TState> {} 
public abstract class MonotoneHeuristic<TState> : AdmissibleHeuristic<TState> {}


public class NullHeuristic : MonotoneHeuristic<RHGameState> {
    public override int Evaluate(RHGameState _) => 0;
}

public class DistanceHeuristic : MonotoneHeuristic<RHGameState> {
    public override int Evaluate(RHGameState state) {
        // Assume the main car is facing right
        if (state.PlacedPieces[0].Position.Y != state.ExitPosition.Y) {
            return int.MaxValue;
        }

        return 5 - state.PlacedPieces[0].Position.X;
    }
}

public class FreeSpacesHeuristic : MonotoneHeuristic<RHGameState> {
    
    public override int Evaluate(RHGameState state) {
        // Assume the main car is facing right
    
        if (state.PlacedPieces[0].Position.Y != state.ExitPosition.Y) {
            return int.MaxValue;
        }

        // Adds distance of the main car and
        // how many cars are in its way
        int distance = 5 - state.PlacedPieces[0].Position.X;
        int blocks = 0;

        for (int i = state.PlacedPieces[0].Position.X + 1; i < 6; i++) {
            if (state[i, 2] != -1) {
                blocks++;
            }
        }

        return distance + blocks;
    }
}

public class MoverHeuristic : MonotoneHeuristic<RHGameState> {
    public override int Evaluate(RHGameState state) {
        // Assume the main car is facing right
        if (state.PlacedPieces[0].Position.Y != state.ExitPosition.Y) {
            return int.MaxValue;
        }

        // Adds distance of the main car and
        // how many moves it takes at least to push out each car of its way
        int distance = 5 - state.PlacedPieces[0].Position.X;
        int moves = 0;

        for (int i = state.PlacedPieces[0].Position.X + 1; i < 6; i++) {
            int index = state[i, 2]; 

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
                        int tile = state[i, j]; 
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
                        int tile = state[i, j]; 
                        if (tile != -1 && tile != index) {
                            costToPushUp++;
                        }
                    }

                    int costToPushDown = 4 - bot;
                    for (int j = 3; j < 5; j++) {
                        // add 1 for each vehicle that's in the way of 
                        // putting the bus to the bottom
                        int tile = state[i, j]; 
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

public struct Level {
    public string Title { get; init; }
    public RHGameState State { get; init; }
}

static class Levels {
    // return the title, and the state
    public static Level LoadLevel(int index) {
        if (index < 0 || index >= levelStrings.Length) {
            throw new ArgumentException($"Level index must be between 0 and {levelStrings.Length}");
        }
        
        try {
            return LoadLevelString(levelStrings[index]);
        } catch (Exception e) {
            throw new Exception($"Error loading level at index {index}: {e.Message}");
        }
    }
    public static Level LoadLevelString(string levelString) {
        string[] lvlS = levelString.Split("\n");
        if (lvlS.Length != 7) {
            throw new Exception($"Level is not formatted correctly.");
        }
        string title = lvlS[0];

        List<PlacedRHPiece> vehicles = new();

        for (int i = 1; i <= 6; i++) {
            string row = lvlS[i];
            for (int j = 0; j < 6; j++) {
                char c = row[j];
                
                // keep going until we find a capital letter
                if (Char.IsUpper(c)) {
                    PlacedRHPiece pp = GetPiece(lvlS, c, i, j);
                    if (pp.Piece is MainCar) {
                        // MainCar must be inserted to index 0
                        vehicles.Insert(0, pp);
                    } else {
                        vehicles.Add(pp);
                    }
                }
            }
        }

        return new Level {
            Title = title,
            State = new RHGameState(vehicles.ToArray())
        };
    }

    public static int LevelCount => levelStrings.Length;
    public static IEnumerable<Level> LoadLevels() {
        for(int i = 0; i < LevelCount; i++) {
            yield return LoadLevel(i);
        }
    }

    private static PlacedRHPiece GetPiece(string[] lvlS, char c, int i, int j) {
        if (lvlS[i][j] != c) {
            throw new ArgumentException($"The front of car labeled with {c} is not at cooridinates {i}, {j}.");
        }
        char lower = Char.ToLower(c);

        // find its neighbouring cell
        foreach (Direction d in Enum.GetValues<Direction>()) {
            if (GetChar(lvlS, i, j, d, 1) == lower) {
                RushHourPiece piece;
                if (GetChar(lvlS, i, j, d, 2) == lower) {
                    // we found a bus
                    piece = new Bus();
                } else {
                    // we found a car
                    if (c == 'A') {
                        piece = new MainCar();    
                    } else {
                        piece = new Car();
                    }
                }

                return new PlacedRHPiece(piece, new Vector2I(j, i - 1), d.GetOpposite());
            }
        }

        throw new Exception($"No neighbouring cell for {c} at ({i},{j}) contains its lower-case body.");
    }

    private static char? GetChar(string[] lvlS, int i, int j, Direction d, int tiles = 1) {
        
        int[,] directions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };

        int ni = i + directions[(int)d, 0] * tiles;
        int nj = j + directions[(int)d, 1] * tiles;

        if (ni < 1 || ni > 6 || nj < 0 || nj > 5){
            return null;
        }
        return lvlS[ni][nj];
    }

    // Capital letter marks the front of the vehicle
    public static readonly string[] levelStrings = [
        """
        Demos - Rectangle
        ......
        ......
        aA....
        bbB...
        ......
        ......
        """,
        """
        Demos - Hollowed cube
        ...b..
        ...B..
        aA....
        cC....
        ......
        ......
        """,
        """
        Beginner - Level 1
        bB...C
        e..h.c
        eaAh.c
        E..H..
        F...Dd
        f.ggG.
        """, // 1247
        // """
        // Beginner - Level 2
        // B..Ddd
        // b..c.E
        // aA.CFe
        // Kkk.fe
        // ..i.Gg
        // jJIHh.
        // """, // 21000+ 
        """
        Beginner - Level 3
        ......
        ......
        .aAC..
        .bBc.F
        .D.c.f
        .deE.f
        """, // 934
        """
        Beginner - Level 4
        b..c..
        b..c..
        BaAC..
        ..dggG
        ..D..f
        ..EeeF
        """, // 806
        """
        Beginner - Level 5
        Bb.C.h
        d..cgH
        daAcgI
        DffFGi
        E...jJ
        e...Kk
        """, // 2870
        """
        Beginner - Level 6
        bB.c..
        dD.CEf
        .aAGef
        hHigeF
        J.Ig..
        j..kkK
        """, // 3070
        // """
        // Beginner - Level 7
        // .BCcde
        // .b.FDE
        // .aAf.G
        // ..hH.g
        // ...i..
        // ...I..
        // """, // 8122
        """
        Beginner - Level 8
        ...nNm
        ..jJkm
        aAhiKM
        bBHIlL
        cCGeeE
        dDgffF
        """, // 952
        """
        Beginner - Level 9
        .bCcDd
        .B.LeE
        aA.lfG
        hIiifg
        h.j.FK
        h.J..k
        """, // 7517
        """
        Beginner - Level 10
        Bbc.dD
        eEC..F
        GaA..f
        ghhH.f
        g..IjJ
        Kk.ilL
        """, // 4846
        """
        Intermediate - Level 15
        bCcD..
        b..d..
        BaAd..
        ..eFff
        ..E..g
        ..hhHG
        """, // 1553 states
        """
        Intermediate - Level 16
        .BbcdD
        .eEC.f
        ..GaAF
        Hhg..i
        J.g..i
        jkKlLI
        """, // 2727
        // """
        // Intermediate - Level 18
        // .bc.dD
        // .Bc.eE
        // aAC..F
        // gHhh.f
        // GIij.f
        // ...JKk
        // """, // 18495+ nice little paths tho
        """
        Intermediate - Level 20
        .bC.dD
        ebcFfG
        EBaA.g
        hHiJ.g
        ..IjkK
        .lLmM.
        """, // 5750 states
        """
        Advanced - Level 24
        BCcdE.
        bF.DeG
        bf.aAg
        hhHI.g
        ..JikK
        LljmM.
        """, // 4780
        // """
        // Advanced - Level 25 - 14000+ states
        // bBc.Dd
        // e.C..F
        // e.aAGf
        // EHhhgf
        // ...IJj
        // Kk.i..
        // """,
        """
        Advanced - Level 28
        bbB.cD
        ....Cd
        EaA..f
        eghhHF
        eGiJkK
        lLIjmM
        """, // 3879
        // """
        // Advanced - Level 30 - 15000+ states No good too many
        // ..BCcc
        // dDbEF.
        // GaAef.
        // g..eHh
        // IiJj.K
        // lLmM.k
        // """,
        """
        Expert - Level 31
        bCcd.e
        BF.D.e
        .faAGE
        hHIjg.
        ..iJkK
        lLimM.
        """, // - 13500 states a bit too much
        """
        Expert - Level 34
        BCcdD.
        b.EFff
        aAeG.H
        IjJgKh
        il.gkm
        .LnnNM
        """, // 1469 states very nice
        """
        Expert - Level 35
        .bBcCd
        ..ef.D
        aAEF.G
        H.ijJg
        h.IkKg
        hlLmmM
        """, // 81
        """
        Expert - Level 39
        BccC..
        b.deFf
        aADe.g
        hHIE.G
        .ji..K
        .JLllk
        """,
        """
        Expert - Level 40
        Ddd.gH
        CbbBGh
        c.FaAi
        eEfL.I
        ...lJj
        kkKl..
        """, // 9358
        // """
        // Level ? Easy
        // ......
        // ..E.f.
        // aAe.fg
        // bbB.FG
        // CD..hH
        // cd..iI
        // """,
        // """
        // Level ?
        // ......
        // ..E.f.
        // aAe.fg
        // bbB.FG
        // CDJ.hH
        // cdj.iI
        // """, // 7171
        // """
        // Very easy
        // ...b..
        // ...b..
        // aA.B..
        // ..D...
        // E.dCcc
        // e...fF
        // """
    ];
}


public struct Move {
    public int PieceIndex { get; init; }
    public Direction Dir { get; init; }
}

public enum Direction {
    Right,
    Down,
    Left,
    Up
}

public static class DirectionMethods{
    public static Direction GetOpposite(this Direction d) {
        return d switch {
            Direction.Right => Direction.Left,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Up => Direction.Down,
            _ => throw new ArgumentOutOfRangeException(nameof(d), $"Invalid direction: {d}")
        };
    }

    public static Vector2I GetVector(this Direction d) {
        return d switch {
            Direction.Right => new Vector2I(1, 0),
            Direction.Down => new Vector2I(0, 1),
            Direction.Left => new Vector2I(-1, 0),
            Direction.Up => new Vector2I(0, -1),
            _ => throw new ArgumentOutOfRangeException(nameof(d), $"Invalid direction: {d}")
        };
    }

}

public class StateMove {
    public RHGameState From {get; init;}
    public RHGameState To {get; init;}
    public Move Move {get; init;}

    private int _hashCode;

    public StateMove(RHGameState from, RHGameState to, Move move) {
        From = from;
        To = to;
        Move = move;

        _hashCode = CalculateHashCode();
    }

    private int CalculateHashCode() {
        int hash1 = From.GetHashCode();
        int hash2 = To.GetHashCode();

        // order the hashes, we don't want the hash to be affected by order.
        if (hash2 < hash1) {
            int temp = hash2;
            hash2 = hash1;
            hash1 = temp;
        }

        int hash = 17;
        hash = hash * 31 + hash1;
        hash = hash * 31 + hash2;

        return hash;
    }

    public override int GetHashCode() {
        return _hashCode;
    }

    public override bool Equals(object? obj) {
        if (obj is not StateMove other) return false;
        if (GetHashCode() != other.GetHashCode()) return false;
        return (From == other.From && To == other.To) || (From == other.To && To == other.From); 
    }

    public static bool operator ==(StateMove? left , StateMove? right) {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;

        return left.Equals(right);
    }

    public static bool operator !=(StateMove? left , StateMove? right) {
        return !(left == right);
    }
}


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

public abstract class RushHourPiece{
    public override bool CanMoveLengthwise => true;
    public override bool CanMoveSideways => false;
    public abstract int Width { get; }
    public abstract int Length { get; }

    public abstract bool CanMoveLengthwise { get; }
    public abstract bool CanMoveSideways { get; }
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


public abstract class RHSolver {
    private SolverStatus _status = SolverStatus.NotStarted;
    public SolverStatus Status { 
        get => _status;
        protected set {
            _status = value;
            if (   _status == SolverStatus.Solved 
                || _status == SolverStatus.NoSolution 
                || _status == SolverStatus.Terminated
                || _status == SolverStatus.DiscoverEndAllFound 
                || _status == SolverStatus.DiscoverEndLimitReached) {
                OnTerminated();
            }
        } 
    }
    protected readonly Random rand = new Random();
    protected readonly float randomFactor;
    private RHGameState? _current;
    public virtual int StepCount {get; protected set;} = 0;
    protected RHGameState? _solvedStateFound;

    public event EventHandler<RHGameState>? NewCurrent;
    public event EventHandler<PathChangeArgs>? PathChange;
    public event EventHandler<StateMove>? NewEdge;
    public event EventHandler<SolverStatus>? Terminated;

    private bool _skipNewCurrent = false;

    protected void OnNewCurrent(RHGameState state) {
        _current = state;
        if (_skipNewCurrent) return;
        NewCurrent?.Invoke(this, state);
    } 
    protected void OnPathChange(PathChangeArgs args) => PathChange?.Invoke(this, args);
    protected void OnNewEdge(StateMove edge) => NewEdge?.Invoke(this, edge);
    protected void OnTerminated() {
        if (_current != null) {
            NewCurrent?.Invoke(this, _current);
        }

        Terminated?.Invoke(this, Status);
    } 
    
    public Heuristic<RHGameState> Heuristic { get; protected init; }

    public RHSolver(Heuristic<RHGameState> heuristic, float randomFactor = 0) {
        Heuristic = heuristic;
        this.randomFactor = randomFactor;
    }

    public float Evaluate(StateMove move) => Evaluate(move.To);

    public float Evaluate(RHGameState state){
        float result = Heuristic.Evaluate(state);
        if (randomFactor > 0) {
            result += rand.NextSingle() * randomFactor;
        }
        return result;
    }
    
    public virtual void Start(RHGameState initial) {
        Status = SolverStatus.Running;
        Extend(initial);
    }

    public virtual bool Extend(RHGameState state) {
        OnNewCurrent(state);

        if (state.IsSolved()) {
            _solvedStateFound = state;
            Status = SolverStatus.Solved;
            return true;
        }
        return false;
    }

    public void Step(int stepCount) {
        // Skip calling new currents when calling steps in batch
        _skipNewCurrent = true;
        for (int i = 0; i < stepCount - 1; i++) {
            Step();
            if (Status != SolverStatus.Running && Status != SolverStatus.Discovering) {
                // quit the function if the solver terminated
                _skipNewCurrent = false;
                return;
            }
        }
        _skipNewCurrent = false;
        Step();
    }

    public virtual void Step() => StepCount++;
    public IEnumerable<StateMove> GetSolutionPath() {
        if (Status != SolverStatus.Solved) {
            throw new Exception("Puzzle is not solved!");
        }
        return GetSolutionPathSolved();
    }
    protected abstract IEnumerable<StateMove> GetSolutionPathSolved();
}

public class TabuSolver : RHSolver {
    public IReadOnlyList<StateMove> Route => _route;
    private List<StateMove> _route = new();
    public int TabuSize { get; init; }
    private StateMove? _nextMove;

    public TabuSolver(Heuristic<RHGameState> h, int tabuSize, float rf = 0) : base(h, rf) {
        TabuSize = tabuSize;
    }

    public override void Start(RHGameState initial) {
        base.Start(initial);
        ChooseNext(initial);
    }

    public override void Step() {
        base.Step();
        if (_nextMove == null) {
            Status = SolverStatus.Terminated;
            return;
        }
        _route.Add(_nextMove);
        OnNewEdge(_nextMove);

        if (TabuSize > 0) {
            OnPathChange(new PathChangeArgs{ onPath = true, move = _nextMove});
            
            if (_route.Count > TabuSize) {
                OnPathChange(new PathChangeArgs{
                    onPath = false, 
                    move = _route[^(TabuSize + 1)]
                });
            }
        }

        // return early if the state is solved.
        if (Extend(_nextMove.To)) {
            return;
        }

        ChooseNext(_nextMove.To);
    }

    protected void ChooseNext(RHGameState state) {
        var stateMoves = state.GetPossibleStateMoves();

        int startIndex = Math.Max(0, Route.Count - TabuSize);

        IEnumerable<StateMove> validMoves = stateMoves.Where(
            // We don't check for loop edges, as those do not exist in our game.
            stateMove => 
                !_route[startIndex..].Select(
                    tabuMove => tabuMove.From
                ).Contains(stateMove.To)
        );

        if (!validMoves.Any()) {
            _nextMove = null;
            return;
        }

        _nextMove = validMoves.MinBy(Evaluate);
    }
    
    protected override List<StateMove> GetSolutionPathSolved() => _route;
}

public class BacktrackingSolver(Heuristic<RHGameState> h, float rf = 0) : RHSolver(h, rf) {
    private List<List<StateMove>> _currentRoute = new ();

    public override void Start(RHGameState initial) {
        base.Start(initial);
        AddOptions(initial);
    }

    public override void Step() { 
        base.Step();
        var options = _currentRoute.Last();
        if (options.Count == 0){
            _currentRoute.RemoveAt(_currentRoute.Count - 1);
            
            if (_currentRoute.Count == 0){
                Status = SolverStatus.NoSolution;
                return;
            }

            // There has to be a first, since we had an options after it
            var edgeToRemove = _currentRoute.Last().First();
            OnNewCurrent(edgeToRemove.From);
            OnPathChange(new PathChangeArgs {
                onPath = false,
                move = edgeToRemove
            });
            _currentRoute.Last().RemoveAt(0);

            return;
        }

        // add the state to the route, and discover it's neighbours
        var bestMove = _currentRoute.Last().First();
        
        OnNewEdge(bestMove);
        OnPathChange(new PathChangeArgs {	
            onPath = true,
            move = bestMove
        });

        // return early if the state is solved.
        if (Extend(bestMove.To)) {
            return;
        }

        AddOptions(bestMove.To);
    }

    protected void AddOptions(RHGameState state) {
        var stateMoves = state.GetPossibleStateMoves();

        var validMoves = stateMoves.Where(
            stateMove => 
                !_currentRoute.Select(
                    pathOptions => pathOptions.First().From
                ).Contains(stateMove.To)
        ) // filter out states already in the current route
        .OrderBy(Evaluate)
        .ToList();

        _currentRoute.Add(validMoves);
    }

    protected override IEnumerable<StateMove> GetSolutionPathSolved() {
        return _currentRoute.Select(options => options.First());
    }
}

public class AcGraphSolver(MonotoneHeuristic<RHGameState> h, float rf = 0) : RHSolver(h, rf) {
    protected PriorityQueue<StateMove, float> OpenStates { get; } = new ();

    protected struct DiscoveredState {
        public int depth;
        public StateMove? moveFromParent;
    }

    protected Dictionary<RHGameState, DiscoveredState> DiscoveredStates = new();

    public override void Start(RHGameState initial) {
        base.Start(initial);
        DiscoveredStates.Add(initial, new DiscoveredState{depth = 0, moveFromParent = null});
        AddOpenStates(initial);
    }

    public override void Step() { 
        base.Step();
        if(!OpenStates.TryDequeue(out StateMove? bestMove, out float p)) {
            Status = SolverStatus.NoSolution;
            return;
        }

        OnNewEdge(bestMove);
        OnPathChange(new PathChangeArgs{ onPath = true, move = bestMove});

        RHGameState extended = bestMove.To;

        // return early if the state is solved.
        if (Extend(extended)) {
            return;
        }

        AddOpenStates(extended);
    }

    public void AddOpenStates(RHGameState extended) {
        IEnumerable<StateMove> filteredMoves = extended.GetPossibleStateMoves().Where(
            stateMove => !DiscoveredStates.Keys.Contains(stateMove.To)
        );

        int depth = DiscoveredStates[extended].depth + 1;
        
        foreach (StateMove move in filteredMoves) {
            DiscoveredStates.Add(move.To, new DiscoveredState{depth = depth, moveFromParent = move});
            OpenStates.Enqueue(move, EvalWithDepth(move.To, depth));
        }
    }

    public virtual float EvalWithDepth(RHGameState state, int depth) {
        // balanced search:
        // f = g + h
        return depth + Evaluate(state);
        
        // A greater depth can be preferred when the sum is equal
        // to find the solution slightly faster 
        // return depth + Evaluate(state) * 1.00000001f;
    }


    protected override IEnumerable<StateMove> GetSolutionPathSolved() { 
        if (_solvedStateFound == null) {
            throw new Exception("Puzzle is not solved!");
        }

        List<StateMove> solutionPath = new List<StateMove>();
        StateMove? stateMove = DiscoveredStates[_solvedStateFound].moveFromParent;

        // Add the moves in reverse order, 
        // starting form the solution
        while (stateMove != null) {
            solutionPath.Add(stateMove);
            stateMove = DiscoveredStates[stateMove.From].moveFromParent;
        }
        solutionPath.Reverse();

        return solutionPath;
    }
}

public abstract class Discoverer : AcGraphSolver {
    private int _maxSteps;
    private int _stepCount = 0;
    public override int StepCount {
        get => _stepCount;
        protected set {
            _stepCount = value;
            if (_stepCount >= _maxSteps) {
                Status = SolverStatus.DiscoverEndLimitReached;
            }
        }
    }

    public Discoverer(MonotoneHeuristic<RHGameState> h, int maxStates) : base(h, 0) {
        _maxSteps = maxStates;
    }

    public override void Start(RHGameState initial) {
        Status = SolverStatus.Discovering;
        Extend(initial);
        DiscoveredStates.Add(initial, new DiscoveredState{depth = 0, moveFromParent = null});
        AddOpenStates(initial);
        // Count the initial state when discovering
        StepCount++;
    }

    public override bool Extend(RHGameState state) {
        OnNewCurrent(state);
        return false;
    }

    public override void Step() { 
        if(!OpenStates.TryDequeue(out StateMove? bestMove, out float p)) {
            Status = SolverStatus.DiscoverEndAllFound;
            return;
        }

        OnNewEdge(bestMove);

        RHGameState extended = bestMove.To;

        // We never check if the state is solved
        Extend(extended);

        AddOpenStates(extended);
        StepCount++;
    }
}

public class BFSDiscoverer(int maxStates) : Discoverer (new NullHeuristic(), maxStates) {}
public class DFSDiscoverer(int maxStates) : Discoverer (new NullHeuristic(), maxStates) {
    public override float EvalWithDepth(RHGameState state, int depth) => -depth;
} 

public struct PathChangeArgs {
    public bool onPath;
    public StateMove move;
}

public enum SolverStatus {
    NotStarted,
    Running,
    Solved,
    NoSolution,
    Terminated,
    Discovering,
    DiscoverEndAllFound,
    DiscoverEndLimitReached
}
