using System.Collections.Generic;
using System.Linq;
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

    private GUISkin _Skin;

    public GUISkin Skin
    {
        get
        {
            return _Skin;
        }
        set
        {
            _Skin = value;
            RecalculateStyles();
        }
    }

    public int MessageCount { get { return Messages.Count; } }

    public bool IsInputFieldEnabled { get; set; }

    public delegate void MessageEnteredHandler(string text);
    public event MessageEnteredHandler OnMessageEntered = delegate { };

    public delegate void CommandEnteredHandler(string command, string[] args);
    public event CommandEnteredHandler OnCommandEntered = delegate { };

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
            {
                if (CurrentInput.StartsWith("/"))
                {
                    if (CurrentInput.Length > 1)
                    {
                        var parts = CurrentInput.Substring(1).Split(' ');
                        if (parts.Length > 0)
                        {
                            var args = parts.Skip(1).ToArray();
                            OnCommandEntered(parts[0], args);
                        }
                    }
                }
                else
                {
                    OnMessageEntered(CurrentInput);
                }
            }
            CurrentInput = "";
            IsInputFieldEnabled = false;
        }
        // TODO make an input button
        if (Event.current.keyCode == KeyCode.T && !IsInputFieldEnabled)
        {
            IsInputFieldEnabled = true;
            DropFirstInput = true;
            //var previousInput = CurrentInput;
            //DeferredAction += () =>
            //{
            //    CurrentInput = previousInput;
            //};
        }

        GUI.skin = Skin;

        // Never shot input field when disconnected (so that we can type in the name box)
        IsInputFieldEnabled = IsInputFieldEnabled && Relay.Instance.CurrentServer != null;

	    GUILayout.Window(1, new Rect(35, 0, 247, Screen.height), DisplayLog, string.Empty);

    }

    private void DisplayLog(int id)
    {
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        foreach (var message in Messages)
        {
            GUILayout.BeginHorizontal();
            //rowStyle.normal.textColor = PlayerRegistry.For(log.Player).Color;
            GUILayout.Box(message.Content, VariableHeightBoxStyle);
            GUILayout.EndHorizontal();
        }
        if (IsInputFieldEnabled)
        {
            DisplayInput();
            GUI.FocusWindow(1);
            GUI.FocusControl("MessageInput");
            //Screen.lockCursor = false;
            //Screen.showCursor = true;
        }
        else
        {
            GUILayout.BeginVertical();
            GUILayout.Space(35);
            GUILayout.EndVertical();
        }
    }

    private void DisplayInput()
    {
        GUILayout.BeginHorizontal();
        GUI.SetNextControlName("MessageInput");
		
        var newInput = GUILayout.TextField(CurrentInput, InputRowStyle);
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


    private GUIStyle InputRowStyle;
    private GUIStyle VariableHeightBoxStyle;

    private void RecalculateStyles()
    {
            InputRowStyle = new GUIStyle( Skin.textField ) { fixedWidth = 200 };

            var basePadding = Skin.box.padding;
            basePadding.top = 11;
            basePadding.bottom = 12;
            basePadding.right = 12;
            VariableHeightBoxStyle = new GUIStyle(Skin.box)
            {
                fixedWidth = 200,
                stretchHeight = false,
                fixedHeight = 0,
                padding = basePadding
            };
    }
}
