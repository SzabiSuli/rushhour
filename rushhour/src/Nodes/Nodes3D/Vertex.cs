namespace rushhour.src.Nodes.Nodes3D;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.UI;

public class Vertex {
    // Physics constants
    public const int repulsionForce = 1000;
    public const int influenceRadius = 1000;
    public const float linearDamp = 10.0f;
    public const float maxSpeed = 100f;
    public const float spawnDistanceFactor = 3;

    // Instance data
    public RHGameState GameState { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; private set; } = Vector3.Zero;

    // Thread safe force accumulation
    private Vector3 _pendingForces = Vector3.Zero;
    private readonly object _forcesLock = new();


    // Static registry
    public static Dictionary<RHGameState, Vertex> Dict { get; } = new();

    // Events
    public static event EventHandler<RHGameState>? VertexClicked;
    public static void FireVertexClicked(object sender, RHGameState state) =>
        VertexClicked?.Invoke(sender, state);


    private static Vertex? _algoCurrent = null;
    public static Vertex? AlgoCurrent {
        get => _algoCurrent;
        private set {
            if (_algoCurrent == value) return;
            Vertex? prevCurrent = _algoCurrent;
            _algoCurrent = value;

            _algoCurrent?.SetEffect(VertexEffect.AlgoCurrent, true);
            prevCurrent?.SetEffect(VertexEffect.AlgoCurrent, false);
        }
    }
    public bool IsAlgoCurrent => this == AlgoCurrent;

    private int _connectedAlgoEdges = 0;
    public int ConnectedAlgoEdges {
        get => _connectedAlgoEdges;
        set {
            if (value < 0) throw new ArgumentException("ConnectedAlgoEdges can't be negative!");
            _connectedAlgoEdges = value;
            SetEffect(VertexEffect.OnAlgoPath, value > 0);
        } 
    }

    // Priority effect handling with int enum in set

    private HashSet<VertexEffect> _effects = new();
    public void AddEffect(VertexEffect e) => _effects.Add(e);
    public void RemoveEffect(VertexEffect e) => _effects.Remove(e);
    public void SetEffect(VertexEffect e, bool active) {
        if (active) AddEffect(e); else RemoveEffect(e);
    }
    public void ClearEffects() {
        _effects.RemoveWhere(ve => ve == VertexEffect.OnAlgoPath);
        ConnectedAlgoEdges = 0;
    }
    public VertexEffect? Effect => _effects.Any() ? _effects.Min() : null;
    public bool Hidden => HideButton.Instance.ButtonPressed && Effect == null;

    public Color GetColor() => Effect switch {
        VertexEffect.ManualCurrent => Colors.Red,
        VertexEffect.Solved        => Colors.Green,
        VertexEffect.Initial       => Colors.RoyalBlue,
        VertexEffect.AlgoCurrent   => Colors.Orange,
        VertexEffect.OnAlgoPath    => new Color(1, 1, 0, 0.5f),
        _                          => new Color(1, 1, 1, 0.5f),
    };

    public float GetScale() => Effect switch {
        VertexEffect.ManualCurrent => 2f,
        VertexEffect.Solved        => 2f,
        VertexEffect.Initial       => 2f,
        VertexEffect.AlgoCurrent   => 2f,
        VertexEffect.OnAlgoPath    => 1.5f,
        _                          => 1f,
    };

    // Construction
    public Vertex(RHGameState gameState) {
        GameState = gameState;
        // Initialize with solved effect if applicable
        SetEffect(VertexEffect.Solved, gameState.IsSolved());
    }

    public static Vertex GetOrCreate(RHGameState state, Vertex? parent) {
        if (Dict.TryGetValue(state, out Vertex? vertex)) {
            return vertex;
        }

        vertex = new Vertex(state);

        if (parent == null) {
            vertex.Position = Vector3.Zero;
        } else {
            var outwardUnit = parent.Position.Normalized();

            Vector3 randUnitVector = new Vector3(
                GD.Randf() - 0.5f,
                GD.Randf() - 0.5f,
                GD.Randf() - 0.5f
            ).Normalized();

            if (randUnitVector.Dot(outwardUnit) < 0) {
                randUnitVector = -randUnitVector;
            }
            vertex.Position = parent.Position + randUnitVector * Edge.springLength * spawnDistanceFactor;
        }

        Dict[state] = vertex;
        return vertex;
    }

    // Force accumulation (thread-safe)
    public void ApplyPendingForce(Vector3 force) {
        lock (_forcesLock) {
            _pendingForces += force;
        }
    }

    // Apply movement
    public void Integrate(double delta) {
        // Assume a mass of 1 unit
        Velocity += _pendingForces * (float)delta;
        _pendingForces = Vector3.Zero;

        // Apply linear damping
        Velocity *= (float)Math.Max(0, 1.0 - linearDamp * delta);

        // Clamp velocity
        Vector3 maxVel = Vector3.One * maxSpeed;
        Velocity = Velocity.Clamp(-maxVel, maxVel);

        // Integrate position
        Position += Velocity * (float)delta;
    }

    // Static event handlers for solver integration

    public static void OnNewCurrent(object? sender, RHGameState newCurrent) {
        Dict.TryGetValue(newCurrent, out Vertex? v);
        AlgoCurrent = v;
    } 

    public static void OnPathChange(object? _, PathChangeArgs args) {
        int diff = args.onPath ? 1 : -1;
        Dict[args.move.To].ConnectedAlgoEdges += diff;
        Dict[args.move.From].ConnectedAlgoEdges += diff;
    } 
}


public enum VertexEffect {
    // Listed from highest priority to lowest
    ManualCurrent = 0,
    Solved = 1,
    Initial = 2,
    AlgoCurrent = 3,
    OnAlgoPath = 4
}
