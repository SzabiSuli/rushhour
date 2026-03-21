namespace rushhour.src.Model;

using System;
using Godot;

public struct Move {
    public int PieceIndex { get; init; }
    public Direction Dir { get; init; }
}

public enum Direction {
    Right,
    Down,
    Left,
    Up
}

public static class DirectionMethods{
    public static Direction GetOpposite(this Direction d) {
        return d switch {
            Direction.Right => Direction.Left,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Up => Direction.Down,
            _ => throw new ArgumentOutOfRangeException(nameof(d), $"Invalid direction: {d}")
        };
    }

    public static Vector2I GetVector(this Direction d) {
        return d switch {
            Direction.Right => new Vector2I(1, 0),
            Direction.Down => new Vector2I(0, 1),
            Direction.Left => new Vector2I(-1, 0),
            Direction.Up => new Vector2I(0, -1),
            _ => throw new ArgumentOutOfRangeException(nameof(d), $"Invalid direction: {d}")
        };
    }

}

public class StateMove {
    public RHGameState From {get; init;}
    public RHGameState To {get; init;}
    public Move Move {get; init;}

    private int _hashCode;

    public StateMove(RHGameState from, RHGameState to, Move move) {
        From = from;
        To = to;
        Move = move;

        _hashCode = CalculateHashCode();
    }

    private int CalculateHashCode() {
        int hash1 = From.GetHashCode();
        int hash2 = To.GetHashCode();

        // order the hashes, we don't want the hash to be affected by order.
        if (hash2 < hash1) {
            int temp = hash2;
            hash2 = hash1;
            hash1 = temp;
        }

        int hash = 17;
        hash = hash * 31 + hash1;
        hash = hash * 31 + hash2;

        return hash;
    }

    public override int GetHashCode() {
        return _hashCode;
    }

    public override bool Equals(object? obj) {
        if (obj is not StateMove other) return false;
        if (GetHashCode() != other.GetHashCode()) return false;
        return (From == other.From && To == other.To) || (From == other.To && To == other.From); 
    }

    public static bool operator ==(StateMove? left , StateMove? right) {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;

        return left.Equals(right);
    }

    public static bool operator !=(StateMove? left , StateMove? right) {
        return !(left == right);
    }
}

public struct PathChangeArgs {
    public bool onPath;
    public StateMove move;
}
