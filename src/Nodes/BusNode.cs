namespace rushhour.src.Nodes;

using Godot;
using rushhour.src.Model;
using System;

public partial class BusNode : VehicleNode
{
	public override void SetSprite(int index) {
        if (index < 0 || index > 4) {
            throw new ArgumentException("Sprite index for bus must be between 0 and 4.");
        }
        int ts = (int)GameBoard.tileSize.X;

        RegionRect = new Rect2(ts * (index % 2), 3 * ts * (index / 2), ts, 3 * ts);
    }
}
