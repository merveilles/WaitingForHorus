using System;
using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
    None, Left, Right, Up, Down, Forward, Backward
}
public class DirectionComparer : IEqualityComparer<Direction>
{
    public static DirectionComparer Default = new DirectionComparer();
    public bool Equals(Direction x, Direction y)
    {
        return (int)x == (int)y;
    }
    public int GetHashCode(Direction obj)
    {
        return ((int) obj).GetHashCode();
    }
}
public static class DirectionEx
{
    public static Direction FromVector(Vector3 vector)
    {
        if (MathHelper.Approximately(vector, Vector3.back)) return Direction.Backward;
        if (MathHelper.Approximately(vector, Vector3.forward)) return Direction.Forward;
        if (MathHelper.Approximately(vector, Vector3.left)) return Direction.Left;
        if (MathHelper.Approximately(vector, Vector3.right)) return Direction.Right;
        if (MathHelper.Approximately(vector, Vector3.up)) return Direction.Up;
        if (MathHelper.Approximately(vector, Vector3.down)) return Direction.Down;

        throw new InvalidOperationException("No direction for this vector");
    }

    public static Vector3 ToVector(this Direction direction)
    {
        switch (direction)
        {
            case Direction.Backward: return Vector3.back;
            case Direction.Forward: return Vector3.forward;
            case Direction.Left: return Vector3.left;
            case Direction.Right: return Vector3.right;
            case Direction.Up: return Vector3.up;
            case Direction.Down: return Vector3.down;
        }
        throw new InvalidOperationException("No vector for this direction");
    }
}

public enum ComplexButtonState
{
    Up, Pressed, Released, Down
}

public static class ButtonStateEx
{
    public static bool IsDown(this ComplexButtonState state)
    {
        return state == ComplexButtonState.Pressed || state == ComplexButtonState.Down;
    }

    public static ComplexButtonState NextState(this ComplexButtonState state, bool pressed)
    {
        switch (state)
        {
            case ComplexButtonState.Up:
                return pressed ? ComplexButtonState.Pressed : ComplexButtonState.Up;
            case ComplexButtonState.Pressed:
                return pressed ? ComplexButtonState.Down : ComplexButtonState.Released;
            case ComplexButtonState.Released:
                return pressed ? ComplexButtonState.Pressed : ComplexButtonState.Up;
            default:
                //case ButtonState.Down:
                return pressed ? ComplexButtonState.Down : ComplexButtonState.Released;
        }
    }
}
public class ComplexButtonStateComparer : IEqualityComparer<ComplexButtonState>
{
    public static readonly ComplexButtonStateComparer Default = new ComplexButtonStateComparer();
    public bool Equals(ComplexButtonState x, ComplexButtonState y) { return x == y; }
    public int GetHashCode(ComplexButtonState obj) { return (int)obj; }
}