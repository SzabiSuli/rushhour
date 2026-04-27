
public abstract class RHSolver {
    private SolverStatus _status = SolverStatus.NotStarted;
    public SolverStatus Status { 
        get => _status;
        protected set {
            _status = value;
            if (   _status == SolverStatus.Solved 
                || _status == SolverStatus.NoSolution 
                || _status == SolverStatus.Terminated
                || _status == SolverStatus.DiscoverEndAllFound 
                || _status == SolverStatus.DiscoverEndLimitReached) {
                OnTerminated();
            }
        } 
    }
    protected readonly Random rand = new Random();
    protected readonly float randomFactor;
    private RHGameState? _current;
    public virtual int StepCount {get; protected set;} = 0;
    protected RHGameState? _solvedStateFound;

    public event EventHandler<RHGameState>? NewCurrent;
    public event EventHandler<PathChangeArgs>? PathChange;
    public event EventHandler<StateMove>? NewEdge;
    public event EventHandler<SolverStatus>? Terminated;

    private bool _skipNewCurrent = false;

    protected void OnNewCurrent(RHGameState state) {
        _current = state;
        if (_skipNewCurrent) return;
        NewCurrent?.Invoke(this, state);
    } 
    protected void OnPathChange(PathChangeArgs args) => PathChange?.Invoke(this, args);
    protected void OnNewEdge(StateMove edge) => NewEdge?.Invoke(this, edge);
    protected void OnTerminated() {
        if (_current != null) {
            NewCurrent?.Invoke(this, _current);
        }

        Terminated?.Invoke(this, Status);
    } 
    
    public Heuristic<RHGameState> Heuristic { get; protected init; }

    public RHSolver(Heuristic<RHGameState> heuristic, float randomFactor = 0) {
        Heuristic = heuristic;
        this.randomFactor = randomFactor;
    }

    public float Evaluate(StateMove move) => Evaluate(move.To);

    public float Evaluate(RHGameState state){
        float result = Heuristic.Evaluate(state);
        if (randomFactor > 0) {
            result += rand.NextSingle() * randomFactor;
        }
        return result;
    }
    
    public virtual void Start(RHGameState initial) {
        Status = SolverStatus.Running;
        Extend(initial);
    }

    public virtual bool Extend(RHGameState state) {
        OnNewCurrent(state);

        if (state.IsSolved()) {
            _solvedStateFound = state;
            Status = SolverStatus.Solved;
            return true;
        }
        return false;
    }

    public void Step(int stepCount) {
        // Skip calling new currents when calling steps in batch
        _skipNewCurrent = true;
        for (int i = 0; i < stepCount - 1; i++) {
            Step();
            if (Status != SolverStatus.Running && Status != SolverStatus.Discovering) {
                // quit the function if the solver terminated
                _skipNewCurrent = false;
                return;
            }
        }
        _skipNewCurrent = false;
        Step();
    }

    public virtual void Step() => StepCount++;
    public IEnumerable<StateMove> GetSolutionPath() {
        if (Status != SolverStatus.Solved) {
            throw new Exception("Puzzle is not solved!");
        }
        return GetSolutionPathSolved();
    }
    protected abstract IEnumerable<StateMove> GetSolutionPathSolved();
}

public class TabuSolver : RHSolver {}

public class BacktrackingSolver(Heuristic<RHGameState> h, float rf = 0) : RHSolver(h, rf) {}


public class AcGraphSolver(MonotoneHeuristic<RHGameState> h, float rf = 0) : RHSolver(h, rf) {}

public abstract class Heuristic<TState>{
    public abstract int Evaluate(TState state);
}

public enum SolverStatus {}


public abstract class Discoverer : AcGraphSolver {}

public class BFSDiscoverer(int maxStates) : Discoverer (new NullHeuristic(), maxStates) {}
public class DFSDiscoverer(int maxStates) : Discoverer (new NullHeuristic(), maxStates) {} 
