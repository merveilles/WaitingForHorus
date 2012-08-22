using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class ChatScript : MonoBehaviour
{
    public readonly List<ChatMessage> ChatLog = new List<ChatMessage>();

    public GUISkin Skin;
    GUIStyle ChatStyle, MyChatStyle;

	string lastMessage = "";
	bool showChat, ignoreT;
    bool forceVisible;

    public static ChatScript Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        ChatStyle = new GUIStyle { normal = { textColor = Color.white }, padding = { top = 9, left = 5, right = 10 }, fixedWidth = 209, fixedHeight = 32 };
        MyChatStyle = new GUIStyle(ChatStyle) { normal = { background = Skin.window.normal.background } };
    }

    void OnServerInitialized()
    {
        CleanUp();
    }
    void OnConnectedToServer()
    {
        CleanUp();
    }
    void CleanUp()
    {
        ChatLog.Clear();
        showChat = false;
        lastMessage = string.Empty;
    }

    void Update()
    {
        forceVisible = Input.GetKey(KeyCode.Tab) || RoundScript.Instance.RoundStopped;
    }

	void OnGUI()
	{
	    if (Network.peerType == NetworkPeerType.Disconnected || Network.peerType == NetworkPeerType.Connecting) return;

	    GUI.skin = Skin;

	    var enteredChat = !showChat && Event.current.keyCode == KeyCode.T;
	    if (enteredChat)
	        showChat = true;

	    var height = 32 + ChatLog.Count(x => !x.Hidden || forceVisible) * 32;
	    GUILayout.Window(1, new Rect(0, Screen.height - height, 277, height), Chat, string.Empty);

	    if (enteredChat)
	    {
	        GUI.FocusWindow(1);
	        GUI.FocusControl("ChatInput");
	        ignoreT = true;
	    }
	}

    void Chat(int windowId)
    {
        try
        {
            foreach (var log in ChatLog)
            {
                if (!PlayerRegistry.For.ContainsKey(log.Player))
                    continue;

                log.Life += Time.deltaTime;
                if (log.Life > 15)
                    log.Hidden = true;

                if (log.Hidden && !forceVisible)
                    continue;

                GUIStyle rowStyle = ChatStyle;
                if (log.Player == Network.player && !log.IsSystem) rowStyle = MyChatStyle;

                GUILayout.BeginHorizontal();
                if (!log.IsSourceless)
                    rowStyle.normal.textColor = PlayerRegistry.For[log.Player].Color;
                rowStyle.padding.left = 10;
                rowStyle.fixedWidth = 0;
                rowStyle.wordWrap = false;
                if (log.IsSystem)
                {
                    //rowStyle.fontStyle = FontStyle.Italic;
                    rowStyle.padding.right = 1;
                    if (!log.IsSourceless)
                    {
                        var playerName = PlayerRegistry.For[log.Player].Username.ToUpper();
                        rowStyle.fixedWidth = rowStyle.CalcSize(new GUIContent(playerName)).x;
                        GUILayout.Label(playerName, rowStyle);
                    }
                    else
                    {
                        rowStyle.fixedWidth = rowStyle.CalcSize(new GUIContent(" ")).x;
                        rowStyle.padding.left = 0;
                        GUILayout.Label(" ", rowStyle);
                    }
                    rowStyle.fixedWidth = 0;
                    rowStyle.padding.left = 0;
                    rowStyle.normal.textColor = Color.white;
                    GUILayout.Label(" " + log.Message, rowStyle);
                    rowStyle.padding.right = 10;
                    rowStyle.fontStyle = FontStyle.Normal;
                }
                else
                {
                    GUILayout.Label(PlayerRegistry.For[log.Player].Username.ToUpper() + ":", rowStyle, GUILayout.MinWidth(0), GUILayout.MaxWidth(100));
                    rowStyle.normal.textColor = Color.white;
                    rowStyle.padding.left = 5;
                    rowStyle.alignment = TextAnchor.UpperLeft;
                    rowStyle.wordWrap = true;
                    GUILayout.Label(log.Message, rowStyle, GUILayout.MaxWidth(225)); 
                }
                
                GUILayout.EndHorizontal();
            }

            GUILayout.HorizontalSlider(0, 0, 1, GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal();

            if (showChat)
            {
                GUI.SetNextControlName("ChatInput");

                lastMessage = GUILayout.TextField(lastMessage);

                if (ignoreT)
                {
                    if (lastMessage.ToLower() == "t")
                    {
                        lastMessage = string.Empty;
                        ignoreT = false;
                    }
                }

                if (Event.current.keyCode == KeyCode.Return)
                {
                    if (lastMessage.Trim() != string.Empty)
                        networkView.RPC("LogChat", RPCMode.All, Network.player, lastMessage, false, false);
                    lastMessage = string.Empty;
                    showChat = false;
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    lastMessage = string.Empty;
                    showChat = false;
                    Screen.lockCursor = true;
                }

                GUI.FocusControl("ChatInput");
            }

            GUILayout.FlexibleSpace();

            if (
                GUILayout.Button(Network.isServer || Network.connections.Length == 0
                                     ? "127.0.0.1"
                                     : Network.connections[0].externalIP))
                Network.Disconnect();

            GUILayout.EndHorizontal();
        }
        catch (ArgumentException)
        {
            // Wtf...
        }
    }
	
	[RPC]
    public void LogChat(NetworkPlayer player, string message, bool systemMessage, bool isSourceless)
    {
        //ChatLog.Insert(0, new Pair<string, string>(username, message));
        ChatLog.Add(new ChatMessage { Player = player, Message = message, IsSystem = systemMessage, IsSourceless = isSourceless });
        if (ChatLog.Count > 60)
            ChatLog.RemoveAt(0);
    }

    public class ChatMessage
    {
        public NetworkPlayer Player;
        public string Message;
        public bool IsSystem, IsSourceless;
        public float Life;
        public bool Hidden;
    }
}
