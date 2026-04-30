namespace rushhour.test;

using rushhour.src.Model;
using rushhour.src.Nodes.Nodes3D;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;
using System;
using System.Linq;


[TestSuite]
public class ViewTests {
    // Re-use the same small test level as ModelTests for state creation.
    private static readonly string _testLevelString = """
    Test Level
    .....b
    .....b
    aA.C.B
    ...c..
    .Ddd..
    ......
    """;

    private static readonly string _solvedLevelString = """
    Test Level
    ...C..
    ...c..
    ....aA
    .....b
    .Ddd.b
    .....B
    """;

    private RHGameState TestState   => Levels.LoadLevelString(_testLevelString).State;
    private RHGameState SolvedState => Levels.LoadLevelString(_solvedLevelString).State;

    // Create a Vertex without touching Vertex.Dict, and position it manually.
    private static Vertex MakeVertex(RHGameState state, Vector3 position) {
        return new Vertex(state) {
            Position = position
        };
    }

    [TestCase]
    public void V01_VertexEffectAddRemovePriority() {
        RHGameState state = TestState;
        var v = MakeVertex(state, Vector3.Zero);

        // A fresh, non-solved vertex should have no effect.
        AssertBool(v.Effect == null).IsTrue();

        // Adding a lower-priority effect.
        v.AddEffect(VertexEffect.OnAlgoPath);
        AssertThat(v.Effect).Equals(VertexEffect.OnAlgoPath);

        // Adding a higher-priority effect must make it dominate.
        v.AddEffect(VertexEffect.AlgoCurrent);
        AssertThat(v.Effect).Equals(VertexEffect.AlgoCurrent);

        // Removing the highest-priority effect restores the lower one.
        v.RemoveEffect(VertexEffect.AlgoCurrent);
        AssertThat(v.Effect).Equals(VertexEffect.OnAlgoPath);

        // Removing the last effect leaves no active effect.
        v.RemoveEffect(VertexEffect.OnAlgoPath);
        AssertBool(v.Effect == null).IsTrue();
    }

    [TestCase]
    public void V02_VertexColorByEffect() {
        RHGameState state = TestState;

        // Solved vertex: constructor auto-adds VertexEffect.Solved for solved states.
        var solvedVertex = MakeVertex(SolvedState, Vector3.Zero);
        AssertThat(solvedVertex.Effect).Equals(VertexEffect.Solved);
        AssertThat(solvedVertex.GetColor()).Equals(Colors.Green);

        var v = MakeVertex(state, Vector3.Zero);

        // No effect → white (with alpha)
        AssertBool(v.Effect == null).IsTrue();
        var white = v.GetColor();
        AssertFloat(white.R).Equals(1f);
        AssertFloat(white.G).Equals(1f);
        AssertFloat(white.B).Equals(1f);

        // ManualCurrent → Red
        v.AddEffect(VertexEffect.ManualCurrent);
        AssertThat(v.GetColor()).Equals(Colors.Red);
        v.RemoveEffect(VertexEffect.ManualCurrent);

        // Initial → RoyalBlue
        v.AddEffect(VertexEffect.Initial);
        AssertThat(v.GetColor()).Equals(Colors.RoyalBlue);
        v.RemoveEffect(VertexEffect.Initial);

        // AlgoCurrent → Orange
        v.AddEffect(VertexEffect.AlgoCurrent);
        AssertThat(v.GetColor()).Equals(Colors.Orange);
        v.RemoveEffect(VertexEffect.AlgoCurrent);

        // OnAlgoPath → yellowish semi-transparent
        v.AddEffect(VertexEffect.OnAlgoPath);
        Color algoPathColor = v.GetColor();
        AssertFloat(algoPathColor.R).Equals(1f);
        AssertFloat(algoPathColor.G).Equals(1f);
        AssertFloat(algoPathColor.B).Equals(0f);
        v.RemoveEffect(VertexEffect.OnAlgoPath);
    }

    [TestCase]
    public void V03_VertexScaleByEffect() {
        var v = MakeVertex(TestState, Vector3.Zero);

        // Default (no effect) → 1f
        AssertFloat(v.GetScale()).Equals(1f);

        // Each special effect must return the documented scale.
        v.AddEffect(VertexEffect.ManualCurrent);
        AssertFloat(v.GetScale()).Equals(2f);
        v.RemoveEffect(VertexEffect.ManualCurrent);

        v.AddEffect(VertexEffect.Solved);
        AssertFloat(v.GetScale()).Equals(2f);
        v.RemoveEffect(VertexEffect.Solved);

        v.AddEffect(VertexEffect.Initial);
        AssertFloat(v.GetScale()).Equals(2f);
        v.RemoveEffect(VertexEffect.Initial);

        v.AddEffect(VertexEffect.AlgoCurrent);
        AssertFloat(v.GetScale()).Equals(2f);
        v.RemoveEffect(VertexEffect.AlgoCurrent);

        v.AddEffect(VertexEffect.OnAlgoPath);
        AssertFloat(v.GetScale()).Equals(1.5f);
        v.RemoveEffect(VertexEffect.OnAlgoPath);

        // Back to default after clearing all effects
        AssertFloat(v.GetScale()).Equals(1f);
    }

