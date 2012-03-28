using System;

public struct TimedAnalogButtonState : IEquatable<TimedAnalogButtonState>
{
    const double TriggerThreshold = 0.5;

    public readonly float Value;
    public readonly ComplexButtonState State;
    public readonly float TimePressed;

    TimedAnalogButtonState(float value, ComplexButtonState state, float timePressed)
    {
        Value = value;
        State = state;
        TimePressed = timePressed;
    }

    internal TimedAnalogButtonState NextState(float value, float elapsed)
    {
        var down = value > TriggerThreshold;
        return new TimedAnalogButtonState(value, State.NextState(down), down ? TimePressed + elapsed : 0);
    }

    public bool Equals(TimedAnalogButtonState other)
    {
        return other.Value == Value && other.State == State && other.TimePressed == TimePressed;
    }

    //public override string ToString()
    //{
    //    return StringHelper.ReflectToString(this);
    //}
}
