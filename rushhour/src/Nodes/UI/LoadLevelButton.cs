namespace rushhour.src.Nodes.UI;

using System;
using System.ComponentModel;
using Godot;
using rushhour.src.Model;
using rushhour.src.Nodes.Board;

public partial class LoadLevelButton : PanelContainer
{

    [Export] public Button button = null!;
    [Export] public GameBoard gameBoard = null!;
    [Export] public Label label = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        
    }

    public void Init(Level level) {
        label.Text = level.Title;
        gameBoard.Setup(level.State);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
