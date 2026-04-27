namespace rushhour.src.Nodes.UI;


using Godot;
using rushhour.src.Model;
using System;

public partial class StatusContainer : VBoxContainer
{
    public static StatusContainer Instance {get; private set;} = null!;
    public StatusContainer() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of StatusContainer!");
        }
    }

    public Label StepCountLabel => GetChild<Label>(0);
    public Label SolverStatusLabel => GetChild<Label>(1);
    public Label SolutionLengthLabel => GetChild<Label>(2);
    public Label HeuristicLabel => GetChild<Label>(3);
    
    
    // Called when the node enters the scene tree for the first time.

    public void SetStatusLabel(SolverStatus? status, int? stepCount = null) {
        switch (status) {
            case null:
                SolverStatusLabel.Text = "Solver status: -";
                break;
            case SolverStatus.NotStarted:
                SolverStatusLabel.Text = "Solver status: Initiated";
                break;
            case SolverStatus.Running:
                if (stepCount == 0) {
                    SolverStatusLabel.Text = "Solver status: Initiated";
                } else {
                    SolverStatusLabel.Text = "Solver status: Running";
                }
                break;
            case SolverStatus.Solved:
                SolverStatusLabel.Text = "Solver status: Solved";
                break;
            case SolverStatus.NoSolution:
                SolverStatusLabel.Text = "Solver status: No solution";
                break;
            case SolverStatus.Terminated:
                SolverStatusLabel.Text = "Solver status: Stuck in dead end";
                break;
            case SolverStatus.Discovering:
                SolverStatusLabel.Text = "Discover status: Discovering states";
                break;
            case SolverStatus.DiscoverEndAllFound:
                SolverStatusLabel.Text = "Discover status: All states discovered";
                break;
            case SolverStatus.DiscoverEndLimitReached:
                SolverStatusLabel.Text = "Discover status: State limit reached";
                break;
        }
    }

    public void SetStepCount(int? stepCount) {
        StepCountLabel.Text = $"Step count: {stepCount?.ToString() ?? "-"}";
    }

    public void SetSolutionLength(int? solutionLength) {
        SolutionLengthLabel.Text = $"Solution length: {solutionLength?.ToString() ?? "-"}";
    }

    public void SetHeuristicLabel(int? heuristicScore) {
        HeuristicLabel.Text = $"Heuristic score: {heuristicScore?.ToString() ?? "-"}";
    }
    public void UpdateHeuristicLabel(RHGameState state) {
        SetHeuristicLabel(AlgoPlayer.Instance.Solver?.Heuristic.Evaluate(state));
    }
}
