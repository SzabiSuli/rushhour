namespace rushhour.test;

using rushhour.src.Model;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;
using System.Linq;
using System.Collections.Generic;
using System;
using CommandLine;

[TestSuite]
public class ModelTests {

    // This is a solvable state
    string testLevelString = """
    Test Level
    .....b
    .....b
    aA.C.B
    ...c..
    .Ddd..
    ......
    """;

    [TestCase]
    public void U01_LevelParse() {
        // Try to load all levels
        var loadedLevels = Levels.LoadLevels().ToList();

        Level level = Levels.LoadLevelString(testLevelString);

        // Title
        AssertString(level.Title).Equals("Test Level");

        // Vehicle count
        AssertInt(level.State.PlacedPieces.Count).Equals(4);

        // Vehicle types (index 0 must be the MainCar)
        AssertThat(level.State.PlacedPieces[0].Piece.GetType()).Equals(typeof(MainCar));
        AssertThat(level.State.PlacedPieces[1].Piece.GetType()).Equals(typeof(Bus));
        AssertThat(level.State.PlacedPieces[2].Piece.GetType()).Equals(typeof(Car));
        AssertThat(level.State.PlacedPieces[3].Piece.GetType()).Equals(typeof(Bus));

        // Positions and facing directions
        AssertThat(level.State.PlacedPieces[0].Position).Equals(new Vector2I(1, 2));
        AssertThat(level.State.PlacedPieces[0].FacingDirection).Equals(Direction.Right);

        AssertThat(level.State.PlacedPieces[1].Position).Equals(new Vector2I(5, 2));
        AssertThat(level.State.PlacedPieces[1].FacingDirection).Equals(Direction.Down);

        AssertThat(level.State.PlacedPieces[2].Position).Equals(new Vector2I(3, 2));
        AssertThat(level.State.PlacedPieces[2].FacingDirection).Equals(Direction.Up);

        AssertThat(level.State.PlacedPieces[3].Position).Equals(new Vector2I(1, 4));
        AssertThat(level.State.PlacedPieces[3].FacingDirection).Equals(Direction.Left);
    }

    int[,] testBoardGrid = new int[,] {
        { -1, -1, -1, -1, -1,  1 },
        { -1, -1, -1, -1, -1,  1 },
        {  0,  0, -1,  2, -1,  1 },
        { -1, -1, -1,  2, -1, -1 },
        { -1,  3,  3,  3, -1, -1 },
        { -1, -1, -1, -1, -1, -1 }
    };

    [TestCase]
    public void U02_LevelGrid() {
        Level level = Levels.LoadLevelString(testLevelString);
        for (int i = 0; i < 6; i++) {
            for (int j = 0; j < 6; j++) {
                AssertInt(level.State[i, j]).Equals(testBoardGrid[i, j]);
            }
        }
    }

    [TestCase]
    public void U03_AllBuiltInLevelsLoad() {
        var levels = Levels.LoadLevels().ToList();

        // There must be at least one built-in level.
        AssertBool(levels.Count > 0).IsTrue();

        foreach (Level level in levels) {
            // Title must be non-empty.
            AssertBool(level.Title.Trim().Length > 0).IsTrue();

            // State must have at least the main car.
            AssertBool(level.State.PlacedPieces.Count > 0).IsTrue();
            AssertThat(level.State.PlacedPieces[0].Piece.GetType()).Equals(typeof(MainCar));

            // The initial state of a puzzle must not already be solved.
            AssertBool(level.State.IsSolved()).IsFalse();

            // The state must have at least one possible move (it can't be immediately stuck).
            AssertBool(level.State.GetPossibleMoves().Any()).IsTrue();
        }
    }

