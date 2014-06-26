using System.Collections.Generic;
using UnityEngine;

public class Server : MonoBehaviour
{
    public GameMode CurrentGameMode { get; private set; }
    public PlayerPresence BasePlayerPresence;
    public PlayerScript PlayerPrefab;

    public List<NetworkPlayer> NetworkPlayers { get; private set; }
    public IEnumerable<PlayerPresence> Presences { get { return PlayerPresence.AllPlayerPresences; }}

    public GameMode DefaultGameMode;

    public void Awake()
    {
        DontDestroyOnLoad(this);
        NetworkPlayers = new List<NetworkPlayer>();
    }

    public void Start()
    {
        Relay.Instance.CurrentServer = this;
        PlayerPresence.OnPlayerPresenceAdded += ReceivePlayerPresenceAdd;
        SpawnPresence();
        if (networkView.isMine)
        {
            OnPlayerConnected(Network.player);
            CurrentGameMode = (GameMode)Network.Instantiate(DefaultGameMode, Vector3.zero, Quaternion.identity, 0);
            CurrentGameMode.Server = this;
        }
    }

    private void ReceivePlayerPresenceAdd(PlayerPresence newPlayerPresence)
    {
        newPlayerPresence.Server = this;
    }

    private PlayerPresence SpawnPresence()
    {
        var presence = (PlayerPresence) Network.Instantiate(BasePlayerPresence, Vector3.zero, Quaternion.identity, 0);
        return presence;
    }

    public void Update()
    {
    }

    // Only called by Unity on server
    public void OnPlayerConnected(NetworkPlayer player)
    {
        NetworkPlayers.Add(player);
    }

    // Only called by Unity on server
    public void OnPlayerDisconnected(NetworkPlayer player)
    {
        Network.DestroyPlayerObjects(player);
        Network.RemoveRPCs(player);
        NetworkPlayers.Remove(player);
    }

    public void OnLevelWasLoaded(int level)
    {
        if (networkView.isMine)
        {
            CurrentGameMode.ReceiveMapChanged();
        }
    }

    public void OnDestroy()
    {
        if (Relay.Instance.CurrentServer == this)
        {
            Relay.Instance.CurrentServer = null;
        }
    }
}