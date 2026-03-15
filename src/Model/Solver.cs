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
	
	public Heuristic Heuristic { get; protected set; }

	public Solver(Heuristic heuristic) {
		Heuristic = heuristic;
	}

	

	public void Start(RHGameState initialState) {
		Status = SolverStatus.Running;
		Extend(initialState);
	}

	public bool Extend(RHGameState state) {
		OnNewCurrent(state);

		if (state.IsSolved()) {
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

	public List<RHGameState> Route { get; set; } = new();
	public int TabuSize { get; set; }

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

	public override IEnumerable<StateMove> ProcessMoves(IEnumerable<StateMove> stateMoves) {
		return new List<StateMove>();
	}

	public void Step() {

		if (Route.Last().IsSolved()) {
			Status = SolverStatus.Solved;
			return;
		}


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
		var bestMove = CurrentRoute.Last().First();
		
		OnPathChange(new PathChangeArgs {	
			onPath = true,
			move = bestMove
		});

		if (Extend(bestMove.To)) {
			GD.Print("Found Solution!");
			bestMove.To.PrintState();
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
