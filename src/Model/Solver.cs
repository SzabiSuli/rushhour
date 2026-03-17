namespace rushhour.src.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public abstract class Solver {
	public SolverStatus Status { get; set; } = SolverStatus.NotStarted;

	public event EventHandler<RHGameState>? NewCurrent;
	public event EventHandler<PathChangeArgs>? PathChange;
	public event EventHandler<IEnumerable<StateMove>>? DiscoveredEdges;

	protected void OnNewCurrent(RHGameState state) => NewCurrent?.Invoke(this, state);
	protected void OnPathChange(PathChangeArgs args) => PathChange?.Invoke(this, args);
	protected void OnDiscoveredEdges(IEnumerable<StateMove> edges) => DiscoveredEdges?.Invoke(this, edges);
	
	public Heuristic Heuristic { get; protected init; }

	public Solver(Heuristic heuristic) {
		Heuristic = heuristic;
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

		// We expect ProcessMoves to have side effects
		// such as putting the filteredMoves in a structure
		// where in the next step the best move is selected
		IEnumerable<StateMove> filteredMoves = ProcessMoves(stateMoves);
		
		if (filteredMoves.Any()) {
			OnDiscoveredEdges(filteredMoves);
		}

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



	Random rand = new Random();

	public List<RHGameState> Route { get; set; } = new();
	public int TabuSize { get; init; }
	public StateMove? nextMove;

	// public RHGameState? Parent { get; set; }

	// public RHGameState Current { get; set; }

	// public Heuristic Heuristic { get; set; }
	
	// public bool FoundSolution { get; set; }
	// public bool Terminated { get; set; }
	public TabuSolver(Heuristic h, int tabuSize) : base(h) {
		TabuSize = tabuSize;
	}

	public override void Start(RHGameState initial) {
		Route.Add(initial);
		base.Start(initial);
	}

	public override IEnumerable<StateMove> ProcessMoves(IEnumerable<StateMove> stateMoves) {
		IEnumerable<StateMove> validMoves = stateMoves.Where(
			stateMove => !Route[(Route.Count - TabuSize)..].Contains(stateMove.To)
		);

		if (!validMoves.Any()) return validMoves;

		nextMove = validMoves.MaxBy(stateMove => Heuristic.Evaluate(stateMove.To) + rand.NextDouble());

		// we filter out edges that go to the tabu
		// but some edges are already discovered,
		// those will be handled by the edge creator.
		OnDiscoveredEdges(validMoves);
		
		return validMoves;
	}

	public override void Step() {

		if (nextMove == null) {
			// Status = SolverStatus.
		}

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

public class BacktrackingSolver(Heuristic h) : Solver(h) {

	Random rand = new Random();
	public List<List<StateMove>> CurrentRoute { get; } = new ();

	public override IEnumerable<StateMove> ProcessMoves(IEnumerable<StateMove> stateMoves) {
		var validMoves = stateMoves.Where(
			stateMove => 
				!CurrentRoute.Select(
					pathOptions => pathOptions.First().From
				).Contains(stateMove.To)
		) // filter out states already in the current route
		// TODO random thing
		.OrderBy(stateMove => Heuristic.Evaluate(stateMove.To) + rand.NextDouble())
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
public class AcGraphSolver(MonotoneHeuristic h) : Solver(h) {
	public PriorityQueue<StateMove, int> OpenStates { get; } = new ();

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
		if(OpenStates.TryDequeue(out var move, out int p)) {
			OnPathChange(new PathChangeArgs{ onPath = true, move = move});
			Extend(move.To);
		} else {
			Status = SolverStatus.NoSolution;
		}
	}

	public override IEnumerable<StateMove> ProcessMoves(IEnumerable<StateMove> stateMoves) {
		// We are calling stateMoves that are already drawn on the graph,
		// bit its more efficitent, that filtering based on open states,
		// which would have to traverse the whole priority queue.
		OnDiscoveredEdges(stateMoves);

		var validMoves = stateMoves.Where(
			stateMove => !DiscoveredStates.Keys.Contains(stateMove.To)
		);

		if (!validMoves.Any()) return validMoves;


		RHGameState parent = validMoves.First().From;
		int depth = DiscoveredStates[parent].depth + 1;
		
		foreach (var move in validMoves) {
			DiscoveredStates.Add(move.To, new DiscoveredState{depth = depth, parent = parent});
			OpenStates.Enqueue(move, EvalPriority(depth, move.To));
		}

		return validMoves;
	}

	public int EvalPriority(int depth, RHGameState state) {
		// balanced search:
		// f = g + h
		return depth + Heuristic.Evaluate(state);
	}


	// public IEnumerable<RHGameState> GetSolutionPath() { 
	//     if (!FoundSolution) {
	//         return null;
	//     } 
	//     return CurrentRoute.Select(tuple => tuple.Item1);
	// }
}
