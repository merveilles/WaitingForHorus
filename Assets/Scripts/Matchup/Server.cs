using System;
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

    private bool _IsGameActive;
    public bool IsGameActive
    {
        get { return _IsGameActive; }
        set
        {
            _IsGameActive = value;
        }
    }

    public delegate void StatusMessageChangedHandler();
    public event StatusMessageChangedHandler OnStatusMessageChange = delegate {};

    public string StatusMessage
    {
        get { return _StatusMessage; }
        set
        {
            if (_StatusMessage != value)
            {
                _StatusMessage = value;
                if (networkView.isMine)
                    UpdateStatusMessage();
                OnStatusMessageChange();
            }
        }
    }

    private GUIStyle BannerStyle;

    private void UpdateStatusMessage()
    {
        networkView.RPC("RemoteReceiveStatusMessage", RPCMode.Others, StatusMessage);
    }

    private void RequestStatusMessageFromRemote()
    {
        networkView.RPC("OwnerReceiveRemoteWantsStatusMessage", networkView.owner);
    }

    [RPC]
// ReSharper disable once UnusedMember.Local
    private void OwnerReceiveRemoteWantsStatusMessage(NetworkMessageInfo info)
    {
        if (networkView.isMine)
        {
            networkView.RPC("RemoteReceiveStatusMessage", info.sender, StatusMessage);
        }
    }

    [RPC]
// ReSharper disable once UnusedMember.Local
    private void RemoteReceiveStatusMessage(string newStatusMessage, NetworkMessageInfo info)
    {
        if (info.sender == networkView.owner)
            StatusMessage = newStatusMessage;
    }


    // Map name stuff
    private string _CurrentMapName;
    private string _StatusMessage;

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

        StatusMessage = "?";

        BannerMessages = new List<BannerMessage>();

        if (networkView.isMine)
        {
            _CurrentMapName = Application.loadedLevelName;
            IsGameActive = false;
        }
    }

    public void OnNetworkInstantiate(NetworkMessageInfo info)
    {
        if (!networkView.isMine)
        {
            RequestStatusMessageFromRemote();
        }
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

            Relay.Instance.OptionsMenu.ListOfMaps = Relay.Instance.ListedMaps.Where(Application.CanStreamedLevelBeLoaded).ToList();
        }

        // I guess this check is redundant?
        if (!networkView.isMine)
        {
            networkView.RPC("RequestedMapNameFromRemote", networkView.owner);
        }

        BannerStyle = Relay.Instance.BaseSkin.customStyles[3];
        Relay.Instance.MessageLog.OnCommandEntered += ReceiveCommandEntered;
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

    public void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        stream.Serialize(ref _IsGameActive);
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

        // Update banner messages
        float yAccum = 0f;
        for (int i = BannerMessages.Count - 1; i >= 0; i--)
        {
            //BannerMessages[i].Index = displayIndex;
            BannerMessages[i].IndexY = yAccum;
            BannerMessages[i].Update();
            if (BannerMessages[i].Active)
                yAccum += BannerMessages[i].CalculatedHeight + 1f;
            if (BannerMessages[i].Finished)
                BannerMessages.RemoveAt(i);
        }
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
        Relay.Instance.MessageLog.OnCommandEntered -= ReceiveCommandEntered;

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

    public static int DefaultMessageType = 0;
    public static int ChatMessageType = 1;
    public static int BannerMessageType = 2;

    public void BroadcastMessageFromServer(string text)
    {
        BroadcastMessageFromServer(text, DefaultMessageType);
    }

    public void BroadcastMessageFromServer(string text, int messageType)
    {
        if (networkView.isMine)
        {
            networkView.RPC("ReceiveServerMessage", RPCMode.All, text, messageType);

            HandleMessage(text, messageType);
            OnReceiveServerMessage(text);
        }
    }

    public void SendMessageFromServer(string text, NetworkPlayer target)
    {
        if (networkView.isMine)
        {
            networkView.RPC("ReceiveServerMessage", target, text, DefaultMessageType);
        }
    }

    private void HandleMessage(string text, int messageType)
    {
        if (messageType == ChatMessageType)
            GlobalSoundsScript.PlayChatMessageSound();
        else if (messageType == BannerMessageType)
        {
            BannerMessages.Add(new BannerMessage(text.ToUpper(), BannerStyle));
        }
    }

    private void ReceiveCommandEntered(string command, string[] args)
    {
        switch (command)
        {
            case "announce":
                if (args.Length < 1) break;
                if (networkView.isMine)
                    BroadcastMessageFromServer(String.Join(" ", args), BannerMessageType);
            break;
        }
    }


    [RPC]
// ReSharper disable once UnusedMember.Local
    void ReceiveServerMessage(string text, int messageType, NetworkMessageInfo info)
    {
        // Only care about messages from server
        if (info.sender != networkView.owner) return;

        HandleMessage(text, messageType);
        OnReceiveServerMessage(text);
    }

    public void BroadcastChatMessageFromServer(string text, PlayerPresence playerPresence)
    {
        bool isHost = playerPresence.networkView.owner == networkView.owner;
        string sigil = isHost ? "+ " : "";
        BroadcastMessageFromServer(sigil + playerPresence.Name + " : " + text, ChatMessageType);
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

        for (int i = 0; i < BannerMessages.Count; i++)
            BannerMessages[i].OnGUI();
    }

    private List<BannerMessage> BannerMessages; 

    private class BannerMessage
    {
        public readonly string Text;
        public readonly GUIStyle Style;

        public float CalculatedHeight;

        public float IndexY;
        private float CurrentY = -100f;

        public float DesiredY
        {
            get { return Age >= Lifetime ? -100f : IndexY; }
        }

        public float Age = 0f;
        public float Lifetime = 4f;

        public int NumberOfLines
        { get { return Text.Split('\n').Length; } }

        public BannerMessage(string text, GUIStyle style)
        {
            Text = text;
            Style = style;
            CalculatedHeight = NumberOfLines * 22 + Style.padding.top + Style.padding.bottom;
        }

        public void Update()
        {
            CurrentY = Mathf.Lerp(CurrentY, DesiredY, 1f - Mathf.Pow(0.00001f, Time.deltaTime));
            Age += Time.deltaTime;
        }

        public bool Finished { get { return Age >= Lifetime && Mathf.Abs(CurrentY - DesiredY) < 0.01f; } }
        public bool Active { get { return Age < Lifetime; } }

        public void OnGUI()
        {
            var r = new Rect(35, 35 + CurrentY, Screen.width - 35 * 2, 100);
            GUILayout.BeginArea(r);
            GUILayout.BeginHorizontal();
            GUILayout.Space(350);
            GUILayout.Label(Text, Style);
            GUILayout.Space(350);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}