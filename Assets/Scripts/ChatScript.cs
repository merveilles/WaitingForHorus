using System;
using UnityEngine;
using System.Collections.Generic;

public class ChatScript : MonoBehaviour
{
    readonly List<Pair<NetworkPlayer, string>> ChatLog = new List<Pair<NetworkPlayer, string>>();

    public GUISkin Skin;
    GUIStyle ChatStyle, MyChatStyle;

	string lastMessage = "";
	bool showChat, ignoreT;

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

	void OnGUI()
	{
	    if (Network.peerType == NetworkPeerType.Disconnected || Network.peerType == NetworkPeerType.Connecting) return;

	    GUI.skin = Skin;

	    var enteredChat = !showChat && Event.current.keyCode == KeyCode.T;
	    if (enteredChat)
	        showChat = true;

	    var height = 32 + ChatLog.Count * 32;
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
                GUIStyle rowStyle = ChatStyle;
                if (log.First == Network.player) rowStyle = MyChatStyle;

                GUILayout.BeginHorizontal();
                rowStyle.normal.textColor = PlayerRegistry.For[log.First].Color;
                rowStyle.padding.left = 10;
                rowStyle.fixedWidth = 0;
                GUILayout.Label(PlayerRegistry.For[log.First].Username.ToUpper() + ":", rowStyle, GUILayout.MinWidth(0), GUILayout.MaxWidth(125));
                rowStyle.normal.textColor = Color.white;
                rowStyle.padding.left = 5;
                rowStyle.alignment = TextAnchor.UpperLeft;
                rowStyle.wordWrap = true;
                GUILayout.Label(log.Second, rowStyle, GUILayout.MaxWidth(200)); 
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
                        networkView.RPC("LogChat", RPCMode.All, Network.player, lastMessage);
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
        catch (ArgumentException e)
        {
            // Wtf...
        }
    }
	
	[RPC]
    public void LogChat(NetworkPlayer player, string message)
    {
        //ChatLog.Insert(0, new Pair<string, string>(username, message));
        ChatLog.Add(new Pair<NetworkPlayer, string>(player, message));
        if (ChatLog.Count > 10)
            ChatLog.RemoveAt(0);
    }
}
