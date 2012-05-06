using System.Collections.Generic;
using UnityEngine;

class PlayerRegistry : MonoBehaviour
{
    static PlayerRegistry instance;
    public static PlayerRegistry Instance
    {
        get { return instance; }
    }

    void OnNetworkInstantiate(NetworkMessageInfo info)
    {
        instance = this;
    }

    public static Dictionary<NetworkPlayer, PlayerInfo> For = new Dictionary<NetworkPlayer, PlayerInfo>();

    public static void RegisterCurrentPlayer(string username)
    {
        Instance.networkView.RPC("RegisterPlayer", RPCMode.All, Network.player, username);
    }

    [RPC]
    public void RegisterPlayer(NetworkPlayer player, string username)
    {
        //Debug.Log(player + " = " + username);
        var color = Color.white;
        For.Add(player, new PlayerInfo { Username = username, Color = color });
    }
    [RPC]
    public void RegisterPlayerFull(NetworkPlayer player, string username, float r, float g, float b)
    {
        //Debug.Log(player + " = " + username);
        For.Add(player, new PlayerInfo { Username = username, Color = new Color(r, g, b)});
    }

    [RPC]
    public void UnregisterPlayer(NetworkPlayer player)
    {
        For.Remove(player);
    }

    public void OnPlayerConnected(NetworkPlayer player)
    {
        foreach (var otherPlayer in For.Keys)
            if (otherPlayer != player)
                networkView.RPC("RegisterPlayerFull", player, otherPlayer, For[otherPlayer].Username, For[otherPlayer].Color.r, For[otherPlayer].Color.g, For[otherPlayer].Color.b);
    }
    public void OnPlayerDisconnected(NetworkPlayer player)
    {
        //networkView.RPC("UnregisterPlayer", RPCMode.All, player);
    }
    public void OnDisconnectedFromServer(NetworkDisconnection info)
    {
        For.Clear();
    }

    public class PlayerInfo
    {
        public string Username;
        public Color Color;
    }
}
