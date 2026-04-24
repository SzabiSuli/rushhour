namespace rushhour.src.Nodes.UI;


using System;
using Godot;

public partial class PlayPauseButton : Button
{

    [Export] CompressedTexture2D playIcon = null!;
    [Export] CompressedTexture2D pauseIcon = null!;
    [Export] Label playPauseLabel = null!;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        Toggled += OnToggled;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
    }


    public void OnToggled(bool playing) {
        Icon = playing ? pauseIcon : playIcon;
        playPauseLabel.Text = playing ? "Pause" : "Play";
    }
}
