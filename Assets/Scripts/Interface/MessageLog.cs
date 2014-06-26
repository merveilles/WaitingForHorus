using System;
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

    public bool IsInputFieldEnabled { get; set; }

    public delegate void MessageEnteredHandler(string text);
    public event MessageEnteredHandler OnMessageEntered = delegate { };

    private string CurrentInput = "";

    // We need to drop the first input after focusing the text field, otherwise
	// it enters the key used to summon it... maybe I'm doing something wrong,
	// but I couldn't find an easier way to avoid this.
    private bool DropFirstInput = false;

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

    public void Update()
    {
    }

    public void OnGUI()
    {

        // TODO move this somewhere else? maybe it's fine here for now
        if (Event.current.keyCode == KeyCode.Escape)
        {
            //CurrentInput = "";
            IsInputFieldEnabled = false;
            // TODO cursor lock/unlock via queue/stack
        }

        // TODO how should this stuff be ordered?
        if (Event.current.keyCode == KeyCode.Return &&
            (Event.current.type == EventType.KeyDown || Event.current.type == EventType.Layout) &&
            GUI.GetNameOfFocusedControl() == "MessageInput")
        {
            // Don't broadcast empty string. TODO should probably trim, too.
            if (CurrentInput != "")
                OnMessageEntered(CurrentInput);
            CurrentInput = "";
            IsInputFieldEnabled = false;
        }
        // TODO make an input button
        if (Event.current.keyCode == KeyCode.T && !IsInputFieldEnabled)
        {
            IsInputFieldEnabled = true;
            GUI.FocusWindow(1);
            GUI.FocusControl("MessageInput");
            DropFirstInput = true;
            //var previousInput = CurrentInput;
            //DeferredAction += () =>
            //{
            //    CurrentInput = previousInput;
            //};
        }

        GUI.skin = Skin;
        var height = 36 + MessageCount * 36;
	    GUILayout.Window(1, new Rect(35, Screen.height - height, 247, height), DisplayLog, string.Empty);

    }

    private void DisplayLog(int id)
    {
        foreach (var message in Messages)
        {
            GUIStyle rowStyle = new GUIStyle( Skin.box ) { fixedWidth = 200 };
            GUILayout.BeginHorizontal();
            //rowStyle.normal.textColor = PlayerRegistry.For(log.Player).Color;
            GUILayout.Box(message.Content, rowStyle);
            GUILayout.EndHorizontal();
        }
        if (IsInputFieldEnabled)
        {
            DisplayInput();
        }
    }

    private void DisplayInput()
    {
        GUILayout.BeginHorizontal();
        GUI.SetNextControlName("MessageInput");
		
		GUIStyle sty = new GUIStyle( Skin.textField ) { fixedWidth = 180 };
        var newInput = GUILayout.TextField(CurrentInput, sty);
        if (DropFirstInput && newInput != CurrentInput)
        {
            DropFirstInput = false;
        }
        else
        {
            CurrentInput = newInput;
        }
        GUILayout.EndHorizontal();
    }

}
