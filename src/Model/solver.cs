namespace rushhour.src;


using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public interface ISolver
{
    // bool IsRunning { get; }
    bool FoundSolution { get; set;}
    bool Terminated { get; }
    
    // HashSet<GraphNode> WorkingSetNodes { get; }
    // HashSet<GraphEdge> WorkingSetEdges { get; }

    void Step();
    List<GameState> GetSolutionPath();


    // TimeSpan StepDelay { get; set; }
}

public class HillClimberSolver : ISolver
{
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

    public BacktrackingSolver(Heuristic heuristic, RHGameState initialState){
        Heuristic = heuristic;
        var neighbours = initialState.GetPossibleMoves()
        .Select(initialState.WithMove)
        .Where(state => !CurrentRoute.Select(tuple => tuple.Item1).Contains(state)) // filter out states already in the current route
        .OrderBy(Heuristic.Evaluate)
        .ToList();
        CurrentRoute.Add(new (initialState, neighbours));

        // Current = initialState;
        FoundSolution = false;
        Terminated = false;
    }

    public void Step() { 
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



    }

    public List<GameState> GetSolutionPath() { 
        // Implementation hidden
        return null; 
    }
}

// public class LocalSearchSolver : ISolver {
//     public bool IsRunning { get; private set; }
//     public bool FoundSolution { get; private set; }
//     public HashSet<GraphNode> WorkingSetNodes { get; } = new HashSet<GraphNode>();
//     public HashSet<GraphEdge> WorkingSetEdges { get; } = new HashSet<GraphEdge>();
//     // public TimeSpan StepDelay { get; set; }

//     public void Step() { 
//         // Implementation hidden
//     }

//     public List<GameState> GetSolutionPath() { 
//         // Implementation hidden
//         return null; 
//     }
// }