    [TestCase]
    public void U04_LevelMoves() {
        RHGameState state = Levels.LoadLevelString(testLevelString).State;

        var possibleMoves = state.GetPossibleMoves().ToList();
        var possibleStateMoves = state.GetPossibleStateMoves().ToList();

        // GetPossibleMoves and GetPossibleStateMoves must be consistent.
        AssertInt(possibleMoves.Count).Equals(possibleStateMoves.Count);

        for (int i = 0; i < possibleMoves.Count; i++) {
            Move move = possibleMoves[i];
            // Apply the move and verify the resulting state is constructible
            // (the constructor throws on overlaps / out-of-bounds).
            RHGameState next = state.WithMove(move);

            // The moved piece must stay within bounds.
            PlacedRHPiece movedPiece = next.PlacedPieces[move.PieceIndex];
            AssertInt(movedPiece.Position.X).IsGreaterEqual(0);
            AssertInt(movedPiece.Position.Y).IsGreaterEqual(0);
            AssertInt(movedPiece.Position.X).IsLess(state.BoardWidth);
            AssertInt(movedPiece.Position.Y).IsLess(state.BoardHeight);

            // Assert that the move's "To" state matches the one from GetPossibleStateMoves.
            AssertThat(possibleStateMoves[i].Move).Equals(move);
            AssertThat(possibleStateMoves[i].To).Equals(next);
        }

    }
    [TestCase]
    public void U05_StateImmutability() {
        RHGameState original = Levels.LoadLevelString(testLevelString).State;
        Move firstMove = original.GetPossibleMoves().First();

        Vector2I originalPos = original.PlacedPieces[firstMove.PieceIndex].Position;

        RHGameState next = original.WithMove(firstMove);

        // Assert that the original position stays unchanged
        AssertThat(original.PlacedPieces[firstMove.PieceIndex].Position).Equals(originalPos);

        // The new state must differ from the original.
        AssertBool(original == next).IsFalse();
    }

    [TestCase]
    public void U06_StateEquality() {
        RHGameState state1 = Levels.LoadLevelString(testLevelString).State;
        RHGameState state2 = Levels.LoadLevelString(testLevelString).State;

        // Same configuration is equal.
        AssertThat(state1).Equals(state2);
        AssertBool(state1 == state2).IsTrue();
        AssertBool(state1 != state2).IsFalse();
        AssertBool(state1.Equals(state2)).IsTrue();

        // After a move → not equal.
        Move move = state1.GetPossibleMoves().First();
        RHGameState moved1 = state1.WithMove(move);
        
        AssertThat(state1).IsNotEqual(moved1);
        AssertBool(state1 == moved1).IsFalse();
        AssertBool(state1 != moved1).IsTrue();
        AssertBool(state1.Equals(moved1)).IsFalse();

        // Make the same move on the second state
        RHGameState moved2 = state2.WithMove(move);

        // Assert those are equal aswell
        AssertThat(moved1).Equals(moved2);
        AssertBool(moved1 == moved2).IsTrue();
        AssertBool(moved1 != moved2).IsFalse();
        AssertBool(moved1.Equals(moved2)).IsTrue();

        // First move should be with the main car
        // Load the altered level from a string and compare those aswell

        RHGameState state3 = Levels.LoadLevelString("""
        Moved test state
        .....b
        .....b
        .aAC.B
        ...c..
        .Ddd..
        ......
        """).State;

        // Assert the newly loaded state is equal to the moved one
        AssertThat(moved1).Equals(state3);
        AssertBool(moved1 == state3).IsTrue();
        AssertBool(moved1 != state3).IsFalse();
        AssertBool(moved1.Equals(state3)).IsTrue();
    }

    [TestCase]
    public void U07_HashConsistency() {
        RHGameState state1 = Levels.LoadLevelString(testLevelString).State;
        RHGameState state2 = Levels.LoadLevelString(testLevelString).State;

        // Equal states should result in an identical hash.
        AssertInt(state1.GetHashCode()).Equals(state2.GetHashCode());

        // Different state should (almost certainly) result in a different hash.
        Move move = state1.GetPossibleMoves().First();
        RHGameState moved = state1.WithMove(move);
        AssertInt(state1.GetHashCode()).IsNotEqual(moved.GetHashCode());
    }


    string testSolvedLevelString = """
    Test Level
    ...C..
    ...c..
    ....aA
    .....b
    .Ddd.b
    .....B
    """;

    [TestCase]
    public void U08_SolvedDetection() {
        RHGameState unsolved = Levels.LoadLevelString(testLevelString).State;
        AssertBool(unsolved.IsSolved()).IsFalse();

        // Load a solved state
        RHGameState solved = Levels.LoadLevelString(testSolvedLevelString).State;
        AssertBool(solved.IsSolved()).IsTrue();

        // Assert exit is at the right place
        AssertThat(solved.ExitPosition).Equals(new Vector2I(5, 2));
    }