    [TestCase]
    public void V04_EdgeEffectsAndColor() {
        RHGameState stateA = TestState;
        Move move = stateA.GetPossibleMoves().First();
        RHGameState stateB = stateA.WithMove(move);
        StateMove stateMove = new StateMove(stateA, stateB, move);

        Vertex vA = MakeVertex(stateA, Vector3.Zero);
        Vertex vB = MakeVertex(stateB, new Vector3(1, 0, 0));
        var edge = new Edge(vA, vB, stateMove);

        // No effect → white with low alpha
        AssertBool(edge.Effect == null).IsTrue();
        Color noEffect = edge.GetColor();
        AssertFloat(noEffect.R).Equals(1f);
        AssertFloat(noEffect.G).Equals(1f);
        AssertFloat(noEffect.B).Equals(1f);

        // AlgoEdge → yellow semi-transparent
        edge.AddEffect(EdgeEffect.AlgoEdge);
        AssertThat(edge.Effect).Equals(EdgeEffect.AlgoEdge);
        Color algoColor = edge.GetColor();
        AssertFloat(algoColor.R).Equals(1f);
        AssertFloat(algoColor.G).Equals(1f);
        AssertFloat(algoColor.B).Equals(0f);

        // SolutionEdge has higher priority (lower enum int value).
        edge.AddEffect(EdgeEffect.SolutionEdge);
        AssertThat(edge.Effect).Equals(EdgeEffect.SolutionEdge);
        Color solutionColor = edge.GetColor();
        AssertFloat(solutionColor.R).Equals(0f);
        AssertFloat(solutionColor.G).Equals(1f);
        AssertFloat(solutionColor.B).Equals(0f);

        // Remove SolutionEdge → AlgoEdge should re-dominate.
        edge.RemoveEffect(EdgeEffect.SolutionEdge);
        AssertThat(edge.Effect).Equals(EdgeEffect.AlgoEdge);

        // ClearEffects → no effect
        edge.ClearEffects();
        AssertBool(edge.Effect == null).IsTrue();
    }

    [TestCase]
    public void V05_EdgeSpringForceDirection() {
        RHGameState stateA = TestState;
        Move move = stateA.GetPossibleMoves().First();
        RHGameState stateB = stateA.WithMove(move);
        StateMove stateMove = new StateMove(stateA, stateB, move);

        // Place vertices so their distance is exactly 2 × springLength (too far apart).
        // The spring should pull them together: From gets a force in the positive X
        // direction and To gets a force in the negative X direction.
        float dist = Edge.springLength * 2f;
        Vertex vFrom = MakeVertex(stateA, Vector3.Zero);
        Vertex vTo   = MakeVertex(stateB, new Vector3(dist, 0, 0));

        var edge = new Edge(vFrom, vTo, stateMove);

        // Accumulate forces.
        edge.ApplySpringForce();

        // Integrate with a small delta to observe velocity change.
        double delta = 0.01;
        vFrom.Integrate(delta);
        vTo.Integrate(delta);

        // vFrom should have moved towards vTo (positive X direction).
        AssertFloat(vFrom.Position.X).IsGreater(0f);

        // vTo should have moved towards vFrom (negative X direction).
        AssertFloat(vTo.Position.X).IsLess(dist);

        // Now test with vertices too close (distance < springLength).
        // The spring should push them apart.
        float closeDist = Edge.springLength * 0.5f;
        Vertex vFromClose = MakeVertex(stateA, Vector3.Zero);
        Vertex vToClose   = MakeVertex(stateB, new Vector3(closeDist, 0, 0));

        var edgeClose = new Edge(vFromClose, vToClose, stateMove);
        edgeClose.ApplySpringForce();

        vFromClose.Integrate(delta);
        vToClose.Integrate(delta);

        // vFromClose should have moved away from vToClose (negative X direction).
        AssertFloat(vFromClose.Position.X).IsLess(0f);

        // vToClose should have moved away from vFromClose (positive X direction).
        AssertFloat(vToClose.Position.X).IsGreater(closeDist);
    }

    [TestCase]
    public void V06_VertexPhysicsIntegration() {
        var v = MakeVertex(TestState, Vector3.Zero);

        // Initial velocity should be zero.
        AssertThat(v.Velocity).Equals(Vector3.Zero);
        AssertThat(v.Position).Equals(Vector3.Zero);

        // Apply a force in the +X direction.
        v.ApplyPendingForce(new Vector3(100f, 0f, 0f));
        v.Integrate(0.01);

        // Velocity should now be positive in X.
        AssertFloat(v.Velocity.X).IsGreater(0f);
        // Position should have moved in the +X direction.
        AssertFloat(v.Position.X).IsGreater(0f);

        // After integration, pending forces are consumed; a second integration
        // with no new force should decelerate due to linear damping.
        float velocityAfterFirstStep = v.Velocity.X;
        v.Integrate(0.01);
        AssertFloat(v.Velocity.X).IsLess(velocityAfterFirstStep);

        // Applying a force in the opposite direction should further decelerate / reverse.
        v.ApplyPendingForce(new Vector3(-10000f, 0f, 0f));
        v.Integrate(0.01);
        AssertFloat(v.Velocity.X).IsLess(0f);
    }
}
