namespace rushhour.src.Model;


using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using rushhour.src.Nodes;

public interface ISolver {
	// bool IsRunning { get; }
	bool FoundSolution { get; set;}
	bool Terminated { get; }
	
	// HashSet<GraphNode> WorkingSetNodes { get; }
	// HashSet<GraphEdge> WorkingSetEdges { get; }

	void Step();
	List<GameState> GetSolutionPath();


	// TimeSpan StepDelay { get; set; }
}

public class HillClimberSolver : ISolver {
	// public bool IsRunning { get; private set; }
	// public bool FoundSolution { get; private set; }
	// public HashSet<GraphNode> WorkingSetNodes { get; } = new HashSet<GraphNode>();
	// public HashSet<GraphEdge> WorkingSetEdges { get; } = new HashSet<GraphEdge>();
	// public TimeSpan StepDelay { get; set; }

	Random rand = new Random();


	#nullable enable
	public RHGameState? Parent { get; set; }
	#nullable disable

	public RHGameState Current { get; set; }

	public Heuristic Heuristic { get; set; }
	
	public bool FoundSolution { get; set; }
	public bool Terminated { get; set; }
	public HillClimberSolver(Heuristic heuristic, RHGameState initialState){

		Heuristic = heuristic;
		Current = initialState;
		Parent = null;
		FoundSolution = false;
	}

	public void Step() { 

		if (Current.IsSolved()){
			FoundSolution = true;
			Terminated = true;
			return;
		}

		var possibleMoves = Current.GetPossibleMoves();
		if (possibleMoves.Count() == 0){
			Terminated = true;
			return; 
		}

		if (possibleMoves.Count() == 1){
			// the only neighbour is the parent
			RHGameState temp = Current;
			Current = Parent;
			Parent = temp;
			return;
		}

		var possibleStates = possibleMoves.Select(
			Current.WithMove 
		).ToList();


		var orderedStates = possibleStates.OrderBy(
			// TODO add random value between 1 and 0
			state => Heuristic.Evaluate(state) + rand.NextDouble()
		);

		RHGameState bestMove;
		if (orderedStates.First() == Parent){
			bestMove = orderedStates.Skip(1).First();
		} else {
			bestMove = orderedStates.First();
		}

		Parent = Current;
		Current = bestMove;
	}
	
	public List<GameState> GetSolutionPath() { 
		// Implementation hidden
		return null; 
	}
}

public class BacktrackingSolver  {
	// public bool IsRunning { get; private set; }
	public bool FoundSolution { get; private set; }
	public bool Terminated { get; private set; }
	// public HashSet<GraphNode> WorkingSetNodes { get; } = new HashSet<GraphNode>();
	// public HashSet<GraphEdge> WorkingSetEdges { get; } = new HashSet<GraphEdge>();
	// public TimeSpan StepDelay { get; set; }

	public List<Tuple<RHGameState, List<RHGameState>>> CurrentRoute { get; } = new ();
	// public RHGameState Current { get; set; }
	Heuristic Heuristic { get; set; }

	public RHGameState? Current => CurrentRoute.LastOrDefault()?.Item1;

	

	public MainScene MainScene { get; set; }

	public BacktrackingSolver(Heuristic heuristic, RHGameState initialState, MainScene mainScene){

		// TODO fix it not terminating

		Heuristic = heuristic;
		MainScene = mainScene;
		// Current = initialState;
		FoundSolution = false;
		Terminated = false;

		AddAndExtend(initialState, true);
	}

	private void AddAndExtend(RHGameState state, bool isFixed = false){
		var moves = state.GetPossibleMoves();
		var neighbours = moves
		.Select(move => (move, state.WithMove(move)))
		.Where(move_and_state => !CurrentRoute.Select(tuple => tuple.Item1).Contains(move_and_state.Item2)) // filter out states already in the current route
		.OrderBy(move_and_state => Heuristic.Evaluate(move_and_state.Item2))
		.ToList();
		
		CurrentRoute.Add(new (state, neighbours.Select(x => x.Item2).ToList()));
		Vertex from = MainScene.GetOrCreateVertex(state);
		from.IsFixed = isFixed;
		foreach (var move_and_state in neighbours){
			Vertex to = MainScene.GetOrCreateVertex(move_and_state.Item2);
			MainScene.GetOrCreateEdge(from, to, move_and_state.Item1);
		}
	}

	public void Step() { 

		// DEBUG
		for (int i = 0; i < CurrentRoute.Count; i++){
			GD.Print("--------------------------------");
			GD.Print($"Vertex {i}.:");
			CurrentRoute[i].Item1.PrintState();
			GD.Print($"{CurrentRoute[i].Item2.Count} Neighbours:");
			foreach (var neighbour in CurrentRoute[i].Item2){
				neighbour.PrintState();
				GD.Print("-------");
			}
			GD.Print("--------------------------------");
		}

		if (CurrentRoute.Count == 0){
			FoundSolution = false;
			Terminated = true;
			return;
		}
		var (current, neighbours) = CurrentRoute.Last();
		if (current.IsSolved()){
			FoundSolution = true;
			Terminated = true;
			return;
		}
		if (neighbours.Count == 0){
			CurrentRoute.RemoveAt(CurrentRoute.Count() - 1);
			return;
		}
		// choose the best neighbour to continue the path
		var nextStep = CurrentRoute.Last().Item2.First();

		// remove it from the choices list, so if we come back here, we won't check this one again.
		CurrentRoute.Last().Item2.RemoveAt(0);

		// add the state to the route, and discover it's neighbours
		AddAndExtend(nextStep);
	}

	public IEnumerable<RHGameState> GetSolutionPath() { 
		if (!FoundSolution) {
			return null;
		} 
		return CurrentRoute.Select(tuple => tuple.Item1);
	}
}

public class GraphSolver  {
	// public bool IsRunning { get; private set; }
	public bool FoundSolution { get; private set; }
	public bool Terminated { get; private set; }
	// public HashSet<GraphNode> WorkingSetNodes { get; } = new HashSet<GraphNode>();
	// public HashSet<GraphEdge> WorkingSetEdges { get; } = new HashSet<GraphEdge>();
	// public TimeSpan StepDelay { get; set; }

	public PriorityQueue<RHGameState, int> OpenStates { get; } = new ();
	// public RHGameState Current { get; set; }
	Heuristic Heuristic { get; set; }

	Dictionary<RHGameState, List<RHGameState>> RoutesGraph = new();

	// public RHGameState? Current => CurrentRoute.Last()?.Item1;


	public GraphSolver(Heuristic heuristic, RHGameState initialState){
		Heuristic = heuristic;
		// Current = initialState;
		FoundSolution = false;
		Terminated = false;

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
 
