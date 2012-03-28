using System;
using System.Collections.Generic;

public static class ArrayHelper
{
    #region In

    public static bool In<T>(IEqualityComparer<T> comparer, T value, params T[] values) 
    {
        foreach (var v in values)
            if (comparer.Equals(value, v)) return true;
        return false;
    }
    public static bool In<T>(IEqualityComparer<T> comparer, T value, T value1, T value2)
    {
        return (comparer.Equals(value, value1) || comparer.Equals(value, value2));
    }
    public static bool In<T>(IEqualityComparer<T> comparer, T value, T value1, T value2, T value3)
    {
        return In(comparer, value, value1, value2) || comparer.Equals(value, value3);
    }
    public static bool In<T>(IEqualityComparer<T> comparer, T value, T value1, T value2, T value3, T value4)
    {
        return In(comparer, value, value1, value2, value3) || comparer.Equals(value, value4);
    }
    public static bool In<T>(IEqualityComparer<T> comparer, T value, T value1, T value2, T value3, T value4, T value5)
    {
        return In(comparer, value, value1, value2, value3, value4) || comparer.Equals(value, value5);
    }
    public static bool In<T>(IEqualityComparer<T> comparer, T value, T value1, T value2, T value3, T value4, T value5, T value6)
    {
        return In(comparer, value, value1, value2, value3, value4, value5) || comparer.Equals(value, value6);
    }

    public static bool In<T>(T value, params T[] values) where T : IEquatable<T>
    {
        foreach (var v in values)
            if (value.Equals(v)) return true;
        return false;
    }
    public static bool In<T>(T value, T value1, T value2) where T : IEquatable<T>
    {
        return (value.Equals(value1) || value.Equals(value2));
    }
    public static bool In<T>(T value, T value1, T value2, T value3) where T : IEquatable<T>
    {
        return In(value, value1, value2) || value.Equals(value3);
    }
    public static bool In<T>(T value, T value1, T value2, T value3, T value4) where T : IEquatable<T>
    {
        return In(value, value1, value2, value3) || value.Equals(value4);
    }
    public static bool In<T>(T value, T value1, T value2, T value3, T value4, T value5) where T : IEquatable<T>
    {
        return In(value, value1, value2, value3, value4) || value.Equals(value5);
    }
    public static bool In<T>(T value, T value1, T value2, T value3, T value4, T value5, T value6) where T : IEquatable<T>
    {
        return In(value, value1, value2, value3, value4, value5) || value.Equals(value6);
    }

    #endregion

    #region Coalesce

    public static T Coalesce<T>(IEqualityComparer<T> comparer, T first, T second) where T : struct
    {
        T defaultValue = default(T);

        if (!comparer.Equals(defaultValue, first)) return first;
        if (!comparer.Equals(defaultValue, second)) return second;

        return defaultValue;
    }
    public static T Coalesce<T>(IEqualityComparer<T> comparer, T first, T second, T third) where T : struct
    {
        T defaultValue = default(T);

        if (!comparer.Equals(defaultValue, first)) return first;
        if (!comparer.Equals(defaultValue, second)) return second;
        if (!comparer.Equals(defaultValue, third)) return third;

        return defaultValue;
    }
    public static T Coalesce<T>(IEqualityComparer<T> comparer, T first, T second, T third, T fourth) where T : struct
    {
        T defaultValue = default(T);

        if (!comparer.Equals(defaultValue, first)) return first;
        if (!comparer.Equals(defaultValue, second)) return second;
        if (!comparer.Equals(defaultValue, third)) return third;
        if (!comparer.Equals(defaultValue, fourth)) return fourth;

        return defaultValue;
    }
    public static T Coalesce<T>(IEqualityComparer<T> comparer, params T[] values) where T : struct, IEquatable<T>
    {
        return Coalesce(comparer, values as IEnumerable<T>);
    }
    public static T Coalesce<T>(IEqualityComparer<T> comparer, IEnumerable<T> values) where T : struct
    {
        T defaultValue = default(T);

        foreach (var value in values)
            if (!comparer.Equals(defaultValue, value)) return value;

        return defaultValue;
    }

    public static T Coalesce<T>(T first, T second) where T : struct, IEquatable<T>
    {
        T defaultValue = default(T);

        if (!first.Equals(defaultValue)) return first;
        if (!second.Equals(defaultValue)) return second;

        return defaultValue;
    }
    public static T Coalesce<T>(T first, T second, T third) where T : struct, IEquatable<T>
    {
        T defaultValue = default(T);

        if (!first.Equals(defaultValue)) return first;
        if (!second.Equals(defaultValue)) return second;
        if (!third.Equals(defaultValue)) return third;

        return defaultValue;
    }
    public static T Coalesce<T>(T first, T second, T third, T fourth) where T : struct, IEquatable<T>
    {
        T defaultValue = default(T);

        if (!first.Equals(defaultValue)) return first;
        if (!second.Equals(defaultValue)) return second;
        if (!third.Equals(defaultValue)) return third;
        if (!fourth.Equals(defaultValue)) return fourth;

        return defaultValue;
    }
    public static T Coalesce<T>(params T[] values) where T : struct, IEquatable<T>
    {
        return Coalesce(values as IEnumerable<T>);
    }
    public static T Coalesce<T>(IEnumerable<T> values) where T : struct, IEquatable<T>
    {
        T defaultValue = default(T);

        foreach (var value in values)
            if (!value.Equals(defaultValue)) return value;

        return defaultValue;
    }

    #endregion

    public static T Next<T>(this T[] values, T current)
    {
        var currentIndex = Array.IndexOf(values, current);
        return values[Math.Min(currentIndex + 1, values.Length - 1)];
    }

    public static T Previous<T>(this T[] values, T current)
    {
        var currentIndex = Array.IndexOf(values, current);
        return values[Math.Max(currentIndex - 1, 0)];
    }
}
