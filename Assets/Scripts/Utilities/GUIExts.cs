using UnityEngine;

public static class GUIExts
{
    public static bool Button(string text, params GUILayoutOption[] options)
    {
        bool pressed = GUILayout.Button(text, options);
        if (pressed)
            GlobalSoundsScript.PlayButtonPress();
        return pressed;
    }
    public static bool Button(string text, GUIStyle style, params GUILayoutOption[] options)
    {
        bool pressed = GUILayout.Button(text, style, options);
        if (pressed)
            GlobalSoundsScript.PlayButtonPress();
        return pressed;
    }
}