    [TestCase]
    public void U09_NullHeuristic() {
        NullHeuristic h = new NullHeuristic();
        RHGameState state = Levels.LoadLevelString(testLevelString).State;

        AssertInt(h.Evaluate(state)).Equals(0);

        // Check each neighbour aswell
        foreach (var sm in state.GetPossibleStateMoves()) {
            AssertInt(h.Evaluate(sm.To)).Equals(0);
        }
    }

    [TestCase]
    public void U10_DistanceHeuristicRange() {
        DistanceHeuristic h = new DistanceHeuristic();

        // Explore some states and test them
        RHGameState initial = Levels.LoadLevelString(testLevelString).State;
        var visited = new HashSet<RHGameState> { initial };
        var queue = new Queue<RHGameState>();
        queue.Enqueue(initial);

        while (queue.Count > 0) {
            RHGameState current = queue.Dequeue();
            int val = h.Evaluate(current);

            AssertBool(val >= 0).IsTrue();
            AssertBool(val <= 4).IsTrue();

            // Zero means solved.
            if (current.IsSolved()) {
                AssertInt(val).Equals(0);
            } else {
                AssertBool(val > 0).IsTrue();
            }

            foreach (var sm in current.GetPossibleStateMoves()) {
                if (visited.Add(sm.To)) {
                    queue.Enqueue(sm.To);
                }
            }
        }
    }
    // Test level:
    // .....b
    // .....b
    // aA.C.B
    // ...c..
    // .Ddd..
    // ......

    string testLevelString2 = """
    Test level
    ..F..b
    ..f.eb
    aAfCEB
    ...c..
    .Ddd..
    ......
    """;
    string testLevelString3 = """
    Test level
    F....b
    f...eb
    faACEB
    ...cg.
    .DddG.
    ......
    """;

    [TestCase]
    public void U11_FreeSpacesHeuristicKnownValue() {
        Heuristic<RHGameState> h = new FreeSpacesHeuristic();

        RHGameState state = Levels.LoadLevelString(testLevelString).State;
        // The initial state of test level 1 must have a heuristic score of:
        // 4 distance + 2 blocks = 6
        AssertInt(h.Evaluate(state)).Equals(6);

        RHGameState state2 = Levels.LoadLevelString(testLevelString2).State;
        // 4 distance + 4 blocks = 8
        AssertInt(h.Evaluate(state2)).Equals(8);

        RHGameState state3 = Levels.LoadLevelString(testLevelString3).State;
        // 3 distance + 3 blocks = 6
        // only the blocks in front of the main car should count
        AssertInt(h.Evaluate(state2)).Equals(6);


        // On the solved state h must return 0.
        RHGameState solved = Levels.LoadLevelString(testSolvedLevelString).State;
        AssertInt(h.Evaluate(solved)).Equals(0);
    }
    [TestCase]
    public void U12_MoverHeuristicKnownValue() {
        Heuristic<RHGameState> h = new MoverHeuristic();

        RHGameState state = Levels.LoadLevelString(testLevelString).State;
        // The initial state of test level 1 must have a heuristic score of:
        // 4 + Min(2, 1 + 1) + 3 = 9
        // 4 distance, 2 moves to put C out of the way, 3 moves to put B out of the way 
        AssertInt(h.Evaluate(state)).Equals(9);

        RHGameState state2 = Levels.LoadLevelString(testLevelString2).State;
        // 4 + (3 + 1) + Min(2, 1 + 1) + Min (1, 2) + 3 = 14
        AssertInt(h.Evaluate(state2)).Equals(14);

        RHGameState state3 = Levels.LoadLevelString(testLevelString3).State;
        // 3 + Min(2, 1 + 1) + Min(1, 2 + 2) + 3 = 9
        AssertInt(h.Evaluate(state2)).Equals(9);

        // On the solved state h must return 0.
        RHGameState solved = Levels.LoadLevelString(testSolvedLevelString).State;
        AssertInt(h.Evaluate(solved)).Equals(0);
    }

