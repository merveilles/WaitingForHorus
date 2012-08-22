using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class NetworkLeaderboard : MonoBehaviour
{
    public readonly List<LeaderboardEntry> Entries = new List<LeaderboardEntry>();

    static NetworkLeaderboard instance;
    public static NetworkLeaderboard Instance
    {
        get { return instance; }
    }

    void OnNetworkInstantiate(NetworkMessageInfo info)
    {
        instance = this;

        if (Network.isServer)
            Entries.Add(new LeaderboardEntry
            {
                Ping = Network.GetLastPing(Network.player),
                NetworkPlayer = Network.player
            });
    }

    void Update()
    {
        if (Network.isServer)
        {
            foreach (var entry in Entries)
                entry.Ping = Network.GetLastPing(entry.NetworkPlayer);
        }

        // update colors
        var isFirst = true;
        var isSecond = false;
        foreach (var entry in Entries.OrderByDescending(x => x.Kills))
        {
            if (!PlayerRegistry.For.ContainsKey(Network.player) ||
                !PlayerRegistry.For.ContainsKey(entry.NetworkPlayer))
                continue;

            var player = PlayerRegistry.For[entry.NetworkPlayer];
            if (isSecond)
                player.Color = new Color(114 / 255f, 222 / 255f, 194 / 255f); // cyan
            else if (isFirst)
                player.Color = new Color(255 / 255f, 166 / 255f, 27 / 255f); // orange
            else
                player.Color = new Color(226f / 255, 220f / 255, 198f / 255); // blanc cassé

            if (isFirst)
            {
                isSecond = true;
                isFirst = false;
            }
            else
                isSecond = false;
        }
    }

    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        // Sync entry count
        int entryCount = stream.isWriting ? Entries.Count : 0;
        stream.Serialize(ref entryCount);

        // Tidy up collection size
        if (stream.isReading)
        {
            while (Entries.Count < entryCount) Entries.Add(new LeaderboardEntry());
            while (Entries.Count > entryCount) Entries.RemoveAt(Entries.Count - 1);
        }

        // Sync entries
        foreach (var entry in Entries)
        {
            stream.Serialize(ref entry.NetworkPlayer);
            stream.Serialize(ref entry.Kills);
            stream.Serialize(ref entry.Deaths);
            stream.Serialize(ref entry.Ping);
            stream.Serialize(ref entry.ConsecutiveKills);
        }
    }

    [RPC]
    public void RegisterKill(NetworkPlayer shooter, NetworkPlayer victim)
    {
        if (!Network.isServer) return;

        var scheduledMessage = 0;

        LeaderboardEntry entry;
        if(shooter != victim)
        {
            entry = Entries.FirstOrDefault(x => x.NetworkPlayer == shooter);
            if (entry != null)
            {
                entry.Kills++;
                entry.ConsecutiveKills++;

                if (entry.ConsecutiveKills == 3)
                    scheduledMessage = 1;
                if (entry.ConsecutiveKills == 6)
                    scheduledMessage = 2;
                if (entry.ConsecutiveKills == 9)
                    scheduledMessage = 3;
            }
        }

        entry = Entries.FirstOrDefault(x => x.NetworkPlayer == victim);
        var endedSpree = false;
        if (entry != null)
        {
            entry.Deaths++;
            if (entry.ConsecutiveKills >= 3)
                endedSpree = true;
            entry.ConsecutiveKills = 0;
        }

        if (victim == shooter)
            ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, shooter, "commited suicide", true);
        else
            ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, shooter, "killed " + (endedSpree ? "and stopped " : "") + PlayerRegistry.For[victim].Username.ToUpper(), true);

        if (scheduledMessage == 1)
            ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, shooter, "is threatening!", true);
        if (scheduledMessage == 2)
            ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, shooter, "is dangerous!", true);
        if (scheduledMessage == 3)
            ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, shooter, "is merciless!", true);
    }

    void OnPlayerConnected(NetworkPlayer player)
    {
        Entries.Add(new LeaderboardEntry
        {
            Ping = Network.GetLastPing(player),
            NetworkPlayer = player
        });
    }
    void OnPlayerDisconnected(NetworkPlayer player)
    {
        Entries.RemoveAll(x => x.NetworkPlayer == player);
    }
    void OnDisconnectedFromServer(NetworkDisconnection info) 
    {
        Entries.Clear();
    }
}

class LeaderboardEntry
{
    public NetworkPlayer NetworkPlayer;
    public int Kills;
    public int Deaths;
    public int Ping;
    public int ConsecutiveKills;
}
