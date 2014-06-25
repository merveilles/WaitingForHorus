using System.Linq;
using UnityEngine;

class LeaderboardViewerScript : MonoBehaviour
{
    #pragma warning disable 0649
    public GUISkin Skin;
    GUIStyle RowStyle, MyRowStyle, MultiRowWindowStyle;

    public GameObject LeaderboardPrefab;
    NetworkLeaderboard Leaderboard;
    #pragma warning restore 0649

    bool visible;

    public void Awake()
    {
        DontDestroyOnLoad( gameObject );
        MultiRowWindowStyle = new GUIStyle(Skin.window) { padding = { bottom = 0 } };
        RowStyle = new GUIStyle(Skin.box);
        MyRowStyle = new GUIStyle(Skin.box);
        if( Network.isServer ) 
            Network.Instantiate( LeaderboardPrefab, Vector3.zero, Quaternion.identity, 0 );
    }

    public void OnDisconnectedFromServer(NetworkDisconnection info) 
    {
        Leaderboard = null;
    }

    public void Update()
    {
        visible = Input.GetKey(KeyCode.Tab) || RoundScript.Instance.RoundStopped;
    }

    public void OnGUI()
    {
        if (Network.peerType == NetworkPeerType.Disconnected || Network.peerType == NetworkPeerType.Connecting) return;

        if (Leaderboard == null)
            Leaderboard = NetworkLeaderboard.Instance;

        GUI.skin = Skin;

        if (!visible)
        {
            //var height = 32;
           // GUILayout.Window(2, new Rect(278, 0, /*466*/376, height), BoardRow, string.Empty, SingleRowWindowStyle);
        }
        else
        {
            var height = Leaderboard.Entries.Count(x => PlayerRegistry.Has(x.NetworkPlayer) && PlayerRegistry.For(x.NetworkPlayer).Spectating) * 32;
            GUILayout.Window(2, new Rect(Screen.width - 445, (  40 ) - height / 2, 376, height), BoardWindow, string.Empty, MultiRowWindowStyle);
        }
    }

    // TODO unused method?
    //void BoardRow(int windowId)
    //{
    //    var log = Leaderboard.Entries.FirstOrDefault(x => x.NetworkPlayer == Network.player);
    //    if (log == null || !PlayerRegistry.Has(Network.player)) return;
    //    {
    //        GUIStyle rowStyle = RowStyle;
    //       // rowStyle.normal.textColor = PlayerRegistry.For(log.NetworkPlayer).Color;

    //        GUILayout.BeginHorizontal();
    //        GUILayout.Box(PlayerRegistry.For(log.NetworkPlayer).Username.ToUpper(), rowStyle, GUILayout.MinWidth(125), GUILayout.MaxWidth(125));

    //        //rowStyle.normal.textColor = Color.white;

    //        GUILayout.Box(log.Kills + " K", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
    //        GUILayout.Box(log.Deaths + " D", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
    //        //GUILayout.Label(log.Ratio.ToString() + " R", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
    //        GUILayout.Box(log.Ping + " P", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
    //        GUILayout.EndHorizontal();
    //    }
    //}

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

            //rowStyle.normal.textColor = PlayerRegistry.For(log.NetworkPlayer).Color;

            GUILayout.BeginHorizontal();
            GUILayout.Box(PlayerRegistry.For(log.NetworkPlayer).Username.ToUpper(), rowStyle, GUILayout.MinWidth(125), GUILayout.MaxWidth(125));

           // rowStyle.normal.textColor = Color.white;

            GUILayout.Box(log.Kills + " K", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            GUILayout.Box(log.Deaths + " D", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            //GUILayout.Label(log.Ratio.ToString() + " R", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            GUILayout.Box(log.Ping + " P", rowStyle, GUILayout.MinWidth(90), GUILayout.MaxWidth(90));
            GUILayout.EndHorizontal();
        }
    }
}

