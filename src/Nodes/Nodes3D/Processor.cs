namespace rushhour.src.Nodes.Nodes3D;

using System;
using Godot;

public partial class Processor : Node
{
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta) {
        foreach (Vertex vertex in Vertex.Dict.Values) {
            vertex.EvalPendingForces();
        }

        // GD.Print("Update end");
    }

    
}
