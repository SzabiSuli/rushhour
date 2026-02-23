namespace rushhour.src;

using rushhour.src.Model;

public class GraphEdge {
    public GraphNode From { get; set; }
    public GraphNode To { get; set; }
    public Move MoveUsed { get; set; }
}
