using System;
using System.Collections.Generic;
using System.Linq;

public static class RandomHelper
{
    public static readonly Random Random = new Random();

    public static bool Probability(double p)
    {
        return p >= Random.NextDouble();
    }

    public static int Sign()
    {
        return Probability(0.5) ? -1 : 1;
    }

    public static float Centered(double distance)
    {
        return (float)((Random.NextDouble() - 0.5) * distance * 2);
    }
    public static float Centered(double distance, double around)
    {
        return (float)((Random.NextDouble() - 0.5) * distance * 2 + around);
    }

    public static float Between(double min, double max)
    {
        return (float)(Random.NextDouble() * (max - min) + min);
    }

    public static T InEnumerable<T>(IEnumerable<T> enumerable)
    {
        return enumerable.ElementAt(Random.Next(0, enumerable.Count()));
    }

    public static T InEnum<T>(bool exclude0th) where T : struct
    {
        var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
        return values[Random.Next(exclude0th ? 1 : 0, values.Count())];
    }
    public static T InEnum<T>() where T : struct
    {
        return InEnum<T>(false);
    }
}
