namespace rushhour.src.Nodes;

using Godot;
using rushhour.src.Model;
using System;

public partial class CarNode : VehicleNode
{
	public override void SetSprite(int index) {
        if (index < 0 || index > 9) {
            throw new ArgumentException("Sprite index for car must be between 0 and 9.");
        }
        int ts = (int)GameBoard.tileSize.X;

        RegionRect = new Rect2(ts * index, 8 * ts, ts, 2 * ts);
    }
}
