namespace rushhour.src.Model;


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using rushhour.src.Nodes;

public abstract class Solver {
	public SolverStatus Status { get; set; } = SolverStatus.NotStarted;

	public event EventHandler<RHGameState>? NewCurrent;
	public event EventHandler<PathChangeArgs>? PathChange;
	public event EventHandler<List<StateMove>>? DiscoveredEdges;

	protected void OnNewCurrent(RHGameState state) => NewCurrent?.Invoke(this, state);
	protected void OnPathChange(PathChangeArgs args) => PathChange?.Invoke(this, args);
	protected void OnDiscoveredEdges(List<StateMove> edges) => DiscoveredEdges?.Invoke(this, edges);
	
	public Heuristic Heuristic { get; protected set; }

	public Solver(Heuristic heuristic) {
		Heuristic = heuristic;
	}


	// void Step();
	// List<GameState> GetSolutionPath();


	// TimeSpan StepDelay { get; set; }
}

public class HillClimberSolver(Heuristic h) : Solver(h) {
	// TODO make this tabu search
	// public bool IsRunning { get; private set; }
	// public bool FoundSolution { get; private set; }
	// public HashSet<GraphNode> WorkingSetNodes { get; } = new HashSet<GraphNode>();
	// public HashSet<GraphEdge> WorkingSetEdges { get; } = new HashSet<GraphEdge>();
	// public TimeSpan StepDelay { get; set; }

	Random rand = new Random();


	public RHGameState? Parent { get; set; }

	public RHGameState Current { get; set; }

	// public Heuristic Heuristic { get; set; }
	
	public bool FoundSolution { get; set; }
	public bool Terminated { get; set; }
	// public HillClimberSolver(Heuristic heuristic, RHGameState initialState){

	// 	Heuristic = heuristic;
	// 	Current = initialState;
	// 	Parent = null;
	// 	FoundSolution = false;
	// }

	public void Step() { 

		if (Current.IsSolved()){
			FoundSolution = true;
			Terminated = true;
			return;
		}

		var possibleMoves = Current.GetPossibleMoves();
		if (!possibleMoves.Any()){
			Terminated = true;
			return; 
		}

		if (possibleMoves.Count() == 1){
			// the only neighbour is the parent
			// TODO it can also be the initial state with one option
			RHGameState temp = Current;
			Current = Parent!;
			Parent = temp;
			return;
		}

		var possibleStates = possibleMoves.Select(
			Current.WithMove 
		).ToList();


		var orderedStates = possibleStates.OrderBy(
			state => Heuristic.Evaluate(state) + rand.NextDouble()
		);

		RHGameState bestMove;
		if (orderedStates.First() == Parent!){
			bestMove = orderedStates.Skip(1).First();
		} else {
			bestMove = orderedStates.First();
		}

		Parent = Current;
		Current = bestMove;
	}
	
	public List<GameState>? GetSolutionPath() { 
		// Implementation hidden
		return null; 
	}
}

public class BacktrackingSolver(Heuristic h) : Solver(h) {
	// public bool IsRunning { get; private set; }
	// public HashSet<GraphNode> WorkingSetNodes { get; } = new HashSet<GraphNode>();
	// public HashSet<GraphEdge> WorkingSetEdges { get; } = new HashSet<GraphEdge>();
	// public TimeSpan StepDelay { get; set; }

	public List<List<StateMove>> CurrentRoute { get; } = new ();

    public void Start(RHGameState initialState) {
		Status = SolverStatus.Running;

		// TODO Maybe not here
		MainScene.Instance.GetOrCreateVertex(initialState, null);

		Extend(initialState);
	}

	private void Extend(RHGameState initial){
		OnNewCurrent(initial);

		var moves = initial.GetPossibleMoves();
		
		if (!moves.Any()) return;

		var stateMoves = moves
		.Select(move => new StateMove(initial, initial.WithMove(move), move))
		// Note: we don't filter out moves that end up in the same state as the current last one, 
		// because they don't exist in our context
		.Where(stateMove => !CurrentRoute.Select(pathOptions => pathOptions.First().From).Contains(stateMove.To)) // filter out states already in the current route
		.OrderBy(stateMove => Heuristic.Evaluate(stateMove.To))
		.ToList();

		if (stateMoves.Count > 0) {
			OnDiscoveredEdges(stateMoves);
		}

		CurrentRoute.Add(stateMoves);
	}

	// returns true if the extended edge leads to a solution
	private bool Extend(StateMove bestMove){
		OnPathChange(new PathChangeArgs {	
			onPath = true,
			move = bestMove
		});
		OnNewCurrent(bestMove.To);

		if (bestMove.To.IsSolved()) {
			return true;
		}

		var moves = bestMove.To.GetPossibleMoves();
		var stateMoves = moves
		.Select(move => new StateMove(bestMove.To, bestMove.To.WithMove(move), move))
		// Note: we don't filter out moves that end up in the same state as the current last one, 
		// because they don't exist in our context
		.Where( 
			stateMove => // filter out states already in the current route
			!CurrentRoute.Select(pathOptions => pathOptions.First().From).Contains(stateMove.To)
		) 
		.OrderBy(stateMove => Heuristic.Evaluate(stateMove.To))
		.ToList();

		if (stateMoves.Count > 0) {
			OnDiscoveredEdges(stateMoves);
		}

		CurrentRoute.Add(stateMoves);

		return false;
	}

	public void Step() { 
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
		if (Extend(CurrentRoute.Last().First())) {
			GD.Print("Found Solution!");
			CurrentRoute.Last().First().To.PrintState();
			Status = SolverStatus.Solved;
		}
	}

	public IEnumerable<StateMove>? GetSolutionPath() { 
		if (Status != SolverStatus.Solved) {
			return null;
		} 
		return CurrentRoute.Select(options => options.First());
	}
}

public class GraphSolver(Heuristic h) : Solver(h) {
	// public HashSet<GraphNode> WorkingSetNodes { get; } = new HashSet<GraphNode>();
	// public HashSet<GraphEdge> WorkingSetEdges { get; } = new HashSet<GraphEdge>();
	// public TimeSpan StepDelay { get; set; }

	public PriorityQueue<RHGameState, int> OpenStates { get; } = new ();
	// public RHGameState Current { get; set; }

	Dictionary<RHGameState, List<RHGameState>> RoutesGraph = new();

	// public RHGameState? Current => CurrentRoute.Last()?.Item1;


	public void Start(RHGameState initialState){
		OpenStates.Enqueue(initialState, 0);
	}

	public void Step() { 
		var state = OpenStates.Dequeue();
		Extend(state);
	}

	private void Extend(RHGameState state) {
		
	}

	// public IEnumerable<RHGameState> GetSolutionPath() { 
	//     if (!FoundSolution) {
	//         return null;
	//     } 
	//     return CurrentRoute.Select(tuple => tuple.Item1);
	// }
}
