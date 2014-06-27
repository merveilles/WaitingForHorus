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
        PlayerPresence.OnPlayerPresenceRemoved += ReceivePlayerPresenceRemove;
        SpawnPresence();
        if (networkView.isMine)
        {
            OnPlayerConnected(Network.player);
            CurrentGameMode = (GameMode) Instantiate(DefaultGameMode, Vector3.zero, Quaternion.identity);
            CurrentGameMode.Server = this;
        }
    }

    private void ReceivePlayerPresenceAdd(PlayerPresence newPlayerPresence)
    {
        newPlayerPresence.Server = this;
        if (newPlayerPresence.HasBeenNamed)
            ReceivePlayerPresenceIdentified(newPlayerPresence);
        else
            newPlayerPresence.OnBecameNamed += () =>
                ReceivePlayerPresenceIdentified(newPlayerPresence);
        //BroadcastMessageFromServer(newPlayerPresence.Name + " has joined");
    }

    private void ReceivePlayerPresenceRemove(PlayerPresence removedPlayerPresence)
    {
        BroadcastMessageFromServer(removedPlayerPresence.Name + " has left");
    }

    private void ReceivePlayerPresenceIdentified(PlayerPresence playerPresence)
    {
        BroadcastMessageFromServer(playerPresence.Name + " has joined");
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
        PlayerPresence.OnPlayerPresenceAdded -= ReceivePlayerPresenceAdd;
        PlayerPresence.OnPlayerPresenceRemoved -= ReceivePlayerPresenceRemove;
        if (CurrentGameMode != null)
        {
            Destroy(CurrentGameMode.gameObject);
        }
        foreach (var playerPresence in PlayerPresence.AllPlayerPresences)
        {
            Destroy(playerPresence.gameObject);
        }
        foreach (var playerScript in PlayerScript.AllEnabledPlayerScripts)
        {
            Destroy(playerScript.gameObject);
        }
        if (Relay.Instance.CurrentServer == this)
        {
            Relay.Instance.CurrentServer = null;
        }

        // FIXME dirty hack
        {
            Screen.lockCursor = false;
            Screen.showCursor = true;
            if (Relay.Instance.MainCamera != null)
            {
                var indicator = Relay.Instance.MainCamera.GetComponent<WeaponIndicatorScript>();
                if (indicator != null)
                    indicator.enabled = false;
            }
        }
    }

    public delegate void ReceiveServerMessageHandler(string text);

    public event ReceiveServerMessageHandler OnReceiveServerMessage = delegate {};

    public void BroadcastMessageFromServer(string text)
    {
        if (networkView.isMine)
        {
            networkView.RPC("ReceiveServerMessage", RPCMode.All, text);
            OnReceiveServerMessage(text);
        }
    }

    public void SendMessageFromServer(string text, NetworkPlayer target)
    {
        if (networkView.isMine)
        {
            networkView.RPC("ReceiveServerMessage", target, text);
        }
    }

    [RPC]
    void ReceiveServerMessage(string text, NetworkMessageInfo info)
    {
        // Only care about messages from server
        if (info.sender != networkView.owner) return;
        OnReceiveServerMessage(text);
    }

    public void BroadcastChatMessageFromServer(string text, PlayerPresence playerPresence)
    {
        bool isHost = playerPresence.networkView.owner == networkView.owner;
        string sigil = isHost ? "+ " : "";
        BroadcastMessageFromServer(sigil + playerPresence.Name + " : " + text);
    }

    public void ChangeLevel(string levelName)
    {
        if (Application.loadedLevelName != levelName)
            Application.LoadLevel("pi_mar");
    }
    [RPC]
    private void RemoteReceiveLevelChange(string levelName, NetworkMessageInfo info)
    {
        if (info.sender == networkView.owner)
            ChangeLevel(levelName);
    }
}