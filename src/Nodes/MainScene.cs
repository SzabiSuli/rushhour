namespace rushhour.src.Nodes;

using System;
using Godot;

public partial class MainScene : Control {
    public static MainScene Instance {get; private set;} = null!;
    public MainScene() {
        if (Instance is null) {
            Instance = this;
        } else {
            throw new Exception("Should only create one instance of MainScene!");
        }
    }

    public override void _Ready(){
        RenderingServer.SetDefaultClearColor(Colors.Black);
    }
}
