namespace rushhour.src.Nodes.UI;

using Godot;
using System;
using rushhour.src.Nodes.Nodes3D;

public partial class HideButton : CheckButton {
    public static HideButton Instance {get; private set;} = null!;
    public HideButton() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of AlgoPlayer!");
        }
    }

    public override void _Ready() {
        Toggled += Vertex.OnHideButtonToggled;
        Toggled += Edge.OnHideButtonToggled;
    }
}
