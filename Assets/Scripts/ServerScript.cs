using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using JsonFx.Json;
using JsonFx.Serialization;
using JsonFx.Serialization.Resolvers;
using Mono.Nat;
using UnityEngine;

public class ServerScript : MonoBehaviour 
{	
    public static ServerScript Instance { get; private set; }
	
	public const int Port = 31415;
    const int MaxPlayers = 6;
    const string MasterServerUri = "http://api.xxiivv.com/?key=wfh";

    public string[] AllowedLevels = { "pi_rah" }; //"pi_mar", "pi_rah", "pi_gho", "pi_set"

    public NetworkPeerType PeerType;

    public bool LocalMode;

    public GUISkin Skin;
	public int BuildVersion;

    static JsonWriter jsonWriter;
    static JsonReader jsonReader;

    public int connectionFacilitatorPort; 
    public string connectionFacilitatorIPAddress;
	
	public Texture Multiply;

    IFuture<string> wanIp;
    IFuture<ReadResponse> readResponse;
    IFuture<int> thisServerId;
    ServerInfo currentServer;
    bool connecting;
    string lastStatus;
	string chosenUsername = "Anon";
	public string chosenIP = "127.0.0.1";

    enum MappingStatus { InProgress, Success, Failure }
    class MappingResult
    {
        public INatDevice Device;
        public MappingStatus Status = MappingStatus.InProgress;
        public Mapping Mapping;
    }
    List<MappingResult> mappingResults = new List<MappingResult>();

    bool natDiscoveryStarted;
    float sinceRefreshedPlayers;
    int lastPlayerCount;
    bool couldntCreateServer;
    float sinceStartedDiscovery;
    string lastLevelName;
	
	public bool ResumingSavedGame = false;
	public List<LeaderboardEntry> SavedLeaderboardEntries = new List<LeaderboardEntry>();

    public static bool Spectating;

    GUIStyle TextStyle;
    GUIStyle WelcomeStyle;

    static bool isAsyncLoading;
    public static bool IsAsyncLoading
    {
        get { return isAsyncLoading || Application.isLoadingLevel; }
        private set { isAsyncLoading = value; }
    }

    class ReadResponse
    {
        public string Message;
        public int Connections;
        public int Activegames;
        public ServerInfo[] Servers;
    }

    class ServerInfo
    {
        public string Ip;
        public int Players;
        public string Map;
        public int Id;
        public bool ConnectionFailed;
		public int Version;

        public object Packed
        {
            get { return new { id = Id, ip = Ip, players = Players, map = Map, version = Version }; }
        }

        public override string ToString()
        {
            return jsonWriter.Write(this);
        }
    }

    public enum HostingState
    {
        WaitingForInput,
        ReadyToListServers,
        WaitingForServers,
        ReadyToChooseServer,
        ReadyToDiscoverNat,
        ReadyToConnect,
//        ReadyForIp,
        WaitingForNat,
//        WaitingForIp,
        ReadyToHost,
        Hosting,
        Connected
    }
    public static HostingState hostState = HostingState.WaitingForInput;
	
    void Awake()
    {
        Instance = this;
    }
	
    void Start()
    {
        DontDestroyOnLoad(gameObject);

        Network.natFacilitatorPort = connectionFacilitatorPort; Network.natFacilitatorIP = connectionFacilitatorIPAddress;

        chosenUsername = PlayerPrefs.GetString("username", "Anon");

        jsonWriter = new JsonWriter(new DataWriterSettings(new ConventionResolverStrategy(ConventionResolverStrategy.WordCasing.CamelCase)));
        jsonReader = new JsonReader(new DataReaderSettings(new ConventionResolverStrategy(ConventionResolverStrategy.WordCasing.CamelCase)));

        Application.targetFrameRate = -1;
        TextStyle = new GUIStyle { normal = { textColor = new Color(1.0f, 138 / 255f, 0) }, padding = { left = 30, top = 12 } };
        WelcomeStyle = new GUIStyle { normal = { textColor = new Color(1, 1, 1, 1f) } };

        RoundScript.Instance.CurrentLevel = RandomHelper.InEnumerable(AllowedLevels);
        ChangeLevelIfNeeded(RoundScript.Instance.CurrentLevel, true);

        QueryServerList();
    }

