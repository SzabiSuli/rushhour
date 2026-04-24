namespace rushhour.src.Nodes.UI;


using System;
using Godot;
using rushhour.src.Model;


public partial class LevelsGrid : GridContainer
{

    public const String levelButtonScenePath = "res://scenes/ui/tabs/load_level_button.tscn";


    public static PackedScene LevelButtonCreator { get; } = 
        ResourceLoader.Load<PackedScene>(levelButtonScenePath);

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        for (int i = 0; i < Levels.LevelCount; i++) {
            var (levelString, level) = Levels.LoadLevel(i);
            LoadLevelButton llb = LevelButtonCreator.Instantiate<LoadLevelButton>();
            llb.Init(levelString, level);
            AddChild(llb);
            llb.button.Pressed += () => AlgoPlayer.Instance.LoadLevel(level);
        }
    }
}
