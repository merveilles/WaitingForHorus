using System;
using UnityEngine;

static class ColorHelper
{
    public static void ColorToHSV(Color color, out float hue, out float saturation, out float value)
    {
        float max = Math.Max(color.r, Math.Max(color.g, color.b));
        float min = Math.Min(color.r, Math.Min(color.g, color.b));

        hue = color.GetHue();
        saturation = (max == 0) ? 0 : 1f - (1f * min / max);
        value = max;
    }

    public static float GetHue(this Color color)
    {
        if ((color.r == color.g) && (color.g == color.b)) return 0f;
        float num = color.r;
        float num2 = color.g;
        float num3 = color.b;
        float num7 = 0f;
        float num4 = num;
        float num5 = num;
        if (num2 > num4) num4 = num2;
        if (num3 > num4) num4 = num3;
        if (num2 < num5) num5 = num2;
        if (num3 < num5) num5 = num3;
        float num6 = num4 - num5;
        if (num == num4) num7 = (num2 - num3) / num6;
        else if (num2 == num4) num7 = 2f + ((num3 - num) / num6);
        else if (num3 == num4) num7 = 4f + ((num - num2) / num6);
        num7 *= 60f;
        if (num7 < 0f) num7 += 360f;
        return num7;
    }

    public static Color ColorFromHSV(float hue, float saturation, float value)
    {
        int hi = (int)(hue / 60) % 6;
        float f = hue / 60 - (int)(hue / 60);

        var v = value;
        var p = value * (1 - saturation);
        var q = value * (1 - f * saturation);
        var t = value * (1 - (1 - f) * saturation);

        if (hi == 0) return new Color(v, t, p);
        if (hi == 1) return new Color(q, v, p);
        if (hi == 2) return new Color(p, v, t);
        if (hi == 3) return new Color(p, q, v);
        if (hi == 4) return new Color(t, p, v);
        return new Color(v, p, q);
    }
}
