namespace rushhour.src.Nodes.Nodes3D;

using System;
using System.Collections.Generic;
using Godot;
using rushhour.src.Model;

public partial class Vertex : RigidBody3D
{
    public const int repulsionForce = 1000;
    public const int influenceRadius = 1000;
    public readonly Vector3 maxVelocity = Vector3.One * 100;
    public readonly Vector3 negMaxVelocity = Vector3.One * (-100);
    public RHGameState GameState { get; set; } = null!;
    public const String scenePath = "res://scenes/vertex.tscn";
    public static PackedScene Creator {get;} = ResourceLoader.Load<PackedScene>(scenePath);
    public static Dictionary<RHGameState, Vertex> Dict { get; } = new();
    private static Vertex? _current = null;
    public static Vertex? Current {
        get => _current;
        set {
            if (_current == value) return;
            Vertex? prevCurrent = _current;

            _current = value;

            _current?.OnThisCurrentChanged();
            prevCurrent?.OnThisCurrentChanged();   
        }
    }
    public bool IsCurrent => this == Current;

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
        vertex.AddToGroup("Vertices");
        Dict[state] = vertex;
        return vertex;
    }

    public static void OnNewCurrent(object? sender, RHGameState newCurrent) {
        Dict.TryGetValue(newCurrent, out Vertex? v);
        Current = v;
    } 
    public void Init(RHGameState gameState) {
        GameState = gameState;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        this.InputEvent += OnInputEvent;

        SleepingStateChanged += UpdateColor;
    }

    public override void _ExitTree() {
        if (IsCurrent) {
            // Don't need to call the property
            _current = null;
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

    public void OnThisCurrentChanged() => UpdateColor();
    public void UpdateColor() {
        var sprite = GetChild<Sprite3D>(1);
        Color c = IsCurrent ? Colors.Green : Sleeping ? Colors.Red : Colors.White;
        sprite.Modulate = c;
    }
}