    [TestCase]
    public void U13_HeuristicAdmissibility() {
        // Start from a solved state
        RHGameState solved = Levels.LoadLevelString(testSolvedLevelString).State;
        AssertBool(solved.IsSolved()).IsTrue();

        Heuristic<RHGameState>[] heuristics = {
            new NullHeuristic(),
            new DistanceHeuristic(),
            new FreeSpacesHeuristic(),
            new MoverHeuristic()
        };

        // Start a check states breadth first from the solved state
        var stepsToSolveState = new Dictionary<RHGameState, int> { [solved] = 0 };
        var queue = new Queue<RHGameState>();
        queue.Enqueue(solved);

        while (queue.Count > 0) {
            var state = queue.Dequeue();
            int hStar = stepsToSolveState[state];

            // Assert that the heuristic score of the state is less or equal to the distance from being solved
            
            AssertBool(heuristics.All(h => h.Evaluate(state) <= hStar)).IsTrue();

            // if (state.IsSolved()) { distanceFromSolved = hStar; break; }
            foreach (var sm in state.GetPossibleStateMoves()) {
                if (!stepsToSolveState.ContainsKey(sm.To)) {
                    if (sm.To.IsSolved()) {
                        stepsToSolveState[sm.To] = 0;
                    } else {
                        stepsToSolveState[sm.To] = hStar + 1;
                    }
                    queue.Enqueue(sm.To);
                }
            }
        }
    }

    [TestCase]
    public void U14_HeuristicMonotonicity() {
        MonotoneHeuristic<RHGameState>[] heuristics = {
            new NullHeuristic(),
            new DistanceHeuristic(),
            new FreeSpacesHeuristic(),
            new MoverHeuristic()
        };

        RHGameState initial = Levels.LoadLevelString(testLevelString).State;
        var visited = new HashSet<RHGameState> { initial };
        var queue = new Queue<RHGameState>();
        queue.Enqueue(initial);

        while (queue.Count > 0) {
            RHGameState state = queue.Dequeue();

            foreach (var sm in state.GetPossibleStateMoves()) {
                foreach (var h in heuristics) {
                    int hs = h.Evaluate(state);
                    int hs2 = h.Evaluate(sm.To);

                    // Monotonicity: | h(s) - h(s') | <= 1
                    AssertInt(Math.Abs(hs - hs2)).IsLessEqual(1);
                }

                if (visited.Add(sm.To)) {
                    queue.Enqueue(sm.To);
                }
            }
        }
    }

    [TestCase]
    public void U15_TabuSolverTermination() {
        // The state
        // .....b
        // .....b
        // aA.C.B
        // ...c..
        // .Ddd..
        // ......
        //                       A   B   C   D
        // has an upper bound of 5 * 4 * 5 * 4 = 400 states
        // the main car A can be in 5 places, B can be in 4, C in 5, D in 4 not subtracting overlaps
        // With a tabu size of 400 the algorithm is guaranteed to terminate

        RHGameState initial = Levels.LoadLevelString(testLevelString).State;
        var solver = new TabuSolver(new DistanceHeuristic(), tabuSize: 400);

        bool terminated = false;
        solver.Terminated += (_, _) => terminated = true;

        solver.Start(initial);
        int stepCountCheck = 0;

        // Also assert the stepcounts match
        AssertInt(solver.StepCount).Equals(stepCountCheck);

        int maxSteps = 410;
        while (!terminated && solver.StepCount <= maxSteps) {
            solver.Step();
            stepCountCheck++;
            AssertInt(solver.StepCount).Equals(stepCountCheck);
        }

        // The solver must have reached a terminal status.
        AssertBool(
            solver.Status == SolverStatus.Solved ||
            solver.Status == SolverStatus.Terminated ||
            solver.Status == SolverStatus.NoSolution
        ).IsTrue();

        // Assert the Terminated event has been called
        AssertBool(terminated).IsTrue();
    }

    [TestCase]
    public void U16_BacktrackingCompleteness() {
        RHGameState solvable = Levels.LoadLevelString(testLevelString).State;
        var solver = new BacktrackingSolver(new MoverHeuristic());
        solver.Start(solvable);

        // Assume that with the mover heuristic the solution can be found within 100_000 steps
        int maxSteps = 100_000;
        while (solver.Status == SolverStatus.Running && solver.StepCount <= maxSteps) {
            solver.Step();
        }

        AssertThat(solver.Status).Equals(SolverStatus.Solved);

        // A valid solution path should be non-empty.
        var path = solver.GetSolutionPath().ToList();
        AssertBool(path.Count > 0).IsTrue();

        // The last state in the path should be the solved state.
        AssertBool(path.Last().To.IsSolved()).IsTrue();
    }

