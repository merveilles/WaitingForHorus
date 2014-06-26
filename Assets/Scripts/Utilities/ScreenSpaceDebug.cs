using System.Collections.Generic;
using UnityEngine;

public class ScreenSpaceDebug : MonoBehaviour
{
    public static ScreenSpaceDebug Instance { get; private set; }
    private bool ShouldDraw = false;

    private class Message
    {
        private Vector3 Position;
        private string Text;
        private float Age;
        private float Lifetime;

        private Color Color;
        private Vector2? Size;
        private Vector2 Offset;

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
        public bool Finished {get { return Age >= Lifetime; }}

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
        }
    }
    public void Awake()
    {
        Instance = this;
        Messages = new List<Message>();
    }

    private List<Message> Messages;

    public void Update()
    {
        for (int i = Messages.Count - 1; i >= 0; i--)
        {
            Messages[i].Update();
            if (Messages[i].Finished)
                Messages.RemoveAt(i);
        }
        if (Input.GetKeyDown("f12"))
        {
            ShouldDraw = !ShouldDraw;
        }
    }

    public void OnGUI()
    {
        if (!ShouldDraw) return;
        for (int i = 0; i < Messages.Count; i++)
        {
            Messages[i].OnGUI();
        }
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
}