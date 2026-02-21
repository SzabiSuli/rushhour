namespace rushhour.src;

using System.Collections.Generic;
using Godot;

public interface ISolver
{
    bool IsRunning { get; }
    bool FoundSolution { get; }
    
    HashSet<GraphNode> WorkingSetNodes { get; }
    HashSet<GraphEdge> WorkingSetEdges { get; }

    void Step();
    List<GameState> GetSolutionPath();

    // TimeSpan StepDelay { get; set; }
}

public class BfsSolver : ISolver
{
    public bool IsRunning { get; private set; }
    public bool FoundSolution { get; private set; }
    public HashSet<GraphNode> WorkingSetNodes { get; } = new HashSet<GraphNode>();
    public HashSet<GraphEdge> WorkingSetEdges { get; } = new HashSet<GraphEdge>();
    // public TimeSpan StepDelay { get; set; }

    public void Step() 
    { 
        // Implementation hidden
    }
    
    public List<GameState> GetSolutionPath() 
    { 
        // Implementation hidden
        return null; 
    }
}

public class BacktrackingSolver : ISolver
{
    public bool IsRunning { get; private set; }
    public bool FoundSolution { get; private set; }
    public HashSet<GraphNode> WorkingSetNodes { get; } = new HashSet<GraphNode>();
    public HashSet<GraphEdge> WorkingSetEdges { get; } = new HashSet<GraphEdge>();
    // public TimeSpan StepDelay { get; set; }

    public void Step() 
    { 
        // Implementation hidden
    }

    public List<GameState> GetSolutionPath() 
    { 
        // Implementation hidden
        return null; 
    }
}

public class LocalSearchSolver : ISolver
{
    public bool IsRunning { get; private set; }
    public bool FoundSolution { get; private set; }
    public HashSet<GraphNode> WorkingSetNodes { get; } = new HashSet<GraphNode>();
    public HashSet<GraphEdge> WorkingSetEdges { get; } = new HashSet<GraphEdge>();
    // public TimeSpan StepDelay { get; set; }

    public void Step() 
    { 
        // Implementation hidden
    }

    public List<GameState> GetSolutionPath() 
    { 
        // Implementation hidden
        return null; 
    }
}
