namespace rushhour.src.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

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
    // List<GameState> GetSolutionPath();


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
    
    public List<GameState>? GetSolutionPath() { 
        // Implementation hidden
        return null; 
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

	public IEnumerable<StateMove>? GetSolutionPath() { 
		if (Status != SolverStatus.Solved) {
			return null;
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


	// public IEnumerable<RHGameState> GetSolutionPath() { 
	//     if (!FoundSolution) {
	//         return null;
	//     } 
	//     return CurrentRoute.Select(tuple => tuple.Item1);
	// }
}