    [TestCase]
    public void U17_AcGraphOptimality() {
        RHGameState initial = Levels.LoadLevelString(testLevelString).State;


        var stepsFromStart = new Dictionary<RHGameState, int> { [initial] = 0 };
        var queue = new Queue<RHGameState>();
        queue.Enqueue(initial);
        RHGameState? solved = null;

        while (queue.Count > 0 && solved == null) {
            var state = queue .Dequeue();
            int distance = stepsFromStart[state];

            foreach (var sm in state.GetPossibleStateMoves()) {
                if (!stepsFromStart.ContainsKey(sm.To)) {
                    stepsFromStart[sm.To] = distance + 1;

                    if (sm.To.IsSolved()) {
                        solved = sm.To;
                        break;
                    }
                    queue.Enqueue(sm.To);
                }
            }
        }

        // Assert we found a solution
        AssertThat(solved).IsNotNull();


        int optimalCost = stepsFromStart[solved.Cast<RHGameState>()];


        // Now run the A*-graph solver with a monotone heuristic.
        var solver = new AcGraphSolver(new DistanceHeuristic());
        solver.Start(initial);

        int maxSteps = 410;
        while (solver.Status == SolverStatus.Running && solver.StepCount <= maxSteps) {
            solver.Step();
        }

        AssertThat(solver.Status).Equals(SolverStatus.Solved);

        var solutionPath = solver.GetSolutionPath().ToList();
        AssertInt(solutionPath.Count).Equals(optimalCost);

        // assert that the route is correctly constructed
        AssertThat(solutionPath.First().From).Equals(initial);
        AssertBool(solutionPath.Last().To.IsSolved()).IsTrue();

        // the route is correctly connected
        for (int i = 0; i < optimalCost - 1; i++) {
            AssertThat(solutionPath[i].To).Equals(solutionPath[i+1].From);
        }
    }

    // An unsolvable state: the B bus cannot be put on the bottom to clear the way
    string testUnsolvableLevelString = """
    Unsolvable
    .....B
    .....b
    aA...b
    ......
    ......
    ddDccC
    """;


    [TestCase]
    public void U18_BacktrackingNoSolution() {
        RHGameState unsolvable = Levels.LoadLevelString(testUnsolvableLevelString).State;

        // state is indeed not solved.
        AssertBool(unsolvable.IsSolved()).IsFalse();

        var solver = new BacktrackingSolver(new NullHeuristic());
        solver.Start(unsolvable);

        // with V = 4 * 3 = 12 states and b = 4 max moves for each state,
        // a maximum of b^V = 4^12 routes will be inspected, each with a maximum step count of V
        // So the algorithm must temrinate in b^V * V = 4^12 * 12 = 201326592 steps
        int maxSteps = 201_326_592; 
        while (solver.Status == SolverStatus.Running && solver.StepCount <= maxSteps) {
            solver.Step();
        }

        // Backtracking is complete: it must report NoSolution for an unsolvable puzzle.
        AssertThat(solver.Status).Equals(SolverStatus.NoSolution);
    }

    [TestCase]
    public void U19_AcGraphNoSolution() {
        RHGameState unsolvable = Levels.LoadLevelString(testUnsolvableLevelString).State;
        AssertBool(unsolvable.IsSolved()).IsFalse();

        var solver = new AcGraphSolver(new NullHeuristic());
        solver.Start(unsolvable);

        // The solver must terminate in V steps
        int maxSteps = 400;
        while (solver.Status == SolverStatus.Running && solver.StepCount <= maxSteps) {
            solver.Step();
        }

        // Ac is complete: it must report NoSolution for an unsolvable puzzle.
        AssertThat(solver.Status).Equals(SolverStatus.NoSolution);
    }

    