    void Update()
    {
        // Automatic host/connect logic follows

        switch( hostState )
        {
            case HostingState.ReadyToListServers:
                lastStatus = "Listing servers...";
                QueryServerList();
                hostState = HostingState.WaitingForServers;
                break;

            case HostingState.WaitingForServers:
                if( !readResponse.HasValue && !readResponse.InError )
                    break;

                var shouldHost = readResponse.Value.Servers.Sum( x => MaxPlayers - x.Players ) < MaxPlayers / 2f;

                /*Debug.Log("Should host? " + shouldHost);

                if (shouldHost && !cantNat && !couldntCreateServer)
                    hostState = HostingState.ReadyToDiscoverNat;
                else*/
                    hostState = HostingState.ReadyToChooseServer;
                break;

            case HostingState.ReadyToChooseServer:
                if (readResponse == null)
                {
                    hostState = HostingState.ReadyToListServers;
                    return;
                }

                currentServer = readResponse.Value.Servers.OrderBy(x => x.Players).ThenBy(x => Guid.NewGuid()).FirstOrDefault(x => !x.ConnectionFailed && x.Players < MaxPlayers && x.Version == BuildVersion ); //&& x.BuildVer == BuildVersion
                if( currentServer == null )
                {
                    //if( couldntCreateServer ) //|| cantNat
                    //{
                        Debug.Log( "Tried to find server, failed. Returning to interactive state." );
                        readResponse = null;
                        lastStatus = "No server found.";
                        hostState = HostingState.WaitingForInput;
                    //}
                   // else
                    //    hostState = HostingState.ReadyToDiscoverNat;
                }
                else
				{	
					chosenIP = currentServer.Ip;
                    hostState = HostingState.ReadyToConnect;
				}
                break;

            case HostingState.ReadyToDiscoverNat:
                lastStatus = "Looking for UPnP...";
                if (!natDiscoveryStarted /*|| !wanIp.HasValue*/)
                {
                    Debug.Log("NAT discovery started");
                    StartNatDiscovery();
//                    GetWanIP();
                }
                hostState = LocalMode ? HostingState.ReadyToHost : HostingState.WaitingForNat;
                break;

            case HostingState.WaitingForNat:
                sinceStartedDiscovery += Time.deltaTime;
                if (sinceStartedDiscovery > 0.5f)
                {
                    NatUtility.StopDiscovery();
                    mappingResults.Clear();
                    sinceStartedDiscovery = 0;

                    if (mappingResults.Any(x => x.Status == MappingStatus.Success))
                        Debug.Log("Some mapping attempts failed, but will proceed with hosting anyway");
                    else
                        Debug.Log("Can't map UPnP ports, but will proceed with hosting anyway");
                    hostState = HostingState.ReadyToHost;
                }

                if (mappingResults.Count == 0 || mappingResults.Any(x => x.Status == MappingStatus.InProgress))
                    break;

                sinceStartedDiscovery = 0;

                if (mappingResults.All(x => x.Status == MappingStatus.Success))
                {
                    Debug.Log("Ready to host!");
                    hostState = HostingState.ReadyToHost;
                }
                else
                {
                    if (mappingResults.Any(x => x.Status == MappingStatus.Success))
                        Debug.Log("Some mapping attempts failed, but will proceed with hosting anyway");
                    else
                        Debug.Log("Can't map UPnP ports, but will proceed with hosting anyway");
                    hostState = HostingState.ReadyToHost;
                }
                break;

//            case HostingState.ReadyForIp:
//                if (wanIp == null || !wanIp.HasValue)
//                    GetWanIP();
//                hostState = HostingState.WaitingForIp;
//                break;
//
//            case HostingState.WaitingForIp:
//                lastStatus = "Determining IP...";
//                if (wanIp.HasValue)
//                    hostState = HostingState.ReadyToHost;
//                break;

            case HostingState.ReadyToHost:
                lastStatus = "Creating server...";
                couldntCreateServer = false;
                if (CreateServer())
                {
                    hostState = HostingState.Hosting;
                    AddServerToList();
                    lastPlayerCount = 0;
                    lastLevelName = RoundScript.Instance.CurrentLevel;
                    sinceRefreshedPlayers = 0;
                }
                else
                {
                    Debug.Log("Couldn't create server, will try joining instead");
                    couldntCreateServer = true;
                    hostState = HostingState.ReadyToChooseServer;
                }
                break;

            case HostingState.Hosting:
                if( !Network.isServer )
                {
                    Debug.Log("Hosting but is not the server...?");
                    break;
                }

                sinceRefreshedPlayers += Time.deltaTime;
                if (thisServerId.HasValue && 
                        (lastPlayerCount != Network.connections.Length ||
                         lastLevelName != RoundScript.Instance.CurrentLevel || 
                         sinceRefreshedPlayers > 55))
                {
                    Debug.Log("Refreshing...");
                    RefreshListedServer();
                    sinceRefreshedPlayers = 0;
                    lastPlayerCount = Network.connections.Length;
                    lastLevelName = RoundScript.Instance.CurrentLevel;
                }
                break;

            case HostingState.ReadyToConnect:
                lastStatus = "Connecting...";
                if( Connect() )
                    hostState = HostingState.Connected;
                else
                {
                    currentServer.ConnectionFailed = true;
                    Debug.Log("Couldn't connect, will try choosing another server");
                    hostState = HostingState.ReadyToChooseServer;
                }
                break;
        }
    }

