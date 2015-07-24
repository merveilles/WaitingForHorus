using System;
using UnityEngine;

public static class MathHelper
{
    public const float Pi = (float)Math.PI;
    public const float PiOver2 = (float)(Math.PI / 2);
    public const float PiOver4 = (float)(Math.PI / 4);

    public static bool AlmostEquals(float a, float b, float epsilon)
    {
        return Math.Abs(a - b) <= epsilon;
    }
    public static bool AlmostEquals(Vector3 a, Vector3 b, float epsilon)
    {
        return AlmostEquals(a.x, b.x, epsilon) && AlmostEquals(a.y, b.y, epsilon) && AlmostEquals(a.z, b.z, epsilon);
    }

    public static bool Approximately(Vector3 a, Vector3 b)
    {
        return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
    }

    public static Vector3 Modulate(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }
}
