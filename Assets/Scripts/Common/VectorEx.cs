using System;
using System.Collections.Generic;
using Cancel.RateLimit;
using UnityEngine;

public static class VectorEx
{
    public static Vector2 XY(this Vector3 v)
    {
        return new Vector2(v.x, v.y);
    }

    public static float DistanceSquared(this Vector2 v1, Vector2 v2)
    {
        return (v2.x - v1.x) * (v2.x - v1.x) + (v2.y - v1.y) * (v2.y - v1.y);
    }

    public static Vector3 Sign(this Vector3 vector)
    {
        return new Vector3(Math.Sign(vector.x), Math.Sign(vector.y), Math.Sign(vector.z));
    }

    public static Vector3 Abs(this Vector3 vector)
    {
        return new Vector3(Math.Abs(vector.x), Math.Abs(vector.y), Math.Abs(vector.z));
    }

    public static Vector3 Floor(this Vector3 vector)
    {
        return new Vector3((int)vector.x, (int)vector.y, (int)vector.z);
    }

    public static Vector3 MaxClamp(this Vector3 vector)
    {
        var absVec = vector.Abs();

        if (absVec.x >= absVec.y && absVec.x >= absVec.z)
            return new Vector3(Math.Sign(vector.x), 0, 0);
        if (absVec.y >= absVec.x && absVec.y >= absVec.z)
            return new Vector3(0, Math.Sign(vector.y), 0);
        if (absVec.z >= absVec.x && absVec.z >= absVec.y)
            return new Vector3(0, 0, Math.Sign(vector.z));

        return Vector3.zero;
    }

    public static Vector3 MaxClampXZ(this Vector3 vector)
    {
        if (Math.Abs(vector.x) >= Math.Abs(vector.z))
            return new Vector3(Math.Sign(vector.x), 0, 0);
        return new Vector3(0, 0, Math.Sign(vector.z));
    }

    public static Vector3 Round(this Vector3 vector)
    {
        return new Vector3((float)Math.Round(vector.x, MidpointRounding.AwayFromZero),
                           (float)Math.Round(vector.y, MidpointRounding.AwayFromZero),
                           (float)Math.Round(vector.z, MidpointRounding.AwayFromZero));
    }

    public static Vector3 Clamp(this Vector3 vector, float min, float max)
    {
        return new Vector3(Mathf.Clamp(vector.x, min, max),
                           Mathf.Clamp(vector.y, min, max),
                           Mathf.Clamp(vector.z, min, max));
    }

    public static Vector3 New(float unit)
    {
        return new Vector3(unit, unit, unit);
    }

    public static Vector3 ToVector3(this Vector4 v)
    {
        return new Vector3(v.x, v.y, v.z);
    }
}

public static class MathExts
{
    public static void SpringDamperTo(float currentValue, float currentVelocity, float targetValue, float damping,
        float strength, float deltaTime, out float newValue, out float newVelocity)
    {
        newVelocity = currentVelocity + (targetValue - currentValue) * strength * deltaTime;
        newVelocity *= Mathf.Pow(damping, deltaTime);
        newValue = currentValue + newVelocity * deltaTime;
    }
    public static void SpringDamperTo(Vector3 currentValue, Vector3 currentVelocity, Vector3 targetValue, float damping,
        float strength, float deltaTime, out Vector3 newValue, out Vector3 newVelocity)
    {
        newVelocity = currentVelocity + (targetValue - currentValue) * strength * deltaTime;
        newVelocity *= Mathf.Pow(damping, deltaTime);
        newValue = currentValue + newVelocity * deltaTime;
    }

    public static Quaternion QScale(Quaternion q, float scale)
    {
        float angle;
        Vector3 axis;
        q.ToAngleAxis(out angle, out axis);
        return Quaternion.AngleAxis(angle * scale, axis);
    }

    // Euler angles will give us 0..360, but we want -180..180 for doing rotational velocity.
    private static Vector3 ToRelative(Vector3 eulers)
    {
        Vector3 result;
        result.x = (eulers.x + 180f) % 360f - 180f;
        result.y = (eulers.y + 180f) % 360f - 180f;
        result.z = (eulers.z + 180f) % 360f - 180f;
        return result;
    }

    public static void SpringDamperTo(Quaternion currentValue, Vector3 currentVelocity, Quaternion targetValue, float damping,
        float strength, float deltaTime, out Quaternion newValue, out Vector3 newVelocity)
    {
        if (Quaternion.Angle(currentValue, targetValue) <= Mathf.Epsilon && currentVelocity.sqrMagnitude <= 0.0001f)
        {
            newValue = targetValue;
            newVelocity = Vector3.zero;
            return;
        }
        Quaternion difference = Quaternion.Inverse(currentValue) * targetValue;
        Vector3 towards = ToRelative(difference.eulerAngles);
        newVelocity = currentVelocity + towards * strength * deltaTime;
        newVelocity *= Mathf.Pow(damping, deltaTime);
        if (newVelocity.sqrMagnitude < 0.0001f && towards.sqrMagnitude < 0.0001f)
        {
            newVelocity = Vector3.zero;
            newValue = targetValue;
            return;
        }
        newValue = currentValue * Quaternion.Euler(newVelocity * deltaTime);
    }
}

namespace Cancel.Interpolation
{
    public class ScalarSpring
    {
        public float CurrentValue;
        public float CurrentVelocity;
        public float TargetValue;
        public float Damping;
        public float Strength;

