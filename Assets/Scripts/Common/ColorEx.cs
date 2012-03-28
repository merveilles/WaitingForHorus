using System;
using UnityEngine;

public static class ColorEx
{
    public static Color Saturate(this Color color)
    {
        return new Color(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), Mathf.Clamp01(color.a));
    }

    public static Color Round(this Color color)
    {
        return new Color((float)Math.Round(color.r, MidpointRounding.AwayFromZero),
                         (float)Math.Round(color.g, MidpointRounding.AwayFromZero),
                         (float)Math.Round(color.b, MidpointRounding.AwayFromZero),
                         (float)Math.Round(color.a, MidpointRounding.AwayFromZero));
    }
}
