namespace rushhour.src.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public abstract class Solver {
    private SolverStatus _status = SolverStatus.NotStarted;
    public SolverStatus Status { 
        get => _status;
        protected set {
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
    private RHGameState? _current;
    public int StepCount {get; private set;} = 0;
    protected RHGameState? _solvedStateFound;

    // TODO these shouldn't create vertices directly, 
    // but enqueue them for creation so, it does not slow down the algorithm.
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
            if (Status != SolverStatus.Running) {
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

public class TabuSolver : Solver {
    public List<StateMove> Route { get; set; } = new();
    public int TabuSize { get; init; }
    public StateMove? nextMove;

    public TabuSolver(Heuristic h, int tabuSize, float rf = 0) : base(h, rf) {
        TabuSize = tabuSize;
    }

    public override void Start(RHGameState initial) {
        base.Start(initial);
        ChooseNext(initial);
    }

    public override void Step() {
        base.Step();
        if (nextMove == null) {
            Status = SolverStatus.Terminated;
            return;
        }
        Route.Add(nextMove);
        OnNewEdge(nextMove);

        if (TabuSize > 0) {
            OnPathChange(new PathChangeArgs{ onPath = true, move = nextMove});
            
            if (Route.Count > TabuSize) {
                OnPathChange(new PathChangeArgs{
                    onPath = false, 
                    move = Route[^(TabuSize + 1)]
                });
            }
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
    
    protected override List<StateMove> GetSolutionPathSolved() => Route;
}

public class BacktrackingSolver(Heuristic h, float rf = 0) : Solver(h, rf) {
    public List<List<StateMove>> CurrentRoute { get; } = new ();

    public override void Start(RHGameState initial) {
        base.Start(initial);
        AddOptions(initial);
    }

    public override void Step() { 
        base.Step();
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

    protected override IEnumerable<StateMove> GetSolutionPathSolved() {
        return CurrentRoute.Select(options => options.First());
    }
}

// TODO make A and A* searches?
public class AcGraphSolver(MonotoneHeuristic h, float rf = 0) : Solver(h, rf) {
    private PriorityQueue<StateMove, float> OpenStates { get; } = new ();

    struct DiscoveredState {
        public int depth;
        public StateMove? moveFromParent;
    }

    private Dictionary<RHGameState, DiscoveredState> DiscoveredStates = new();

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

    public float EvalWithDepth(RHGameState state, int depth) {
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

public struct PathChangeArgs {
    public bool onPath;
    public StateMove move;
}