        public float ImpulseFalloff;
        public float AmortizedImpulse;
        public ScalarSpring(float startingValue)
        {
            CurrentValue = startingValue;
            TargetValue = startingValue;
            CurrentVelocity = 0f;
            AmortizedImpulse = 0f;
            Damping = 0.001f;
            Strength = 200f;
            ImpulseFalloff = 0.0001f;
        }
        public void Update()
        {
            CurrentVelocity += AmortizedImpulse * Time.deltaTime;
            AmortizedImpulse = Mathf.Lerp(AmortizedImpulse, 0f,
                1.0f - Mathf.Pow(ImpulseFalloff, Time.deltaTime));
            MathExts.SpringDamperTo(CurrentValue, CurrentVelocity, TargetValue, Damping, Strength, Time.deltaTime, out CurrentValue, out CurrentVelocity);
        }
        public void AddImpulse(float velocity)
        {
            AmortizedImpulse += velocity;
        }
    }
    public class PositionalSpring
    {
        public Vector3 CurrentValue;
        public Vector3 CurrentVelocity;
        public Vector3 TargetValue;
        public float Damping;
        public float Strength;

        public float ImpulseFalloff;
        private Vector3 AmortizedImpulse;

        public PositionalSpring(Vector3 startingValue)
        {
            CurrentValue = startingValue;
            TargetValue = startingValue;
            CurrentVelocity = Vector3.zero;
            AmortizedImpulse = Vector3.zero;
            Damping = 0.001f;
            Strength = 200f;
            ImpulseFalloff = 0.0001f;
        }
        public void Update()
        {
            CurrentVelocity += AmortizedImpulse * Time.deltaTime;
            AmortizedImpulse = Vector3.Lerp(AmortizedImpulse, Vector3.zero,
                1.0f - Mathf.Pow(ImpulseFalloff, Time.deltaTime));
            MathExts.SpringDamperTo(CurrentValue, CurrentVelocity, TargetValue, Damping, Strength, Time.deltaTime, out CurrentValue, out CurrentVelocity);
        }
        public void AddImpulse(Vector3 velocity)
        {
            AmortizedImpulse += velocity;
        }
    }

    public class RotationalSpring
    {
        public Quaternion CurrentValue;
        public Vector3 CurrentVelocity;
        public Quaternion TargetValue;
        public float Damping;
        public float Strength;

        public float ImpulseThrottleTime
        {
            get { return ThrottledImpulses.MinimumTimeBetweenItems; }
            set { ThrottledImpulses.MinimumTimeBetweenItems = value; }
        }

        public float ImpulseFalloff;
        public int ImpulseQueueLimit = 0;
        private Vector3 AmortizedImpulse;

        private readonly Throttler<Vector3> ThrottledImpulses = new Throttler<Vector3>();

        public void Update()
        {
            foreach (var eulerAngles in ThrottledImpulses.Update())
                AmortizedImpulse += eulerAngles;
            CurrentVelocity += AmortizedImpulse * Time.deltaTime;
            AmortizedImpulse = Vector3.Lerp(AmortizedImpulse, Vector3.zero,
                1.0f - Mathf.Pow(ImpulseFalloff, Time.deltaTime));
            MathExts.SpringDamperTo(CurrentValue, CurrentVelocity, TargetValue, Damping, Strength, Time.deltaTime, out CurrentValue, out CurrentVelocity);
        }

        public RotationalSpring(Quaternion startingValue)
        {
            CurrentValue = startingValue;
            TargetValue = startingValue;
            CurrentVelocity = Vector3.zero;
            AmortizedImpulse = Vector3.zero;
            Damping = 0.001f;
            Strength = 200f;
            ImpulseFalloff = 0.0001f;
            ImpulseThrottleTime = 0.12f;
        }

        public void AddImpulse(Vector3 eulerAngles)
        {
            if (ImpulseQueueLimit < 1 || ThrottledImpulses.Items.Count < ImpulseQueueLimit)
                ThrottledImpulses.Add(eulerAngles);
        }
    }
}

namespace Cancel.RateLimit
{
    public class Throttler<T>
    {
        public readonly Queue<T> Items = new Queue<T>();
        public float TimeSinceLast = 0f;
        public float MinimumTimeBetweenItems = 0.03f;

        public void Add(T item)
        {
            Items.Enqueue(item);
        }

        public IEnumerable<T> Update() 
        {
            TimeSinceLast += Time.deltaTime;
            while (Items.Count > 0 && TimeSinceLast > MinimumTimeBetweenItems)
            {
                TimeSinceLast -= MinimumTimeBetweenItems;
                yield return Items.Dequeue();
            }
            TimeSinceLast = Mathf.Min(TimeSinceLast, MinimumTimeBetweenItems);
        }
    }

    public class Delayer<T>
    {
        public float DelayTime = 0.03f;

        private class Item
        {
            public T Value;
            public float TimeInQueue;

            public Item(T value)
            {
                Value = value;
                TimeInQueue = 0f;
            }

            public void Update()
            {
                TimeInQueue += Time.deltaTime;
            }
        }
        private readonly Queue<Item> Items = new Queue<Item>();

        public void Add(T item)
        {
            Items.Enqueue(new Item(item));
        }

        public IEnumerable<T> Update()
        {
            foreach (var item in Items)
                item.Update();
            while (Items.Count > 0 && Items.Peek().TimeInQueue > DelayTime)
                yield return Items.Dequeue().Value;
        }
    }
}