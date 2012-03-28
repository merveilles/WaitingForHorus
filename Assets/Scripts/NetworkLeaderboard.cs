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
            foreach (var entry in Entries)
            {
                entry.Ping = Network.GetLastPing(entry.NetworkPlayer);
                entry.Ratio = (float)entry.Kills / (entry.Deaths == 0 ? 1 : entry.Deaths);
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
        for (int i = 0; i < 100; i++)
        {
            foreach (var entry in Entries)
            {
                stream.Serialize(ref entry.NetworkPlayer);
                stream.Serialize(ref entry.Kills);
                stream.Serialize(ref entry.Deaths);
                stream.Serialize(ref entry.Ping);
                stream.Serialize(ref entry.Ratio);
            }
        }
    }

    [RPC]
    public void RegisterKill(NetworkPlayer shooter, NetworkPlayer victim)
    {
        if (!Network.isServer) return;

        var entry = Entries.FirstOrDefault(x => x.NetworkPlayer == shooter);
        if (entry != null) entry.Kills++;

        entry = Entries.FirstOrDefault(x => x.NetworkPlayer == victim);
        if (entry != null) entry.Deaths++;
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
    public float Ratio;
}
