using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;

public class ScreenSpaceDebug : MonoBehaviour
{
    public static ScreenSpaceDebug Instance { get; private set; }
    public bool ShouldDraw = false;
    public GUISkin Skin;

    private class LineMessage
    {
        public string Text;
        public float Age;
        public float Lifetime;
        public Color Color;

        public bool DrawOnce = false;
        public bool Drawn = false;

        public LineMessage(string text, float lifetime, Color color)
        {
            Text = text;
            Lifetime = lifetime;
            Color = color;
            Age = 0f;
        }

        public void Update()
        {
            Age += Time.deltaTime;
        }
        public bool Finished {get { return Age >= Lifetime || (DrawOnce && Drawn); }}

        public void DrawLine(GUIStyle style)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Box(Text, style);
            GUILayout.EndHorizontal();
            Drawn = true;
        }

    }

    private class Message
    {
        private Vector3 Position;
        private string Text;
        private float Age;
        private float Lifetime;

        private Color Color;
        private Vector2? Size;
        private Vector2 Offset;

        // TODO hacks...
        public bool DrawOnce = false;
        public bool Drawn = false;

        public Message(Vector3 position, string text, float lifetime, Color color, Vector2? size, Vector2 offset)
        {
            Position = position;
            Text = text;
            Lifetime = lifetime;
            Age = 0f;

            Color = color;
            Size = size;
            Offset = offset;
        }

        public void Update()
        {
            Age += Time.deltaTime;
        }
        public bool Finished {get { return Age >= Lifetime || (DrawOnce && Drawn); }}

        public void OnGUI()
        {
            if (Camera.current == null) return;
            Vector3 screenPosition = Camera.current.WorldToScreenPoint(Position);
            screenPosition.y = Screen.height - screenPosition.y;
            if (screenPosition.z < 0) return;
            if (Size.HasValue)
            {
                var rect = new Rect(Offset.x + screenPosition.x - Size.Value.x / 2f, Offset.y + screenPosition.y - Size.Value.y / 2f, Size.Value.x, Size.Value.y);
                GUI.contentColor = Color;
                GUI.Box(rect, Text);
            }
            else
            {
                var rect = new Rect(Offset.x + screenPosition.x - 1, Offset.y + screenPosition.y + 2, 500, 500);
                GUI.contentColor = Color.black;
                GUI.Label(rect, Text);
                rect = new Rect(Offset.x + screenPosition.x, Offset.y + screenPosition.y, 500, 500);
                GUI.contentColor = Color;
                GUI.Label(rect, Text);
            }
            Drawn = true;
        }
    }
    public void Awake()
    {
        Instance = this;
        Messages = new List<Message>();
        LineMessages = new Dictionary<int, LineMessage>();
        AnonymousMessages = new List<LineMessage>();
        KeysCache = new List<int>();
    }

    private List<Message> Messages;
    private Dictionary<int, LineMessage> LineMessages;
    private List<LineMessage> AnonymousMessages; 

    private List<int> KeysCache;

    private int LineMessagesHeight
    {
        get { return (LineMessages.Count + AnonymousMessages.Count) * 36; }
    }

    public void Update()
    {
        // Purge old world space messages
        for (int i = Messages.Count - 1; i >= 0; i--)
        {
            Messages[i].Update();
            if (Messages[i].Finished || (!ShouldDraw && Messages[i].DrawOnce))
                Messages.RemoveAt(i);
        }

        // Purge old line messages
        KeysCache.Clear();
        foreach (KeyValuePair<int, LineMessage> line in LineMessages)
        {
            if (line.Value.Finished || (!ShouldDraw && line.Value.DrawOnce)) KeysCache.Add(line.Key);
        }
        for (int i = 0; i < KeysCache.Count; i++)
        {
            LineMessages.Remove(KeysCache[i]);
        }
        for (int i = AnonymousMessages.Count - 1; i >= 0; i--)
        {
            if (AnonymousMessages[i].Finished || (!ShouldDraw && AnonymousMessages[i].DrawOnce))
                AnonymousMessages.RemoveAt(i);
        }

        if (Input.GetKeyDown("f12"))
        {
            ShouldDraw = !ShouldDraw;
        }
    }

    public void OnGUI()
    {
        if (!ShouldDraw) return;

        // Draw world space messages
        for (int i = 0; i < Messages.Count; i++)
        {
            Messages[i].OnGUI();
        }

        // Draw screen space messages
        KeysCache.Clear();
        foreach (int i in LineMessages.Keys)
            KeysCache.Add(i);
        KeysCache.Sort();
        GUI.skin = Skin;
	    GUILayout.Window(3, new Rect( Screen.width - (35 + 300), 35, 300, 800), LineWindow, string.Empty);
    }

    private void LineWindow(int id)
    {
        var basePadding = Skin.box.padding;
        basePadding.top = 5;
        basePadding.bottom = 5;
        var style = new GUIStyle(Skin.box)
        {
            fixedWidth = 300,
            stretchHeight = false,
            fixedHeight = 0,
            padding = basePadding
        };
        foreach (int i in KeysCache)
        {
            LineMessages[i].DrawLine(style);
        }
        foreach (var line in AnonymousMessages)
        {
            line.DrawLine(style);
        }
    }

    public static void LogMessageSizes()
    {
        if (Instance == null) return;
        Debug.LogWarning("World messages: " + Instance.Messages.Count);
        Debug.LogWarning("Line messages: " + Instance.LineMessages.Count);
        Debug.LogWarning("Anonymous messages: " + Instance.AnonymousMessages.Count);
    }

    public static void AddMessage(string message, Vector3 worldPosition)
    {
        if (Instance == null) return;
        var msg = new Message(worldPosition, message, 2f, Color.white, null, Vector2.zero);
        Instance.Messages.Add(msg);
    }
    public static void AddMessage(string message, Vector3 worldPosition, Color color)
    {
        if (Instance == null) return;
        var msg = new Message(worldPosition, message, 2f, color, null, Vector2.zero);
        Instance.Messages.Add(msg);
    }
    public static void AddMessage(string message, Vector3 worldPosition, Vector2 size)
    {
        if (Instance == null) return;
        var msg = new Message(worldPosition, message, 2f, Color.white, size, Vector2.zero);
        Instance.Messages.Add(msg);
    }

    public static void AddMessageOnce(string message, Vector3 worldPosition)
    {
        if (Instance == null) return;
        var msg = new Message(worldPosition, message, 2f, Color.white, null, Vector2.zero);
        msg.DrawOnce = true;
        Instance.Messages.Add(msg);
    }

    public static void AddLineOnce(string message)
    {
        if (Instance == null) return;
        var line = new LineMessage(message, 1f, Color.white)
        {
            DrawOnce = true
        };
        Instance.AnonymousMessages.Add(line);
    }

}