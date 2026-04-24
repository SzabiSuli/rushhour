namespace rushhour.src.Nodes.Nodes3D;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.UI;

public partial class Vertex : RigidBody3D
{
    public const int repulsionForce = 1000;
    public const int influenceRadius = 1000;
    public const float defaultSpriteScale = 0.5f;
    public const float defaultCollisionScale = 1;
    public const float epsilon = 0.00001f; // used for float value comparison
    public readonly Vector3 maxVelocity = Vector3.One * 100;

    public readonly Vector3 negMaxVelocity = Vector3.One * (-100);
    public RHGameState GameState { get; set; } = null!;
    public const String scenePath = "res://scenes/graph/vertex.tscn";
    public static PackedScene Creator {get;} = ResourceLoader.Load<PackedScene>(scenePath);
    public static Dictionary<RHGameState, Vertex> Dict { get; } = new();

    private float _scaleTarget = 1;
    public float ScaleTarget {
        get => _scaleTarget;
        set {
            if (Math.Abs(_scaleTarget - value) < epsilon) return;
            _scaleTarget = value;
            
            Sprite.Scale = Vector3.One * defaultSpriteScale * _scaleTarget;
            CollisionShape.Scale = Vector3.One * defaultCollisionScale * _scaleTarget;
        }
    }

    public Sprite3D Sprite => GetChild<Sprite3D>(1);
    public CollisionShape3D CollisionShape => GetChild<CollisionShape3D>(0);

    // TODO maybe use the MainGameBoard algoCurrent
    private static Vertex? _algoCurrent = null;
    public static Vertex? AlgoCurrent {
        get => _algoCurrent;
        private set {
            if (_algoCurrent == value) return;
            Vertex? prevCurrent = _algoCurrent;

            _algoCurrent = value;

            _algoCurrent?.OnThisAlgoCurrentChanged();
            prevCurrent?.OnThisAlgoCurrentChanged();
        }
    }
    public bool IsAlgoCurrent => this == AlgoCurrent;
    private int _connectedAlgoEdges = 0;
    private int ConnectedAlgoEdges {
        get => _connectedAlgoEdges;
        set {
            if (value < 0) {
                throw new ArgumentException("ConnectedAlgoEdges can't be negative!");
            }

            _connectedAlgoEdges = value;
            SetEffect(VertexEffect.OnAlgoPath, value > 0);
        } 
    }

    private HashSet<VertexEffect> _effects = new();

    public void AddEffect(VertexEffect e) {
        _effects.Add(e);
        UpdateEffect();
    }
    public void RemoveEffect(VertexEffect e) {
        _effects.Remove(e);
        UpdateEffect();
    }
    public void SetEffect(VertexEffect e, bool active) {
        if (active) {
            AddEffect(e);
        } else {
            RemoveEffect(e);
        }
    }

    public void ClearEffects() {
        _effects.RemoveWhere(ve => ve == VertexEffect.OnAlgoPath);
        ConnectedAlgoEdges = 0;
        UpdateEffect();
    }

    public VertexEffect? Effect {
        get {
            if (!_effects.Any()) {
                return null;
            }
            return _effects.Min();
        }
    }


    private Vector3 _pendingForces = Vector3.Zero;
    private object _forcesLock = new object();
    public const float ignoreForceTresholdBase = 0.01f;
    public double IgnoreForceTreshold => ignoreForceTresholdBase * _timeAwake * Dict.Count;
    public static EventHandler<RHGameState>? VertexClicked;

    private double _timeAwake = 0;

    public static Vertex GetOrCreate(RHGameState state, Vertex? parent) {
        if (Dict.TryGetValue(state, out Vertex? vertex)) {
            // GD.Print("Vertex already exists");
            return vertex;
        }

        // GD.Print("Creating vertex");

        vertex = Creator.Instantiate<Vertex>();
        vertex.Init(state);

        if (parent == null) {
            vertex.Position = Vector3.Zero;
        } else {
            var outwardUnit = parent.Position.Normalized();

            // Place the vertex outwards
            // TODO tweak this
            Vector3 randUnitVector = new Vector3(
                GD.Randf() - 0.5f,
                GD.Randf() - 0.5f,
                GD.Randf() - 0.5f
            ).Normalized();

            if (randUnitVector.Dot(outwardUnit) < 0) {
                randUnitVector = -randUnitVector;
            }
            vertex.Position = parent.Position + randUnitVector * Edge.springLength;
        }

        // TODO add label with state info
        // vertex.GetNode<Label>("Label").Text = state.ToString();
        GraphScene.Instance.AddChild(vertex);
        Dict[state] = vertex;
        return vertex;
    }

