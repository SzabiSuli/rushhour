namespace rushhour.src.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public abstract class Solver {
    private SolverStatus _status = SolverStatus.NotStarted;
    public SolverStatus Status { 
        get => _status;
        set {
            _status = value;
            if (   _status == SolverStatus.Solved 
                || _status == SolverStatus.NoSolution 
                || _status == SolverStatus.Terminated) {
                OnTerminated();
            }
        } 
    }
    protected Random rand = new Random();
    protected float randomFactor;

    // TODO these shouldn't create vertices directly, 
    // but enqueue them for creation so, it does not slow down the algorithm.
    public event EventHandler<RHGameState>? NewCurrent;
    public event EventHandler<PathChangeArgs>? PathChange;
    public event EventHandler<StateMove>? NewEdge;
    public event EventHandler<SolverStatus>? Terminated;

    protected void OnNewCurrent(RHGameState state) => NewCurrent?.Invoke(this, state);
    protected void OnPathChange(PathChangeArgs args) => PathChange?.Invoke(this, args);
    protected void OnNewEdge(StateMove edge) => NewEdge?.Invoke(this, edge);
    protected void OnTerminated() => Terminated?.Invoke(this, Status);
    
    public Heuristic Heuristic { get; protected init; }

    public Solver(Heuristic heuristic, float randomFactor = 0) {
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

    // TODO might not need to return anything
    public bool Extend(RHGameState state) {
        OnNewCurrent(state);

        if (state.IsSolved()) {
            GD.Print("Found Solution!");
            state.PrintState();
            Status = SolverStatus.Solved;
            return true;
        }
        return false;
    }

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


    public List<StateMove> Route { get; set; } = new();
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
        base.Start(initial);
        ChooseNext(initial);
    }

    public override void Step() {
        if (nextMove == null) {
            Status = SolverStatus.Terminated;
            return;
        }
        Route.Add(nextMove);
        OnNewEdge(nextMove);
        OnPathChange(new PathChangeArgs{ onPath = true, move = nextMove});

        if (TabuSize > 0 && Route.Count > TabuSize) {
            OnPathChange(new PathChangeArgs{
                onPath = false, 
                move = Route[^(TabuSize + 1)]
            });
        }

        // return early if the state is solved.
        if (Extend(nextMove.To)) {
            return;
        }

        ChooseNext(nextMove.To);
    }

    protected void ChooseNext(RHGameState state) {
        var stateMoves = state.GetPossibleStateMoves();

        int startIndex = Math.Max(0, Route.Count - TabuSize);

        IEnumerable<StateMove> validMoves = stateMoves.Where(
            // We don't check for loop edges, as those do not exist in our game.
            stateMove => 
                !Route[startIndex..].Select(
                    tabuMove => tabuMove.From
                ).Contains(stateMove.To)
        );

        if (!validMoves.Any()) {
            nextMove = null;
            return;
        }

        nextMove = validMoves.MinBy(Evaluate);
    }
    
    public override List<StateMove> GetSolutionPath() { 
        // Implementation hidden
        return Route;
    }
}

public class BacktrackingSolver(Heuristic h, float rf = 0) : Solver(h, rf) {
    public List<List<StateMove>> CurrentRoute { get; } = new ();

    public override void Start(RHGameState initial) {
        base.Start(initial);
        AddOptions(initial);
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
                !CurrentRoute.Select(
                    pathOptions => pathOptions.First().From
                ).Contains(stateMove.To)
        ) // filter out states already in the current route
        .OrderBy(Evaluate)
        .ToList();

        CurrentRoute.Add(validMoves);
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
        base.Start(initial);
        DiscoveredStates.Add(initial, new DiscoveredState{depth = 0, parent = null});
        AddOpenStates(initial);
    }

    public override void Step() { 
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
            DiscoveredStates.Add(move.To, new DiscoveredState{depth = depth, parent = extended});
            OpenStates.Enqueue(move, EvalWithDepth(move.To, depth));
        }
    }

    public float EvalWithDepth(RHGameState state, int depth) {
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
