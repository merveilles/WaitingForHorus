using System;

public struct TimedButtonState : IEquatable<TimedButtonState>
{
    public readonly ComplexButtonState State;
    public readonly float TimePressed;

    TimedButtonState(ComplexButtonState state, float timePressed)
    {
        State = state;
        TimePressed = timePressed;
    }

    internal TimedButtonState NextState(bool down, float elapsed)
    {
        return new TimedButtonState(State.NextState(down), down ? TimePressed + elapsed : 0);
    }

    //public override string ToString()
    //{
    //    return StringHelper.ReflectToString(this);
    //}

    public bool Equals(TimedButtonState other)
    {
        return Equals(other.State, State) && other.TimePressed.Equals(TimePressed);
    }
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != typeof (TimedButtonState)) return false;
        return Equals((TimedButtonState) obj);
    }
    public override int GetHashCode()
    {
        unchecked
        {
            return (State.GetHashCode() * 397) ^ TimePressed.GetHashCode();
        }
    }
    public static bool operator ==(TimedButtonState left, TimedButtonState right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(TimedButtonState left, TimedButtonState right)
    {
        return !left.Equals(right);
    }
}
