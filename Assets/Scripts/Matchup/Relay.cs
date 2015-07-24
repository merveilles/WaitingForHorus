using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Network.MasterServer;
using UnityEngine;
using Mono.Nat;

// A single instance, as part of the startup scene, which is used to communicate
// with the connected server or clients.
public class Relay : uLink.MonoBehaviour
{
    // Global, ewww, but probably the only one we'll need in the end.
    public static Relay Instance { get; private set; }
    public Server BaseServer;

    public int CurrentVersionID = 0;

    public GameObject MainCamera;

    public bool DevelopmentMode = false;

    private Server _CurrentServer;

    private bool TryingToConnect = false;

    // Some more un-lovely hacks
    private GUIStyle BoxSpacer;

    private float TimeUntilRefresh = 0f;
    private float TimeBetweenRefreshes = 15f;

    public const int CharacterSpawnGroupID = 1;

    public readonly string[] ListedMaps = new[]
    {
        "pi_mar",
        "pi_set",
        //"pi_ven",
        "pi_rah",
        "pi_bas"
    };

    public OptionsMenu OptionsMenu { get; private set; }
    public bool ShowOptions { get; set; }

    public AnimationCurve MouseSensitivityCurve;

    public int PublicizedVersionID
    {
        get { return DevelopmentMode ? -1 : CurrentVersionID; }
    }

    public static float DesiredTimeBetweenNetworkSends
    {
        // Cheat a little and give some headroom. Unity is sometimes a frame or two late.
        get { return 1f / 58f; }
    }
    public static float SpecifiedTimeBetweenNetworkSends
    {
        get { return 1f / uLink.Network.sendRate; }
    }

    public Server CurrentServer
    {
        get
        {
            return _CurrentServer;
        }
        set
        {
            if (_CurrentServer != null)
            {
                _CurrentServer.OnReceiveServerMessage -= ReceiveServerMessage;
            }
            _CurrentServer = value;
            if (_CurrentServer != null)
            {
                _CurrentServer.OnReceiveServerMessage += ReceiveServerMessage;
            }
            else
            {
                TimeUntilRefresh = 1f;
                // Refresh if we go back to the title screen
            }
            TryingToConnect = false;
        }
    }

    public GUISkin BaseSkin;
    public MessageLog MessageLog { get; private set; }

    private ExternalServerList ExternalServerList;

    public ExternalAddressFinder AddressFinder { get; private set; }

    [Serializable]
    public enum RunMode
    {
        Client, Server
    }

    public const int Port = 31415;
    public string ConnectingServerHostname = "127.0.0.1";

    public Color GoodConnectionColor;
    public Color BadConnectionColor;

    public void Awake()
    {
        DontDestroyOnLoad(this);
        Instance = this;
        MessageLog = new MessageLog();
        MessageLog.Skin = BaseSkin;

        //uLink.Network.natFacilitatorIP = "107.170.78.82";

        ExternalServerList = new ExternalServerList();
        ExternalServerList.OnMasterServerListChanged += ReceiveMasterListChanged;
        ExternalServerList.OnMasterServerListFetchError += ReceiveMasterListFetchError;

        BoxSpacer = new GUIStyle(BaseSkin.box) {fixedWidth = 1};

        OptionsMenu = new OptionsMenu(BaseSkin);

        OptionsMenu.OnOptionsMenuWantsClosed += () =>
        { ShowOptions = false; };
        OptionsMenu.OnOptionsMenuWantsQuitGame += Application.Quit;
        OptionsMenu.OnOptionsMenuWantsGoToTitle += uLink.Network.Disconnect;

        // We want 60fps send rate, but Unity seems retarded and won't actually
		// send at the rate you give it. So we'll just specify it higher and
		// hope to meet the minimum of 60 ticks per second.
        uLink.Network.sendRate = 80;

        // Set up thing that requests to get our WAN IP from some server.
        AddressFinder = new ExternalAddressFinder();
    }

