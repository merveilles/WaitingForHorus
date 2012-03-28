using System;
using UnityEngine;

public struct DirectionalState : IEquatable<DirectionalState>
{
    public readonly Vector2 Direction;
    public readonly Vector2 Movement;

    public readonly TimedButtonState Up;
    public readonly TimedButtonState Down;
    public readonly TimedButtonState Left;
    public readonly TimedButtonState Right;

    DirectionalState(Vector2 direction, Vector2 movement, TimedButtonState up, TimedButtonState down, TimedButtonState left, TimedButtonState right) 
    {
        Direction = direction;
        Movement = movement;
        Up = up;
        Down = down;
        Left = left;
        Right = right;
    }

    internal DirectionalState NextState(bool up, bool down, bool left, bool right, float elapsed)
    {
        var direction = new Vector2(left ? -1 : right ? 1 : 0, up ? 1 : down ? -1 : 0);
        return new DirectionalState(direction, direction - Direction,
                                    Up.NextState(up, elapsed),
                                    Down.NextState(down, elapsed),
                                    Left.NextState(left, elapsed),
                                    Right.NextState(right, elapsed));
    }

    //public override string ToString()
    //{
    //    return StringHelper.ReflectToString(this);
    //}

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != typeof (DirectionalState)) return false;
        return Equals((DirectionalState) obj);
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int result = Direction.GetHashCode();
            result = (result * 397) ^ Movement.GetHashCode();
            result = (result * 397) ^ Up.GetHashCode();
            result = (result * 397) ^ Down.GetHashCode();
            result = (result * 397) ^ Left.GetHashCode();
            result = (result * 397) ^ Right.GetHashCode();
            return result;
        }
    }
    public static bool operator ==(DirectionalState left, DirectionalState right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(DirectionalState left, DirectionalState right)
    {
        return !left.Equals(right);
    }
    public bool Equals(DirectionalState other)
    {
        return other.Direction.Equals(Direction) && other.Movement.Equals(Movement) && other.Up.Equals(Up) && other.Down.Equals(Down) && other.Left.Equals(Left) && other.Right.Equals(Right);
    }
}