    [TestCase]
    public void U20_SolverEventsAreFired() {
        RHGameState initial = Levels.LoadLevelString(testLevelString).State;
        var solver = new AcGraphSolver(new DistanceHeuristic());

        int newEdgeCount = 0;
        int newCurrentCount = 0;
        int pathChangedCount = 0;
        bool terminatedFired = false;
        SolverStatus terminatedStatus = SolverStatus.NotStarted;

        solver.NewEdge     += (_, _)      => newEdgeCount++;
        solver.NewCurrent  += (_, _)      => newCurrentCount++;
        solver.Terminated  += (_, status) => { 
            terminatedFired = true;
            terminatedStatus = status; 
        };

        solver.Start(initial);

        int maxSteps = 500;
        while (solver.Status == SolverStatus.Running && solver.StepCount <= maxSteps) {
            solver.Step();
            // NewEdge should be at least the number of states discovered (apart from the initial states)
            // And every step taken (apart from the solution verification last step) extended a new state
            AssertInt(newEdgeCount).IsGreaterEqual(solver.StepCount - 1);
        
            // NewCurrent should be triggered once every step.
            AssertInt(newCurrentCount).Equals(solver.StepCount);
            
            // PathChanged should be triggered once every step, apart from the last step.
            if (solver.Status == SolverStatus.Running) {
                AssertInt(pathChangedCount).Equals(solver.StepCount);
            } else {
                AssertInt(pathChangedCount).Equals(solver.StepCount - 1);
            }
        }

        // The solver must have reached a terminal state, it should be Solved.
        AssertBool(terminatedFired).IsTrue();
        AssertThat(terminatedStatus).Equals(SolverStatus.Solved);
    }

    [TestCase]
    public void U21_DiscovererStateLimit() {
        RHGameState initial = Levels.LoadLevelString(testLevelString).State;

        int maxStates = 400;

        // First discover all states
        var bfsd = new BFSDiscoverer(int.MaxValue);
        bfsd.Start(initial);

        while (bfsd.Status == SolverStatus.Discovering && bfsd.StepCount <= maxStates) {
            bfsd.Step();
        }

        // The discoverer must have discovered all states
        AssertThat(bfsd.Status).Equals(SolverStatus.DiscoverEndAllFound);
        
        // Step count should be less or equal as the states upper bound.
        AssertInt(bfsd.StepCount).IsLessEqual(maxStates);

        int stateCount = bfsd.StepCount;

        var dfsd = new DFSDiscoverer(int.MaxValue);
        dfsd.Start(initial);

        while (dfsd.Status == SolverStatus.Discovering && dfsd.StepCount <= maxStates) {
            dfsd.Step();
        }

        AssertThat(dfsd.Status).Equals(SolverStatus.DiscoverEndAllFound);

        // DFS should also terminate with the same amount of states
        AssertInt(dfsd.StepCount).Equals(stateCount);

        int limit = stateCount - 1;

        // discover with a limit smaller than the amount of states
        var bfsd2 = new BFSDiscoverer(limit);
        while (bfsd2.Status == SolverStatus.Discovering && bfsd2.StepCount <= maxStates) {
            bfsd2.Step();
        }

        AssertThat(bfsd2.Status).Equals(SolverStatus.DiscoverEndLimitReached);
        AssertInt(bfsd2.StepCount).Equals(limit);

        // same for dfs
        var dfsd2 = new DFSDiscoverer(limit);
        while (dfsd2.Status == SolverStatus.Discovering && dfsd2.StepCount <= maxStates) {
            dfsd2.Step();
        }

        AssertThat(dfsd2.Status).Equals(SolverStatus.DiscoverEndLimitReached);
        AssertInt(dfsd2.StepCount).Equals(limit);
    }

     [TestCase]
    public void U22_StateMoveSymmetry() {
        RHGameState stateA = Levels.LoadLevelString(testLevelString).State;
        Move move = stateA.GetPossibleMoves().First();
        RHGameState stateB = stateA.WithMove(move);

        StateMove ab = new StateMove(stateA, stateB, move);
        // Construct the reverse move (same piece, opposite direction).
        Move reverseMove = new Move {
            PieceIndex = move.PieceIndex,
            Dir = move.Dir.GetOpposite()
        };
        StateMove ba = new StateMove(stateB, stateA, reverseMove);

        // Equality must hold in both directions.
        AssertBool(ab == ba).IsTrue();
        AssertBool(ba == ab).IsTrue();
        AssertInt(ab.GetHashCode()).Equals(ba.GetHashCode());
    }

}