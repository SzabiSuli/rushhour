
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public PlacedRHPiece[] PlacedPieces { get; init;}
    private const int maxPieces = 16;
    public int BoardWidth => 6;
    public int BoardHeight => 6;
    // public PlacedRHPiece MainCar {get; protected set;}
    public int[,] BoardGrid { get; init;}
    public Vector2I ExitPosition => new Vector2I(BoardWidth - 1, 2);

    private readonly int _hashCode;


    public RHGameState(PlacedRHPiece[] placedPieces) : base(){
        if (placedPieces.Length > maxPieces){
            throw new System.Exception($"Too many pieces: {placedPieces.Length} (max {maxPieces})");
        }
        PlacedPieces = placedPieces; // Does NOT copy the array
        if (PlacedPieces.Length == 0 || PlacedPieces[0].Piece is not MainCar){
            throw new System.Exception($"First piece placedPieces must be the main car");
        }

        // initialize board grid with all numbers set to -1
        BoardGrid = new int[BoardWidth, BoardHeight];
        for (int x = 0; x < BoardWidth; x++){
            for (int y = 0; y < BoardHeight; y++){
                BoardGrid[x, y] = -1;
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
                if (BoardGrid[x, y] != -1){
                    throw new System.Exception($"Piece overlaps with existing piece at position ({x}, {y})");
                }
                BoardGrid[x, y] = i;
            }
        }

        _hashCode = CalculateHashCode();
    }

    public bool IsSolved() => PlacedPieces[0].Position == ExitPosition;
    
    public override int GetHashCode() => _hashCode;

    private int CalculateHashCode() {
        int hash = 17;
        for (int i = 0; i < PlacedPieces.Length; i++){
            PlacedRHPiece piece = PlacedPieces[i];
            // hash = hash * 31 + i.GetHashCode();
            hash = hash * 31 + piece.Position.GetHashCode();
        }
        return hash;
    }

    public override bool Equals(object? obj){
        if (obj is not RHGameState other) return false;
        if (GetHashCode() != other.GetHashCode()) return false;
        if (PlacedPieces.Length != other.PlacedPieces.Length) return false;
        for (int i = 0; i < PlacedPieces.Length; i++){
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
        for (int i = 0; i < PlacedPieces.Length; i++){
            var placedPiece = PlacedPieces[i];
            switch (placedPiece.FacingDirection){
                case Direction.Up:
                    if (placedPiece.Position.Y - 1 >= 0 && BoardGrid[placedPiece.Position.X, placedPiece.Position.Y - 1] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Up};
                    }
                    if (placedPiece.Position.Y + placedPiece.Piece.Length < BoardHeight && BoardGrid[placedPiece.Position.X, placedPiece.Position.Y + placedPiece.Piece.Length] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Down};
                    }
                    break;
                case Direction.Down:
                    if (placedPiece.Position.Y + 1 < BoardHeight && BoardGrid[placedPiece.Position.X, placedPiece.Position.Y + 1] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Down};
                    }
                    if (placedPiece.Position.Y - placedPiece.Piece.Length >= 0 && BoardGrid[placedPiece.Position.X, placedPiece.Position.Y - placedPiece.Piece.Length] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Up};
                    }
                    break;
                case Direction.Left:
                    if (placedPiece.Position.X - 1 >= 0 && BoardGrid[placedPiece.Position.X - 1, placedPiece.Position.Y] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Left};
                    }
                    if (placedPiece.Position.X + placedPiece.Piece.Length < BoardWidth && BoardGrid[placedPiece.Position.X + placedPiece.Piece.Length, placedPiece.Position.Y] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Right};
                    }
                    break;
                case Direction.Right:
                    if (placedPiece.Position.X + 1 < BoardWidth && BoardGrid[placedPiece.Position.X + 1, placedPiece.Position.Y] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Right};
                    }
                    if (placedPiece.Position.X - placedPiece.Piece.Length >= 0 && BoardGrid[placedPiece.Position.X - placedPiece.Piece.Length, placedPiece.Position.Y] == -1){
                        yield return new Move{PieceIndex = i, Dir = Direction.Left};
                    }
                    break;
            }
        }
        yield break;
    }

    public RHGameState WithMove(Move move){
        // create a shallow copy of the placed pieces array
        PlacedRHPiece[] newPlacedPieces = (PlacedRHPiece[])PlacedPieces.Clone();
        // copy the piece to move and update its position
        var movedPiece = newPlacedPieces[move.PieceIndex].WithMove(move.Dir);
        newPlacedPieces[move.PieceIndex] = movedPiece;

        return new RHGameState(newPlacedPieces);
    }

    public void PrintState(){
        StringBuilder sb = new StringBuilder();
        for (int y = 0; y < BoardHeight; y++){
            for (int x = 0; x < BoardWidth; x++){
                var i = BoardGrid[x, y];
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


static class Levels {
    // return the title, and the state
    public static (string, RHGameState) LoadLevel(int index) {
        if (index < 0 || index >= levelStrings.Length) {
            throw new ArgumentException($"Level index must be between 0 and {levelStrings.Length}");
        }
        string[] lvlS = levelStrings[index].Split("\n");
        if (lvlS.Length != 7) {
            throw new Exception($"Level {index} is not formatted correctly.");
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

        return (title, new RHGameState(vehicles.ToArray()));
    }

    public static PlacedRHPiece GetPiece(string[] lvlS, char c, int i, int j) {
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

    public static char? GetChar(string[] lvlS, int i, int j, Direction d, int tiles = 1) {
        
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
        Template
        aA....
        ......
        ......
        ......
        ......
        ......
        """,
        """
        Square
        aA....
        bB....
        ......
        ......
        ......
        ......
        """,
        """
        Cube
        ......
        aA....
        ......
        bB....
        cC....
        ......
        """,
        """
        Level 1
        bB...C
        e..h.c
        eaAh.c
        E..H..
        F...Dd
        f.ggG.
        """,
        """
        Level 8
        ...nNm
        ..jJkm
        aAhiKM
        bBHIlL
        cCGeeE
        dDgffF
        """,
        """
        Level ? Easy
        ......
        ..E.f.
        aAe.fg
        bbB.FG
        CD..hH
        cd..iI
        """,
        """
        Level ?
        ......
        ..E.f.
        aAe.fg
        bbB.FG
        CDJ.hH
        cdj.iI
        """
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

public struct PathChangeArgs {
    public bool onPath;
    public StateMove move;
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

public abstract class RushHourPiece : Piece {
    public override bool CanMoveLengthwise => true;
    public override bool CanMoveSideways => false;
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


public abstract class Solver {
    public SolverStatus Status { get; set; } = SolverStatus.NotStarted;
    protected Random rand = new Random();
    protected float randomFactor;

    public event EventHandler<RHGameState>? NewCurrent;
    public event EventHandler<PathChangeArgs>? PathChange;
    public event EventHandler<IEnumerable<StateMove>>? DiscoveredEdges;
    public event EventHandler<StateMove>? NewEdge;

    protected void OnNewCurrent(RHGameState state) => NewCurrent?.Invoke(this, state);
    protected void OnPathChange(PathChangeArgs args) => PathChange?.Invoke(this, args);
    protected void OnDiscoveredEdges(IEnumerable<StateMove> edges) => DiscoveredEdges?.Invoke(this, edges);
    protected void OnNewEdge(StateMove edge) => NewEdge?.Invoke(this, edge);
    
    public Heuristic Heuristic { get; protected init; }

    public Solver(Heuristic heuristic, float randomFactor = 0) {
        Heuristic = heuristic;
        this.randomFactor = randomFactor;
    }

    public float Evaluate(StateMove move) => Evaluate(move.To);

    public float Evaluate(RHGameState state){
        if (randomFactor > 0) {
            return Heuristic.Evaluate(state) + rand.NextSingle() * randomFactor;
        }
        return Heuristic.Evaluate(state);
    }
    
    public virtual void Start(RHGameState initial) {
        Status = SolverStatus.Running;
        Extend(initial);
    }

    // TODO might not need to return anything
    public bool Extend(RHGameState state) {
        OnNewCurrent(state);

        if (state.IsSolved()) {
            GD.Print("Found Solution!");
            state.PrintState();
            Status = SolverStatus.Solved;
            return true;
        }

        var moves = state.GetPossibleMoves();
        
        if (!moves.Any()) return false;

        IEnumerable<StateMove> stateMoves = moves.Select(
            move => new StateMove(state, state.WithMove(move), move)
        );

        OnDiscoveredEdges(stateMoves);

        // We expect ProcessMoves to have side effects
        // such as putting the filteredMoves in a structure
        // where in the next step the best move is selected
        IEnumerable<StateMove> filteredMoves = ProcessMoves(stateMoves);

        return false;
    }

    public abstract IEnumerable<StateMove> ProcessMoves(IEnumerable<StateMove> stateMoves);


    public abstract void Step();
    public abstract IEnumerable<StateMove> GetSolutionPath();


    // TimeSpan StepDelay { get; set; }
}

public class TabuSolver : Solver {
    // TODO make this tabu search
    // public bool IsRunning { get; private set; }
    // public bool FoundSolution { get; private set; }
    // public HashSet<GraphNode> WorkingSetNodes { get; } = new HashSet<GraphNode>();
    // public HashSet<GraphEdge> WorkingSetEdges { get; } = new HashSet<GraphEdge>();
    // public TimeSpan StepDelay { get; set; }


    public List<RHGameState> Route { get; set; } = new();
    public int TabuSize { get; init; }
    public StateMove? nextMove;

    // public RHGameState? Parent { get; set; }

    // public RHGameState Current { get; set; }

    // public Heuristic Heuristic { get; set; }
    
    // public bool FoundSolution { get; set; }
    // public bool Terminated { get; set; }
    public TabuSolver(Heuristic h, int tabuSize, float rf = 0) : base(h, rf) {
        TabuSize = tabuSize;
    }

    public override void Start(RHGameState initial) {
        Route.Add(initial);
        base.Start(initial);
    }

    public override IEnumerable<StateMove> ProcessMoves(IEnumerable<StateMove> stateMoves) {
        int startIndex = Math.Max(0, Route.Count - TabuSize);

        IEnumerable<StateMove> validMoves = stateMoves.Where(
            stateMove => !Route[startIndex..].Contains(stateMove.To)
        );

        if (!validMoves.Any()) return validMoves;

        nextMove = validMoves.MaxBy(Evaluate);

        return validMoves;
    }

    public override void Step() {
        if (nextMove == null) {
            Status = SolverStatus.Terminated;
            return;
        }
        Route.Add(nextMove.To);
        OnNewEdge(nextMove);
        OnPathChange(new PathChangeArgs{ onPath = true, move = nextMove});

        int startIndex = Math.Max(0, Route.Count - TabuSize);

        // if ()

        Extend(nextMove.To);

        // if (Route.Last().IsSolved()) {
        // 	Status = SolverStatus.Solved;
        // 	return;
        // }


        // if (Current.IsSolved()){
        // 	FoundSolution = true;
        // 	Terminated = true;
        // 	return;
        // }

        // var possibleMoves = Current.GetPossibleMoves();
        // if (!possibleMoves.Any()){
        // 	Terminated = true;
        // 	return; 
        // }

        // if (possibleMoves.Count() == 1){
        // 	// the only neighbour is the parent
        // 	// TODO it can also be the initial state with one option
        // 	RHGameState temp = Current;
        // 	Current = Parent!;
        // 	Parent = temp;
        // 	return;
        // }

        // var possibleStates = possibleMoves.Select(
        // 	Current.WithMove 
        // ).ToList();


        // var orderedStates = possibleStates.OrderBy(
        // 	state => Heuristic.Evaluate(state) + rand.NextDouble()
        // );

        // RHGameState bestMove;
        // if (orderedStates.First() == Parent!){
        // 	bestMove = orderedStates.Skip(1).First();
        // } else {
        // 	bestMove = orderedStates.First();
        // }

        // Parent = Current;
        // Current = bestMove;
    }
    
    public override List<StateMove> GetSolutionPath() { 
        // Implementation hidden
        return new List<StateMove>(); 
    }
}

public class BacktrackingSolver(Heuristic h, float rf = 0) : Solver(h, rf) {
    public List<List<StateMove>> CurrentRoute { get; } = new ();

    public override IEnumerable<StateMove> ProcessMoves(IEnumerable<StateMove> stateMoves) {
        var validMoves = stateMoves.Where(
            stateMove => 
                !CurrentRoute.Select(
                    pathOptions => pathOptions.First().From
                ).Contains(stateMove.To)
        ) // filter out states already in the current route
        // TODO random thing
        .OrderBy(Evaluate)
        .ToList();

        CurrentRoute.Add(validMoves);
        
        return validMoves;
    }

    public override void Step() { 
        var options = CurrentRoute.Last();
        if (options.Count == 0){
            CurrentRoute.RemoveAt(CurrentRoute.Count - 1);
            
            if (CurrentRoute.Count == 0){
                Status = SolverStatus.NoSolution;
                return;
            }

            // There has to be a first, since we had an options after it
            var edgeToRemove = CurrentRoute.Last().First();
            OnNewCurrent(edgeToRemove.From);
            OnPathChange(new PathChangeArgs {
                onPath = false,
                move = edgeToRemove
            });
            CurrentRoute.Last().RemoveAt(0);

            return;
        }

        // add the state to the route, and discover it's neighbours
		var bestMove = CurrentRoute.Last().First();
		
        OnNewEdge(bestMove);
		OnPathChange(new PathChangeArgs {	
			onPath = true,
			move = bestMove
		});

		Extend(bestMove.To);
	}

	public override IEnumerable<StateMove> GetSolutionPath() { 
		if (Status != SolverStatus.Solved) {
			throw new Exception("Puzzle is not solved!");
		} 
		return CurrentRoute.Select(options => options.First());
	}
}

// TODO make A and A* searches?
public class AcGraphSolver(MonotoneHeuristic h, float rf = 0) : Solver(h, rf) {
	public PriorityQueue<StateMove, float> OpenStates { get; } = new ();

	struct DiscoveredState {
		public int depth;
		public RHGameState? parent;
	}

	Dictionary<RHGameState, DiscoveredState> DiscoveredStates = new();

	public override void Start(RHGameState initial) {
		DiscoveredStates.Add(initial, new DiscoveredState{depth = 0, parent = null});
		base.Start(initial);
	}

	public override void Step() { 
		if(OpenStates.TryDequeue(out var move, out float p)) {
            OnNewEdge(move);
			OnPathChange(new PathChangeArgs{ onPath = true, move = move});
			Extend(move.To);
		} else {
			Status = SolverStatus.NoSolution;
		}
	}

	public override IEnumerable<StateMove> ProcessMoves(IEnumerable<StateMove> stateMoves) {
		var validMoves = stateMoves.Where(
			stateMove => !DiscoveredStates.Keys.Contains(stateMove.To)
		);

		if (!validMoves.Any()) return validMoves;


		RHGameState parent = validMoves.First().From;
		int depth = DiscoveredStates[parent].depth + 1;
		
		foreach (var move in validMoves) {
			DiscoveredStates.Add(move.To, new DiscoveredState{depth = depth, parent = parent});
			OpenStates.Enqueue(move, EvalWithDepth(depth, move.To));
		}

		return validMoves;
	}

	public float EvalWithDepth(int depth, RHGameState state) {
		// balanced search:
		// f = g + h
		return depth + Evaluate(state);
	}


	public override IEnumerable<StateMove> GetSolutionPath() { 
	    if (Status != SolverStatus.Solved) {
			throw new Exception("Puzzle is not solved!");
	    } 

        return new List<StateMove>();
	    // return CurrentRoute.Select(tuple => tuple.Item1);
	}
}

public enum SolverStatus {
    NotStarted,
    Running,
    Solved,
    NoSolution,
    Terminated
}
