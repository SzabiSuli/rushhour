namespace rushhour.src.Nodes;

using System;
using Godot;
using rushhour.src.Model;


public partial class LevelsGrid : GridContainer
{

    public const String levelButtonScenePath = "res://scenes/load_level_button.tscn";


    public static PackedScene LevelButtonCreator { get; } = 
        ResourceLoader.Load<PackedScene>(levelButtonScenePath);

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        foreach (var (levelString, level) in Levels.LoadLevels()) {
            LoadLevelButton llb = LevelButtonCreator.Instantiate<LoadLevelButton>();
            llb.Init(levelString, level);
            AddChild(llb);
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
