using System.Collections.Generic;
using UnityEngine;

public class GameMessage
{
    public string Content;

    public GameMessage(string content)
    {
        Content = content;
    }
}

public class MessageLog
{
    public readonly Queue<GameMessage> Messages = new Queue<GameMessage>();
    public int BufferSize = 10;
    public bool Visible { get; set; }
    public GUISkin Skin { get; set; }
    public int MessageCount { get { return Messages.Count; } }

    public void AddMessage(GameMessage message)
    {
        Messages.Enqueue(message);
        if (Messages.Count > BufferSize)
            Messages.Dequeue();
    }

    public void AddMessage(string text)
    {
        AddMessage(new GameMessage(text));
    }

    public void OnGUI()
    {
        GUI.skin = Skin;
        var height = 36 + MessageCount * 36;
	    GUILayout.Window(1, new Rect(35, Screen.height - height, 247, height), DisplayChat, string.Empty);
    }

    private void DisplayChat(int id)
    {
        foreach (var message in Messages)
        {
            //GUIStyle rowStyle = new GUIStyle( Skin.box ) { fixedWidth = 200 };
            GUILayout.BeginHorizontal();
            //rowStyle.normal.textColor = PlayerRegistry.For(log.Player).Color;
            GUILayout.Box(message.Content);
            GUILayout.EndHorizontal();
        }
    }

}
