namespace rushhour.src.Nodes.UI;

using Godot;
using System;

public partial class HideButton : CheckButton {
    public static HideButton Instance {get; private set;} = null!;
    public HideButton() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of AlgoPlayer!");
        }
    }
}