    private string GetRandomMapName()
    {
        List<string> maps = ListedMaps.ToList();
        while (maps.Count > 0)
        {
            int idx = UnityEngine.Random.Range(0, maps.Count);
            string mapName = maps[idx];
            if (Application.CanStreamedLevelBeLoaded(mapName))
                return mapName;
            maps.RemoveAt(idx);
        }
        // Bummer :(
        return "";
    }

    public void Start()
    {
        Application.LoadLevel(GetRandomMapName());

        AddressFinder.FetchExternalAddress();
    }

    public void Connect(RunMode mode)
    {
        switch (mode)
        {
            case RunMode.Client:
                TryingToConnect = true;
                uLink.Network.Connect(ConnectingServerHostname, Port);
                MessageLog.AddMessage("Connecting to " + ConnectingServerHostname + ":" + Port);
                break;
            case RunMode.Server:
                TryingToConnect = true;
                uLink.Network.InitializeServer(32, Port, false); // true = use nat facilitator
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ConnectToExternalListedServer(ServerInfoRaw serverInfo)
    {
        TryingToConnect = true;
        // If it looks like it's coming from local LAN, try to connect to
		// localhost. It seems some routers (like mine) will not respect UPnP
		// port mappings if the origin is from within the local network, which
		// obviously makes playtesting locally kind of hard! Obviously this is
		// not a great solution, since you can't play a local LAN game on a
		// separate machine, but our current setup of using WAN/external IPs
		// only doesn't support that anyway.
        if (serverInfo.address == AddressFinder.ExternalAddress)
        {
            uLink.Network.Connect("127.0.0.1", Port);
            MessageLog.AddMessage("Connecting to 127.0.0.1");
        }
        else
        {
            uLink.Network.Connect( serverInfo.address, Port );
            MessageLog.AddMessage( "Connecting to " + serverInfo.address );
        }
    }
    private void ConnectToRandomServer()
    {
        ServerInfoRaw serverInfo;
        if (ExternalServerList.TryGetRandomServer(out serverInfo))
        {
            ConnectToExternalListedServer(serverInfo);
        }
    }

    public void uLink_OnServerInitialized()
    {
        TryingToConnect = false;
        var sb = new StringBuilder();
        sb.AppendLine("Started server on port " + Port);
        sb.Append("Your address is " + AddressFinder.ExternalAddress);
        MessageLog.AddMessage(sb.ToString());
        // Instantiated server will assign itself as the CurrentServer
        uLink.Network.Instantiate(BaseServer, Vector3.zero, Quaternion.identity, 0 );
    }

    public void uLink_OnFailedToConnect(uLink.NetworkConnectionError error)
    {
        MessageLog.AddMessage("Failed to connect: " + error);
        TryingToConnect = false;
    }

    public void uLink_OnDisconnectedFromServer(uLink.NetworkDisconnection error)
    {
        MessageLog.AddMessage("Disconnected from server: " + error);
        if (CurrentServer != null)
            Destroy(CurrentServer.gameObject);
        TryingToConnect = false;
    }

    public void uLink_OnServerUninitialized(uLink.NetworkDisconnection error)
    {
        MessageLog.AddMessage("Disconnected from server: " + error);
        if (CurrentServer != null)
            Destroy(CurrentServer.gameObject);
        TryingToConnect = false;
    }

    public void Update()
    {
        MessageLog.Update();
        if (CurrentServer != null)
        {
            if (Input.GetKeyDown("f8"))
            {
                uLink.Network.Disconnect();
            }
        }

        // Hide/show options
        if (Input.GetKeyDown(KeyCode.Escape) && !MessageLog.HasInputOpen)
            ShowOptions = !ShowOptions;

        if (CurrentServer == null)
        {
            TimeUntilRefresh -= Time.deltaTime;
            if (TimeUntilRefresh <= 0f)
            {
                TimeUntilRefresh += TimeBetweenRefreshes;
                ExternalServerList.Refresh();
            }

            OptionsMenu.ShouldDisplaySpectateButton = false;
        }

		// TODO this generates a lot of garbage, probably should be removed down the line...
        var sb = new StringBuilder();
        sb.AppendLine(PlayerScript.UnsafeAllEnabledPlayerScripts.Count + " PlayerScripts");
        sb.Append(PlayerPresence.UnsafeAllPlayerPresences.Count + " PlayerPresences");
        ScreenSpaceDebug.AddLineOnce(sb.ToString());

        for (int i = 0; i < PlayerScript.UnsafeAllEnabledPlayerScripts.Count; i++)
        {
            var character = PlayerScript.UnsafeAllEnabledPlayerScripts[i];
            var presenceName = character.Possessor == null ? "null" : character.Possessor.Name;
            ScreenSpaceDebug.AddLineOnce("Character: " + character.name + " possessed by " + presenceName);
        }
        for (int i = 0; i < PlayerPresence.UnsafeAllPlayerPresences.Count; i++)
        {
            var presence = PlayerPresence.UnsafeAllPlayerPresences[i];
            var characterName = presence.Possession == null ? "null" : presence.Possession.name;
            ScreenSpaceDebug.AddLineOnce("Presence: " + presence.Name + " possessing " + characterName);
        }

        OptionsMenu.Update();
        //Cursor.visible = !PlayerScript.lockMouse;
    }

	// FIXME remove dis half-baked abomination
	private void TryNewMasterServer() 
	{
		// set serverlist and addressfinder to the new master server
		string masterServer = "http://" + PlayerPrefs.GetString ("masterserver", "master.server.example");
		// update things that rely on master server
		AddressFinder.URI = masterServer + "/my_address";
		ExternalServerList.URI = masterServer + "/servers/horus";

	}

    private bool ExternalServerListAvailable
    {
        get
        {
            if (ExternalServerList == null) return false;
            if (ExternalServerList.MasterListRaw == null) return false;
            return true;
        }
    }
    private bool DoAnyServersExist
    {
        get
        {
            if (ExternalServerList == null) return false;
            if (ExternalServerList.MasterListRaw == null) return false;
            return ExternalServerList.MasterListRaw.active_servers.Length > 0;
        }
    }

    private int ServerListEntries
    {
        get
        {
            if (DoAnyServersExist)
                return ExternalServerList.MasterListRaw.active_servers.Length;
            else
                return 1;
        }
    }

    private int ServerListHeight
    {
        get
        {
            return ServerListEntries * 36;
        }
    }

    public void OnGUI()
    {
        MessageLog.OnGUI();

        // Display name setter and other stuff when not connected
        if (CurrentServer == null)
        {
            GUI.skin = BaseSkin;
            // TODO less magical layout numerology
            GUILayout.Window(Definitions.LoginWindowID, new Rect( ( Screen.width / 2 ) - 155, Screen.height - 110, 77, 35), DrawLoginWindow, string.Empty);
    	    GUILayout.Window(Definitions.ServerListWindowID, new Rect( ( Screen.width / 2 ) - 155, Screen.height - ServerListHeight - 110, 312, ServerListHeight), DrawServerList, string.Empty);
			//GUILayout.Window(Definitions.MasterServerWindowID, new Rect( ( Screen.width / 2) - 155, Screen.height - 110 + 35, 312, 35), DrawMasterServerInput, string.Empty);
        }

        if (ShowOptions)
        {
            //Screen.lockCursor = false;
        }
        OptionsMenu.DrawGUI();
    }

	// FIXME following method is unused right now, can be potentially nuked
	private void DrawMasterServerInput(int id) 
	{
		GUILayout.BeginHorizontal();
		{
			string currentServerName = PlayerPrefs.GetString ("masterserver", "master.server.com");
			// also, doesn't this allocate new stuff on heap? idk...
			GUIStyle rowStyle = new GUIStyle( BaseSkin.textField ) { fixedWidth = 312 }; // yay for magic numbers
			string newMasterServerName = RemoveSpecialCharacters(GUILayout.TextField(currentServerName, rowStyle));
			if (newMasterServerName != currentServerName) 
			{
				PlayerPrefs.SetString ("masterserver", newMasterServerName);
				PlayerPrefs.Save ();
				TryNewMasterServer();
			}
		}
		GUILayout.EndHorizontal();
	}

    private void DrawLoginWindow(int id)
    {
		GUILayout.BeginHorizontal();
        {
            string currentUserName = PlayerPrefs.GetString("username", "Anonymous");
            string newStartingUserName = RemoveSpecialCharacters(GUILayout.TextField(currentUserName));
            if (newStartingUserName != currentUserName)
            {
                PlayerPrefs.SetString("username", newStartingUserName);
                PlayerPrefs.Save();
            }
            GUI.enabled = !TryingToConnect;
            // TODO shouldn't be allocating here, that's dumb, store it
			GUILayout.Box( "", BoxSpacer );
            if(GUILayout.Button("HOST"))
            {
                GlobalSoundsScript.PlayButtonPress();
                Connect(RunMode.Server);
            }
			GUILayout.Box( "", BoxSpacer );
            if (DevelopmentMode)
            {
                if(GUILayout.Button("LOCAL"))
                {
                    GlobalSoundsScript.PlayButtonPress();
                    Connect(RunMode.Client);
                }
            }
            else
            {
                if(GUILayout.Button("RANDOM"))
                {
                    GlobalSoundsScript.PlayButtonPress();
                    ConnectToRandomServer();
                }
            }

			GUILayout.Box( "", BoxSpacer );
            if(GUILayout.Button("REFRESH"))
            {
                GlobalSoundsScript.PlayButtonPress();
                ExternalServerList.Refresh();
            }

            GUI.enabled = true;
        }
        GUILayout.EndHorizontal();
    }

    private void DrawServerList(int id)
    {
        // TODO this should be in a scrollable view, because it will obviously run offscreen if there are too many
        if (DoAnyServersExist)
        {
            GUIStyle rowStyle = new GUIStyle( BaseSkin.box ) { fixedWidth = 312 - 65 };
            StringBuilder sb = new StringBuilder();
            foreach (var serverInfo in ExternalServerList.MasterListRaw.active_servers)
            {
                sb.Append(serverInfo.name);
                if (serverInfo.VersionMismatch)
                {
                    sb.Append( " |Incompatible Version|" );
                }
                else
                {
                    sb.Append(", ");
                    //sb.Append(serverInfo.);
                    sb.Append(" players on ");
                    sb.Append(serverInfo.map);
                }


                GUILayout.BeginHorizontal();
                //rowStyle.normal.textColor = PlayerRegistry.For(log.Player).Color;
                GUILayout.Box(sb.ToString(), rowStyle);
    			GUILayout.Box( "", new GUIStyle( BaseSkin.box ) { fixedWidth = 1 } );
                GUI.enabled = !TryingToConnect && !serverInfo.VersionMismatch;

                if(GUILayout.Button("JOIN"))
                {
                    GlobalSoundsScript.PlayButtonPress();
                    ConnectToExternalListedServer(serverInfo);
                }
                GUILayout.EndHorizontal();

                // clear
                sb.Length = 0;
            }
        }
        else
        {
            GUIStyle rowStyle = new GUIStyle( BaseSkin.box ) { fixedWidth = 312 };
            GUILayout.BeginHorizontal();
            //rowStyle.normal.textColor = PlayerRegistry.For(log.Player).Color;
            string message;
            if (ExternalServerListAvailable)
            {
                message = "No servers";
            }
            else
            {
                message = "Getting server list...";
            }
            GUILayout.Box(message, rowStyle);
            GUILayout.EndHorizontal();
        }
    }

    public bool IsConnected { get { return uLink.Network.peerType != uLink.NetworkPeerType.Disconnected; } }

    private void ReceiveServerMessage(string text)
    {
        MessageLog.AddMessage(text);
    }

    // Really? Nothing like this exists? hmm
    // Also, can still be 'sploited by unicode shenanigans
    private static string RemoveSpecialCharacters(string str) 
    {
       var sb = new StringBuilder();
       foreach (char c in str)
          if (c != '\n' && c != '\r' && sb.Length < 24)
             sb.Append(c);
       return sb.ToString();
    }

    public void OnDestroy()
    {
        ExternalServerList.OnMasterServerListChanged -= ReceiveMasterListChanged;
        ExternalServerList.OnMasterServerListFetchError -= ReceiveMasterListFetchError;
        ExternalServerList.Dispose();

        AddressFinder.Dispose();
    }

    private void ReceiveMasterListChanged()
    {
        // do something useful?
    }

    private void ReceiveMasterListFetchError(string message)
    {
        MessageLog.AddMessage("Failed to get server list: " + message);
    }
}

public class PortMapper : IDisposable
{
    
    private List<INatDevice> NatDevices = new List<INatDevice>();
    public string DetectedExternalAddress { get; private set; }
    public delegate void DetectedExternalAddressChangedHandler(string newDetectedExternalAddress);
    public event DetectedExternalAddressChangedHandler OnDetectedExternalAddressChanged = delegate {};

    public readonly int Port;

    public PortMapper(int port)
    {
        Port = port;

        // Set up hooks for UPnP
        NatUtility.DeviceFound += DeviceFound;
        NatUtility.DeviceLost += DeviceLost;
        NatUtility.StartDiscovery();
    }

    private void TryRemoveDevice(INatDevice device)
    {
        for (int i = NatDevices.Count - 1; i >= 0; i--)
        {
            if (Equals(NatDevices[i], device))
            {
                if (ShouldMapNatDevices)
                    UnmapDevice(device);
                NatDevices.RemoveAt(i);
            }
        }
    }

    private bool _ShouldMapNatDevices = false;

    public bool ShouldMapNatDevices
    {
        get { return _ShouldMapNatDevices; }
        set
        {
            if (_ShouldMapNatDevices != value)
            {
                if (value)
                {
                    MapAllDevices();
                }
                else
                {
                    UnmapAllDevices();
                }
                _ShouldMapNatDevices = value;
            }
        }
    }

    private void MapAllDevices()
    {
        foreach (var device in NatDevices)
            MapDevice(device);
    }
    private void UnmapAllDevices()
    {
        foreach (var device in NatDevices)
            UnmapDevice(device);
    }

    private void MapDevice(INatDevice device)
    {
        bool exists;
        try
        {
            exists = device.GetSpecificMapping(Protocol.Udp, Port).PublicPort != -1;
        }
        catch (MappingException)
        {
            exists = false;
        }
        if (exists)
        {
            Relay.Instance.MessageLog.AddMessage("Unable to create UPnP port map: port has already been mapped.\nIs a server already running on your network?");
            return;
        }
        device.CreatePortMap(new Mapping(Protocol.Udp, Port, Port));
        Relay.Instance.MessageLog.AddMessage("Created UPnP port mapping.");
    }
    private void UnmapDevice(INatDevice device)
    {
        try
        {
            device.DeletePortMap(new Mapping(Protocol.Udp, Port, Port));
            Relay.Instance.MessageLog.AddMessage("Deleted port mapping.");
        }
        catch (MappingException)
        {
            Relay.Instance.MessageLog.AddMessage("Unable to delete port mapping. That's odd.");
        }
    }

    private void DeviceFound(object sender, DeviceEventArgs args)
    {
        INatDevice device = args.Device;

        NatDevices.Add(device);
        if (ShouldMapNatDevices)
            MapDevice(device);

        DetectedExternalAddress = device.GetExternalIP().ToString();
        OnDetectedExternalAddressChanged(DetectedExternalAddress);
    }
    private void DeviceLost(object sender, DeviceEventArgs args)
    {
        INatDevice device = args.Device;

        TryRemoveDevice(device);
    }

    public void Dispose()
    {
        NatUtility.StopDiscovery();
        NatUtility.DeviceFound -= DeviceFound;
        NatUtility.DeviceLost -= DeviceLost;
        ShouldMapNatDevices = false;
    }
}