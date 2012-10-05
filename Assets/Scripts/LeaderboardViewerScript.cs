using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class LeaderboardViewerScript : MonoBehaviour
{
    #pragma warning disable 0649
    public GUISkin Skin;
    GUIStyle RowStyle, MyRowStyle, SingleRowWindowStyle, MultiRowWindowStyle;

    public GameObject LeaderboardPrefab;
    NetworkLeaderboard Leaderboard;
    #pragma warning restore 0649

    bool visible;

    void Awake()
    {
        SingleRowWindowStyle = new GUIStyle(Skin.window) { normal = { background = null } };
        MultiRowWindowStyle = new GUIStyle(Skin.window) { padding = { bottom = 0 } };
        RowStyle = new GUIStyle { normal = { textColor = Color.white }, padding = { top = 9, left = 5, right = 10 }, fixedHeight = 32 };
        MyRowStyle = new GUIStyle(RowStyle) { normal = { background = Skin.window.normal.background } };
    }

    void OnServerInitialized()
    {
        Network.Instantiate(LeaderboardPrefab, Vector3.zero, Quaternion.identity, 0);
    }
    void OnDisconnectedFromServer(NetworkDisconnection info) 
    {
        Leaderboard = null;
    }

    void Update()
    {
        visible = Input.GetKey(KeyCode.Tab) || RoundScript.Instance.RoundStopped;
    }

    void OnGUI()
    {
        if (Network.peerType == NetworkPeerType.Disconnected || Network.peerType == NetworkPeerType.Connecting) return;

        if (Leaderboard == null)
            Leaderboard = NetworkLeaderboard.Instance;

        GUI.skin = Skin;

        if (!visible)
        {
            var height = 32;
            GUILayout.Window(2, new Rect(278, 0, /*466*/376, height), BoardRow, string.Empty, SingleRowWindowStyle);
        }
        else
        {
            var height = Leaderboard.Entries.Count(x => PlayerRegistry.Has(x.NetworkPlayer) && PlayerRegistry.For(x.NetworkPlayer).Spectating) * 32;
            GUILayout.Window(2, new Rect(278, Screen.height / 2 - height / 2, /*466*/376, height), BoardWindow, string.Empty, MultiRowWindowStyle);
        }
    }

    void BoardRow(int windowId)
    {
        var log = Leaderboard.Entries.FirstOrDefault(x => x.NetworkPlayer == Network.player);
        if (log == null || !PlayerRegistry.Has(Network.player)) return;
        {
            GUIStyle rowStyle = RowStyle;
            rowStyle.normal.textColor = PlayerRegistry.For(log.NetworkPlayer).Color;

            GUILayout.BeginHorizontal();
            GUILayout.Label(PlayerRegistry.For(log.NetworkPlayer).Username.ToUpper(), rowStyle, GUILayout.MinWidth(125), GUILayout.MaxWidth(125));

            rowStyle.normal.textColor = Color.white;

            GUILayout.Label(log.Kills.ToString() + " K", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            GUILayout.Label(log.Deaths.ToString() + " D", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            //GUILayout.Label(log.Ratio.ToString() + " R", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            GUILayout.Label(log.Ping.ToString() + " P", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            GUILayout.EndHorizontal();
        }
    }

    void BoardWindow(int windowId)
    {
        foreach (var log in Leaderboard.Entries.OrderByDescending(x => x.Kills))
        {
            if (!PlayerRegistry.Has(Network.player))
                continue;
            if (!PlayerRegistry.Has(log.NetworkPlayer) || PlayerRegistry.For(log.NetworkPlayer).Spectating)
                continue;

            GUIStyle rowStyle = RowStyle;
            if (log.NetworkPlayer == Network.player)
                rowStyle = MyRowStyle;

            rowStyle.normal.textColor = PlayerRegistry.For(log.NetworkPlayer).Color;

            GUILayout.BeginHorizontal();
            GUILayout.Label(PlayerRegistry.For(log.NetworkPlayer).Username.ToUpper(), rowStyle, GUILayout.MinWidth(125), GUILayout.MaxWidth(125));

            rowStyle.normal.textColor = Color.white;

            GUILayout.Label(log.Kills.ToString() + " K", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            GUILayout.Label(log.Deaths.ToString() + " D", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            //GUILayout.Label(log.Ratio.ToString() + " R", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            GUILayout.Label(log.Ping.ToString() + " P", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            GUILayout.EndHorizontal();
        }
    }
}

