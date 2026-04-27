namespace rushhour.src.Nodes.Nodes3D;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.UI;


public class Edge {
    // Physics constants
    public const int springLength = 1;
    public const double optimalIntervalLowerBound = springLength * 0.9;
    public const double optimalIntervalUpperBound = springLength * 1.1;
    public const float springForce = 100;

    // Instance data
    public Vertex From { get; set; }
    public Vertex To { get; set; }
    public StateMove MoveUsed { get; set; }

    // Static registry
    public static Dictionary<StateMove, Edge> Dict { get; } = new();

    // Priority effects as enum ints in set
    private HashSet<EdgeEffect> _effects = new();
    public void AddEffect(EdgeEffect e) => _effects.Add(e);
    public void RemoveEffect(EdgeEffect e) => _effects.Remove(e);
    public void SetEffect(EdgeEffect e, bool active) {
        if (active) AddEffect(e); else RemoveEffect(e);
    }
    public void ClearEffects() => _effects.Clear();
    public EdgeEffect? Effect => _effects.Any() ? _effects.Min() : null;
    public bool Hidden => HideButton.Instance.ButtonPressed && Effect == null;


    public Color GetColor() => Effect switch {
        EdgeEffect.SolutionEdge => new Color(0, 1, 0, 1f),
        EdgeEffect.AlgoEdge     => new Color(1, 1, 0, 0.5f),
        _                       => new Color(1, 1, 1, 0.1f),
    };

    // Construction

    public Edge(Vertex from, Vertex to, StateMove moveUsed) {
        From = from;
        To = to;
        MoveUsed = moveUsed;
    }

    public static Edge GetOrCreate(StateMove move) {
        if (Dict.TryGetValue(move, out Edge? edge)) {
            return edge;
        }

        edge = new Edge(
            Vertex.Dict[move.From], 
            Vertex.Dict[move.To], 
            move
        );
        Dict[move] = edge;
        return edge;
    }

    // Physics
    public void ApplySpringForce() {
        Vector3 distanceVector = To.Position - From.Position;
        var length = distanceVector.Length();
        if (optimalIntervalLowerBound < length && length < optimalIntervalUpperBound) {
            // Spring is close to the optimal length, skip for performance
            return;
        }
        Vector3 force = distanceVector * ((length - springLength) / length) * springForce;

        From.ApplyPendingForce(force);
        To.ApplyPendingForce(-force);
    }

    // Static event handlers for solver integration

    public static void OnNewEdge(object? sender, StateMove edge) {
        // assume the vertex where we moved from already exists, 
        // find it
        Vertex from = Vertex.Dict[edge.From];

        // if the vertex we want to visit 
        // is already created and connected with its neighbours, skip it.
        if (Vertex.Dict.TryGetValue(edge.To, out Vertex? to)) {
            return;
        }

        Vertex.GetOrCreate(edge.To, from);
        GetOrCreate(edge);

        // Connect remaining edges to existing neighbors
        IEnumerable<StateMove> stateMoves = edge.To.GetPossibleMoves().Select(
            move => new StateMove(edge.To, edge.To.WithMove(move), move)
        );

        foreach (StateMove stateMove in stateMoves) {
            if (Vertex.Dict.TryGetValue(stateMove.To, out Vertex? v)) {
                Edge.GetOrCreate(stateMove);
            }
        }
    }

    public static void OnPathChange(object? _, PathChangeArgs args) {
        Dict[args.move].SetEffect(EdgeEffect.AlgoEdge, args.onPath);
    } 
}

public enum EdgeEffect {
    // listed from highest priority to lowest
    SolutionEdge = 0,
    AlgoEdge = 1
}