	void OnGUI() 
    {
        PeerType = Network.peerType;
        if (connecting) PeerType = NetworkPeerType.Connecting;

        GUI.skin = Skin;

        if (PeerType == NetworkPeerType.Connecting || PeerType == NetworkPeerType.Disconnected)
        {
            // Welcome message is now a chat prompt
            if (readResponse != null && readResponse.HasValue)
            {
                var message = "Server activity : " + readResponse.Value.Connections + " players in " + readResponse.Value.Activegames + " games.";
                message = message.ToUpperInvariant();
                
                GUI.Box( new Rect( ( Screen.width / 2 ) - 122, Screen.height - 145, 248, 35), message );
            }

            Screen.showCursor = true;
            GUILayout.Window(0, new Rect( ( Screen.width / 2 ) - 122, Screen.height - 110, 77, 35), Login, string.Empty);
        }
    }

    public static string RemoveSpecialCharacters(string str) 
    {
       var sb = new StringBuilder();
       foreach (char c in str)
          if (c != '\n' && c != '\r' && sb.Length < 24)
             sb.Append(c);
       return sb.ToString();
    }

    void Login(int windowId)
    {
        switch (PeerType)
        {
            case NetworkPeerType.Disconnected:
            case NetworkPeerType.Connecting:
                GUI.enabled = hostState == HostingState.WaitingForInput;
				GUILayout.BeginHorizontal();
                {
                    chosenUsername = RemoveSpecialCharacters(GUILayout.TextField(chosenUsername));
                    PlayerPrefs.SetString("username", chosenUsername.Trim());
					SendMessage("SetChosenUsername", chosenUsername.Trim());
				
                    GUI.enabled = true;
                    GUI.enabled = hostState == HostingState.WaitingForInput && chosenUsername.Trim().Length != 0;
					GUILayout.Box( "", new GUIStyle( Skin.box ) { fixedWidth = 1 } );
                    if( GUILayout.Button("HOST") && hostState == HostingState.WaitingForInput )
                    {
                        PlayerPrefs.Save();
                        GlobalSoundsScript.PlayButtonPress();
                        hostState = HostingState.ReadyToDiscoverNat;
                    }
					GUILayout.Box( "", new GUIStyle( Skin.box ) { fixedWidth = 1 } );
                    if( GUILayout.Button("JOIN") && hostState == HostingState.WaitingForInput )
                    {
                        PlayerPrefs.Save();
                        GlobalSoundsScript.PlayButtonPress();
                        hostState = HostingState.ReadyToListServers;
                        lastStatus = "";
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();

                GUI.enabled = true;
                break;
        }
    }

    void QueryServerList()
    {
        var blackList = new int[0];
        if (readResponse != null && readResponse.HasValue)
        {
            blackList = readResponse.Value.Servers.Where(x => x.ConnectionFailed).Select(x => x.Id).ToArray();
            if (blackList.Length > 0)
                Debug.Log("blacklisted servers : " + blackList.Skip(1).Aggregate(blackList[0].ToString(), (s, i) => s + ", " + i.ToString()));
        }

        readResponse = ThreadPool.Instance.Evaluate(() =>
        {
            using (var client = new WebClient())
            {
                var response = client.DownloadString(MasterServerUri + "&cmd=read");

                try
                {
                    var data = jsonReader.Read<ReadResponse>(response);
                    Debug.Log("MOTD : " + data.Message);
                    Debug.Log(data.Servers.Length + " servers : ");
                    foreach (var s in data.Servers)
                    {
                        s.ConnectionFailed = blackList.Contains(s.Id);
                        Debug.Log(s + (s.ConnectionFailed ? " (blacklisted)" : ""));
                    }
                    return data;
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.ToString());
                    throw ex;
                }
            }
        });
    }

    void AddServerToList()
    {
        thisServerId = ThreadPool.Instance.Evaluate(() =>
        {
            if (LocalMode) return 0;

            using (var client = new WebClient())
            {
                var result = jsonWriter.Write( currentServer.Packed );
                Debug.Log( "server json : " + result );

                // then add new server
                var nvc = new NameValueCollection { { "value", result } };
                var uri = MasterServerUri + "&cmd=add";
                var response = Encoding.ASCII.GetString(client.UploadValues(uri, nvc));
                Debug.Log("Added server, got id = " + response);
                currentServer.Id = int.Parse(response);
                return int.Parse(response);
            }
        });
    }

    void RefreshListedServer()
    {
        currentServer.Players = 1 + Network.connections.Length;
        ThreadPool.Instance.Fire(() =>
        {
            if (LocalMode) return;
            using (var client = new WebClient())
            {
                var result = jsonWriter.Write(currentServer.Packed);

                Debug.Log("server json : " + result);

                // update!
                var nvc = new NameValueCollection { { "value", result } };
                string uri = MasterServerUri + "&cmd=update";
                var response = Encoding.ASCII.GetString(client.UploadValues(uri, nvc));
                Debug.Log(uri);
                Debug.Log("Refreshed server with connection count to " + currentServer.Players + " and map " + currentServer.Map + ", server said : " + response);
            }
        });
    }

    void DeleteServer()
    {
        if (LocalMode) return;
        using (var client = new WebClient())
        {
            var uri = new Uri(MasterServerUri + "&cmd=delete&id=" + thisServerId.Value);
            var nvc = new NameValueCollection { { "", "" } };
            var response = Encoding.ASCII.GetString(client.UploadValues(uri, nvc));
            Debug.Log("Deleted server " + thisServerId.Value + ", server said : " + response);
        }
    }

    bool CreateServer()
    {
        var result = Network.InitializeServer( MaxPlayers, Port, true );
        if (result == NetworkConnectionError.NoError)
        {
            currentServer = new ServerInfo { Ip = Network.player.guid, Map = RoundScript.Instance.CurrentLevel, Players = 1, Version = BuildVersion }; //wanIp.Value

            TaskManager.Instance.WaitUntil(_ => !IsAsyncLoading).Then(() =>
            {
                if (readResponse != null && readResponse.HasValue)
                    ChatScript.Instance.LogChat(Network.player, readResponse.Value.Message, true, true);
            });

            return true;
        }
        lastStatus = "Failed.";
        return false;
    }

    public void ChangeLevel()
    {
        ChangeLevelIfNeeded(RandomHelper.InEnumerable(AllowedLevels.Except(new[] { RoundScript.Instance.CurrentLevel })), false);
    }
    void SyncAndSpawn(string newLevel)
    {
        ChangeLevelIfNeeded(newLevel, false);
        SpawnScript.Instance.WaitAndSpawn();
    }
    public void ChangeLevelIfNeeded(string newLevel)
    {
        ChangeLevelIfNeeded(newLevel, false);
    }
    public void ChangeLevelIfNeeded(string newLevel, bool force)
    {
        if (force)
        {
            Application.LoadLevel(newLevel);
            ChatScript.Instance.LogChat(Network.player, "Changed level to " + newLevel + ".", true, true);
        }
        else if (newLevel != RoundScript.Instance.CurrentLevel)
        {
            IsAsyncLoading = true;
            var asyncOperation = Application.LoadLevelAsync(newLevel);
            TaskManager.Instance.WaitUntil(x => asyncOperation.isDone).Then(() =>
            {
                IsAsyncLoading = false;
                ChatScript.Instance.LogChat(Network.player, "Changed level to " + newLevel + ".", true, true);
            });
        }
        else
            IsAsyncLoading = false;

        RoundScript.Instance.CurrentLevel = newLevel;
        if (currentServer != null) currentServer.Map = RoundScript.Instance.CurrentLevel;
    }

    bool Connect()
    {
        lastStatus = "Connecting...";
        Debug.Log("Connecting to " + chosenIP ); //chosenIP
        var result = Network.Connect( chosenIP );
        if (result != NetworkConnectionError.NoError)
        {
            lastStatus = "Failed.";
            return false;
        }
        connecting = true;
        return true;
    }

    void OnConnectedToServer()
    {
        connecting = false;
        PeerType = NetworkPeerType.Client;
        IsAsyncLoading = true; // Delays spawn until the loading is done server-side

        TaskManager.Instance.WaitUntil(_ => !IsAsyncLoading).Then(() =>
        {
            if (readResponse.HasValue)
                ChatScript.Instance.LogChat(Network.player, readResponse.Value.Message, true, true);
        });
    }

    void OnPlayerConnected(NetworkPlayer player)
    {
        RoundScript.Instance.networkView.RPC("SyncLevel", player, RoundScript.Instance.CurrentLevel);
    }

//    void GetWanIP()
//    {
//        wanIp = ThreadPool.Instance.Evaluate(() =>
//        {
//            if (LocalMode)
//                return "127.0.0.1";
//
//            using (var client = new WebClient())
//            {
//                var response = client.DownloadString("http://checkip.dyndns.org");
//                var ip = (new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b")).Match(response).Value;
//                Debug.Log("Got IP : " + ip);
//                return ip;
//            }
//        });
//    }

    void StartNatDiscovery()
    {
        natDiscoveryStarted = true;

        if (LocalMode) return;

        NatUtility.DeviceFound += (s, ea) =>
        {
            Debug.Log("Mapping port for device : " + ea.Device.ToString());

            mappingResults.AddRange(MapPort(ea.Device));

            // -- This is probably useless
            //string externalIp;
            //try
            //{
            //    externalIp = natDevice.GetExternalIP().ToString();
            //}
            //catch (Exception ex)
            //{
            //    Debug.Log("Failed to get external IP :\n" + ex.ToString());
            //    externalIp = "UNKNOWN";
            //}

            //if (WanIP == "UNKNOWN")
            //{
            //    Debug.Log("Reverted to UPnP device's external IP");
            //    WanIP = externalIp;
            //}
        };
        NatUtility.DeviceLost += (s, ea) => { mappingResults.RemoveAll(x => x.Device == ea.Device); };
        NatUtility.StartDiscovery();
    }

    IEnumerable<MappingResult> MapPort(INatDevice device)
    {
        lastStatus = "Mapping port...";

        var udpMapping = new Mapping(Protocol.Udp, Port, Port) { Description = "Horus (UDP)" };
        var udpResult = new MappingResult { Device = device, Mapping = udpMapping };

        device.BeginCreatePortMap(udpMapping, state =>
        {
            if (state.IsCompleted)
            {
                lastStatus = "Testing UDP mapping...";
                Debug.Log("Mapping complete for : " + udpMapping.ToString());
                try
                {
                    var m = device.GetSpecificMapping(Protocol.Udp, Port);
                    if (m == null)
                        throw new InvalidOperationException("Mapping not found");
                    if (m.PrivatePort != Port || m.PublicPort != Port)
                        throw new InvalidOperationException("Mapping invalid");

                    udpResult.Status = MappingStatus.Success;
                }
                catch (Exception ex)
                {
                    Debug.Log("Failed to validate mapping :\n" + ex.ToString());
                    udpResult.Status = MappingStatus.Failure;
                }
            }
        }, null);

        yield return udpResult;

        var tcpMapping = new Mapping(Protocol.Tcp, Port, Port) { Description = "Horus (TCP)" };
        var tcpResult = new MappingResult { Device = device, Mapping = tcpMapping };

        device.BeginCreatePortMap(tcpMapping, state =>
        {
            if (state.IsCompleted)
            {
                lastStatus = "Testing TCP mapping...";
                Debug.Log("Mapping complete for : " + tcpMapping.ToString());
                try
                {
                    var m = device.GetSpecificMapping(Protocol.Tcp, Port);
                    if (m == null)
                        throw new InvalidOperationException("Mapping not found");
                    if (m.PrivatePort != Port || m.PublicPort != Port)
                        throw new InvalidOperationException("Mapping invalid");

                    tcpResult.Status = MappingStatus.Success;
                }
                catch (Exception ex)
                {
                    Debug.Log("Failed to validate mapping :\n" + ex.ToString());
                    tcpResult.Status = MappingStatus.Failure;
                }
            }
        }, null);

        yield return tcpResult;
    }
	
	void OnServerInitialized()
	{
		Debug.Log("==> GUID is " + Network.player.guid + ". Use this on clients to connect with NAT punchthrough.");
		Debug.Log("==> Local IP/port is " + Network.player.ipAddress + "/" + Network.player.port + ". Use this on clients to connect directly.");
	}

    void OnApplicationQuit()
    {
        foreach (var mr in mappingResults)
        {
            if (mr.Device != null && mr.Mapping != null)
                try
                {
                    mr.Device.DeletePortMap(mr.Mapping);
                    Debug.Log("Deleted port mapping : " + mr.Mapping);
                }
                catch (Exception)
                {
                    if (mr.Status == MappingStatus.Failure)
                        Debug.Log("Tried to delete invalid port mapping and failed -- that's probably fine");
                    else 
                        Debug.Log("Failed to delete port mapping : " + mr.Mapping);
                }
        }
        mappingResults.Clear();

        if (natDiscoveryStarted)
            NatUtility.StopDiscovery();

        Network.Disconnect();

        natDiscoveryStarted = false;
    }

    void OnFailedToConnect(NetworkConnectionError error)
    {
        if (error == NetworkConnectionError.TooManyConnectedPlayers)
            lastStatus = "Server full.";
        else
            lastStatus = "Failed.";

        currentServer.ConnectionFailed = true;
        Debug.Log("Couldn't connect, will try choosing another server");
        hostState = HostingState.ReadyToListServers;

        connecting = false;
    }

    void OnDisconnectedFromServer(NetworkDisconnection info)
    {
       	hostState = HostingState.WaitingForInput;
       	lastStatus = "";
		
        /*if( Network.isServer )
        {
            if( thisServerId.HasValue )
                DeleteServer();
        } 
		else // If I'm not hosting and have lowest guid, then host!
		{
			ResumingSavedGame = true;
			SavedLeaderboardEntries = NetworkLeaderboard.Instance.Entries; // Save Leaderboard
			
			string lowestGUID = PlayerRegistry.GetLowestGUID();
			if( lowestGUID == networkView.owner.guid )
				hostState = HostingState.ReadyToHost;
			else if( lowestGUID != "" )
			{
				chosenIP = lowestGUID;
				StartCoroutine( "DelayedJoin" ); // Give the server a second to register
			}
		}
		
		PlayerRegistry.Instance.Clear(); // Clear registrys now we're finished
		NetworkLeaderboard.Instance.Clear(); // Clear registry now we're finished*/
    }
	
	IEnumerator DelayedJoin()
	{
		yield return new WaitForSeconds( 1.0f );
		
		hostState = HostingState.ReadyToConnect;
	}
}