    public static void OnNewCurrent(object? sender, RHGameState newCurrent) {
        Dict.TryGetValue(newCurrent, out Vertex? v);
        AlgoCurrent = v;
    } 
    public void Init(RHGameState gameState) {
        GameState = gameState;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        this.InputEvent += OnInputEvent;

        if (OS.IsDebugBuild()) {
            SleepingStateChanged += OnSleepingStateChanged;
        }

        // Initialise with the solved effect if it is solved.
        SetEffect(VertexEffect.Solved, GameState.IsSolved());
        SetEffect(VertexEffect.Transparent, HideButton.Instance.ButtonPressed);
    }

    public override void _ExitTree() {
        if (IsAlgoCurrent) {
            // Don't need to call the property
            _algoCurrent = null;
        } 
        base._ExitTree();
    }

    private void OnInputEvent(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx) {
        // Check if the event is a mouse button click
        if (@event is InputEventMouseButton mouseEvent) {
            // Specifically look for the Left Mouse Button being pressed
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed) {
                // GD.Print("Object clicked at: " + eventPosition);
                HandleClick();
            }
        }
    }

    private void HandleClick() => VertexClicked?.Invoke(this, GameState);

    // Put direct manipulation of Velocity here,
    // as this executes in sync with applying momentum.
    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        // Clamp velocity to prevent instability
        Vector3 v = state.LinearVelocity;
        Vector3 clamped = v.Clamp(negMaxVelocity, maxVelocity);
        
        if (v != clamped) {
            state.LinearVelocity = clamped; 
        }

        base._IntegrateForces(state);
    }

    public void ApplyPendingForce(Vector3 force) {
        lock (_forcesLock) {
            _pendingForces += force;
        }
    }

    public void EvalPendingForces() {

        // GD.Print($"{this} force treshold:", IgnoreForceTreshold);

        if (_pendingForces.LengthSquared() > Math.Pow(IgnoreForceTreshold, 1) ) {
            ApplyCentralForce(_pendingForces);
            // GD.Print($"{this} Applying force: ", _pendingForces);
        } else {
            // GD.Print($"{this} Skipping force application: ", _pendingForces);
        }
        // reset the pending forces in either case after an update
        _pendingForces = Vector3.Zero;
    }

    public static void OnPathChange(object? _, PathChangeArgs args) {
        int diff = args.onPath ? 1 : -1;
        
        Dict[args.move.To].ConnectedAlgoEdges += diff;
        Dict[args.move.From].ConnectedAlgoEdges += diff;
    } 

    public static void OnHideButtonToggled(bool on) {
        foreach (Vertex v in Dict.Values) {
            v.SetEffect(VertexEffect.Transparent, on);
        }
    }


    // Physics Priority = 1
    // Meaning this gets called after graphscene _PhysicsProcess
    public override void _PhysicsProcess(double delta) {
        _timeAwake += delta;
        
        // Barnes-Hut approximation via OctTree (O(n log n) instead of O(n^2))
        var tree = OctTree.GetCurrent();
        if (tree != null) {
            var force = OctTree.ComputeForce(tree, this, OctTree.Theta);
            ApplyPendingForce(force);
        }
    }

    public void OnThisAlgoCurrentChanged() => SetEffect(VertexEffect.AlgoCurrent, IsAlgoCurrent);
    public void OnSleepingStateChanged() => SetEffect(VertexEffect.Sleeping, Sleeping);

    public void UpdateEffect() {
        switch (Effect) {
            case VertexEffect.ManualCurrent:
                Sprite.Modulate = Colors.Red;
                ScaleTarget = 2f;
                break;
            case VertexEffect.Solved:
                Sprite.Modulate = Colors.Green;
                ScaleTarget = 2f;
                break;
            case VertexEffect.Initial:
                Sprite.Modulate = Colors.RoyalBlue;
                ScaleTarget = 2f;
                break;
            case VertexEffect.AlgoCurrent:
                Sprite.Modulate = Colors.Orange;
                ScaleTarget = 2f;
                break;
            case VertexEffect.OnAlgoPath:
                Sprite.Modulate = new Color(1, 1, 0, 0.5f);
                ScaleTarget = 1.5f;
                break;
            case VertexEffect.Transparent:
                Sprite.Modulate = new Color(1, 1, 1, 0.01f);
                CollisionShape.Disabled = true;
                ScaleTarget = 1f;
                return; // return so collision shape stays disabled
            case VertexEffect.Sleeping:
                Sprite.Modulate = new Color(1, 0, 1, 0.5f);
                ScaleTarget = 1f;
                break;
            case null:
                Sprite.Modulate = new Color(1, 1, 1, 0.5f);
                ScaleTarget = 1f;
                break;  
        }
        CollisionShape.Disabled = false;
    }
}


public enum VertexEffect {
    // Listed from hightest priority to lowest
    ManualCurrent = 0,
    Solved = 1,
    Initial = 2,
    AlgoCurrent = 3,
    OnAlgoPath = 4,
    Transparent = 5,
    Sleeping = 6,
}
