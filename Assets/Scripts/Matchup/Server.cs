using System.Collections.Generic;
using System.Linq;
using MasterServer;
using UnityEngine;

public class Server : MonoBehaviour
{
    public GameMode CurrentGameMode { get; private set; }
    public PlayerPresence BasePlayerPresence;
    public PlayerScript PlayerPrefab;

    public List<NetworkPlayer> NetworkPlayers { get; private set; }
    public IEnumerable<PlayerPresence> Presences { get { return PlayerPresence.AllPlayerPresences; }}
    public IEnumerable<PlayerPresence> Combatants { get
    {
        return PlayerPresence.AllPlayerPresences.Where(p => !p.IsSpectating);
    } } 

    public GameMode DefaultGameMode;

    // Will only be available on the server
    public string NetworkGUID { get; set; }

    public ExternalServerNotifier ServerNotifier { get; private set; }

    private bool WasMine = false;

    public Leaderboard Leaderboard { get; private set; }


    // Map name stuff
    private string _CurrentMapName;
    public string CurrentMapName
    {
        get { return _CurrentMapName; }
        private set
        {
            _CurrentMapName = value;
            if (networkView.isMine)
            {
                networkView.RPC("RemoteReceiveMapName", RPCMode.Others, _CurrentMapName);
            }
            if (Application.CanStreamedLevelBeLoaded(_CurrentMapName))
            {
                Application.LoadLevel(_CurrentMapName);
                Network.isMessageQueueRunning = false;
            }
        }
    }

    [RPC]
// ReSharper disable once UnusedMember.Local
    private void RequestedMapNameFromRemote(NetworkMessageInfo info)
    {
        networkView.RPC("RemoteReceiveMapName", info.sender, CurrentMapName);
    }

    [RPC]
// ReSharper disable once UnusedMember.Local
    private void RemoteReceiveMapName(string mapName, NetworkMessageInfo info)
    {
        if (networkView.owner == info.sender)
            CurrentMapName = mapName;
    }

    public void Awake()
    {
        DontDestroyOnLoad(this);
        NetworkPlayers = new List<NetworkPlayer>();

        ServerNotifier = new ExternalServerNotifier();

        Leaderboard = new Leaderboard();
        Leaderboard.Skin = Relay.Instance.BaseSkin;

        if (networkView.isMine)
            _CurrentMapName = "pi_mar";
    }

    public void OnNetworkInstantiate(NetworkMessageInfo info)
    {
    }

    public void Start()
    {
        Relay.Instance.CurrentServer = this;
        PlayerPresence.OnPlayerPresenceAdded += ReceivePlayerPresenceAdd;
        PlayerPresence.OnPlayerPresenceRemoved += ReceivePlayerPresenceRemove;
        SpawnPresence();
        if (networkView.isMine)
        {
            WasMine = true;
            OnPlayerConnected(Network.player);
            CurrentGameMode = (GameMode) Instantiate(DefaultGameMode, Vector3.zero, Quaternion.identity);
            CurrentGameMode.Server = this;
            ServerNotifier.Name = PlayerPrefs.GetString("username", "Anonymous") + "'s server";
        }

        // I guess this check is redundant?
        if (!networkView.isMine)
        {
            networkView.RPC("RequestedMapNameFromRemote", networkView.owner);
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
    }

    private void ReceivePlayerPresenceRemove(PlayerPresence removedPlayerPresence)
    {
        BroadcastMessageFromServer(removedPlayerPresence.Name + " has left");
    }

    private void ReceivePlayerPresenceIdentified(PlayerPresence playerPresence)
    {
        BroadcastMessageFromServer(playerPresence.Name + " has joined");
    }

// ReSharper disable once UnusedMethodReturnValue.Local
    private PlayerPresence SpawnPresence()
    {
        var presence = (PlayerPresence) Network.Instantiate(BasePlayerPresence, Vector3.zero, Quaternion.identity, 0);
        return presence;
    }

    public void Update()
    {
        if (networkView.isMine)
        {
            ServerNotifier.GUID = NetworkGUID;
            ServerNotifier.CurrentMapName = Application.loadedLevelName;
            ServerNotifier.NumberOfPlayers = PlayerPresence.UnsafeAllPlayerPresences.Count;
            ServerNotifier.Update();
        }
        Leaderboard.Update();
    }

    // Only called by Unity on server
    public void OnPlayerConnected(NetworkPlayer player)
    {
        NetworkPlayers.Add(player);
    }

    // Only called by Unity on server
    public void OnPlayerDisconnected(NetworkPlayer player)
    {
        Network.RemoveRPCs(player);
        Network.DestroyPlayerObjects(player);
        NetworkPlayers.Remove(player);
    }

    public void OnLevelWasLoaded(int level)
    {
        Network.isMessageQueueRunning = true;
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
        // TODO not sure what to do about this stuff now
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

        // Will need to not dispose this if we're going to delist our server,
		// since we'll have to hand this off to the Relay for ownership.
        // TODO bit o' the old hack 'ere
        if (WasMine)
        {
            ServerNotifier.BecomeUnlisted();
        }
        //ServerNotifier.Dispose();
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
// ReSharper disable once UnusedMember.Local
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
        if (Application.loadedLevelName != levelName &&
            Application.CanStreamedLevelBeLoaded(levelName))
        {
            ServerNotifier.CurrentMapName = levelName;
            CurrentMapName = levelName;
        }
    }

    // I have no idea if this actually works reliably or not, but it seems to
	// work in my testing. Semi-hack? I guess I could stall the application
	// until we know it's worked, but that's kind of jerky, too.
    public void OnApplicationQuit()
    {
        if (WasMine)
        {
            ServerNotifier.BecomeUnlisted();
        }
    }

    public void OnGUI()
    {
        Leaderboard.DrawGUI();
    }
}