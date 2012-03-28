using System;
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
