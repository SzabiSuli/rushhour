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
    public static RHGameState? current;

    public static EventHandler<RHGameState>? VertexClicked; 



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
        MainScene.Instance.graphScene.AddChild(vertex);
        vertex.AddToGroup("Vertices");
        Dict[state] = vertex;
        return vertex;
    }

    public static void OnNewCurrent(object? sender, RHGameState newCurrent) {
        if (current == newCurrent) return;
        if (current is not null) {
            Dict[current].UpdateColor(false);
        }

        Dict[newCurrent].UpdateColor(true);
        current = newCurrent;
    }

    public void Init(RHGameState gameState) {
        GameState = gameState;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        this.InputEvent += OnInputEvent;
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

    private void HandleClick() {
        VertexClicked?.Invoke(this, GameState);
        GameState.PrintState();
    }

    // Put direct manipulation of Velocity here,
    // as this executes in sync with applying momentum.
    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        // Clamp velocity to prevent instability
        state.LinearVelocity = state.LinearVelocity.Clamp(negMaxVelocity, maxVelocity);

        base._IntegrateForces(state);
    }

    public override void _PhysicsProcess(double delta) {
        // Barnes-Hut approximation via OctTree (O(n log n) instead of O(n²))
        var tree = OctTree.GetCurrent();
        if (tree != null) {
            var force = OctTree.ComputeForce(tree, this, OctTree.Theta);
            ApplyCentralForce(force);
        }
    }

    public void UpdateColor(bool isCurrent) {
        var sprite = GetChild<Sprite3D>(1);
        if (isCurrent) {
            sprite.Modulate = Colors.Green;
        } else {
            sprite.Modulate = Colors.White;
        }
    }
}
