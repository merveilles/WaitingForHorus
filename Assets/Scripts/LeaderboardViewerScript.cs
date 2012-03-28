using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class LeaderboardViewerScript : MonoBehaviour
{
    public GUISkin Skin;
    GUIStyle RowStyle, MyRowStyle;

    public GameObject LeaderboardPrefab;
    NetworkLeaderboard Leaderboard;

    bool visible;

    void Awake()
    {
        RowStyle = new GUIStyle { normal = { textColor = Color.white }, padding = { top = 9, left = 5, right = 10 }, fixedHeight = 32 };
        MyRowStyle = new GUIStyle(RowStyle) { normal = { background = Skin.window.normal.background } };
    }

    void OnServerInitialized()
    {
        Network.Instantiate(LeaderboardPrefab, Vector3.zero, Quaternion.identity, 0);
    }
    void OnDisconnectedFromServer(NetworkDisconnection info) 
    {
        if (Network.isServer)
        {
            Network.RemoveRPCs(NetworkLeaderboard.Instance.networkView.viewID);
            Network.Destroy(NetworkLeaderboard.Instance.networkView.viewID);
        }
        Leaderboard = null;
    }

    void Update()
    {
        visible = Input.GetKey(KeyCode.Tab);
    }

    void OnGUI()
    {
        if (Network.peerType == NetworkPeerType.Disconnected || Network.peerType == NetworkPeerType.Connecting) return;

        if (Leaderboard == null)
            Leaderboard = NetworkLeaderboard.Instance;

        if (!visible) return;

        GUI.skin = Skin;

        var height = Leaderboard.Entries.Count * 32;
        GUILayout.Window(2, new Rect(278, Screen.height / 2 - height / 2, 466, height), BoardWindow, string.Empty);
    }
    void BoardWindow(int windowId)
    {
        foreach (var log in Leaderboard.Entries)
        {
            GUIStyle rowStyle = RowStyle;
            if (log.NetworkPlayer == Network.player)
                rowStyle = MyRowStyle;
            rowStyle.normal.textColor = PlayerRegistry.For[log.NetworkPlayer].Color;

            GUILayout.BeginHorizontal();
            GUILayout.Label(PlayerRegistry.For[log.NetworkPlayer].Username.ToUpper(), rowStyle, GUILayout.MinWidth(125), GUILayout.MaxWidth(125));

            rowStyle.normal.textColor = Color.white;
            
            GUILayout.Label(log.Kills.ToString(), rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            GUILayout.Label(log.Deaths.ToString(), rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            GUILayout.Label(log.Ratio.ToString(), rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            GUILayout.Label(log.Ping.ToString(), rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            GUILayout.EndHorizontal();
        }
    }
}

