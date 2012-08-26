using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Mono.Nat;
using UnityEngine;

public class ServerScript : MonoBehaviour 
{	
	public const int Port = 12345;
    const int MaxPlayers = 6;
    public NetworkPeerType PeerType;

    public bool LocalMode;

    public GUISkin Skin;

    IFuture<string> wanIp;
    IFuture<ServerInfo[]> serverList;
    IFuture<int> thisServerId;
    ServerInfo chosenServer;
    bool connecting;
    string lastStatus;
	string chosenUsername = "Anon";

    INatDevice natDevice;
    Mapping udpMapping, tcpMapping;
    bool? udpMappingSuccess, tcpMappingSuccess;
    bool natDiscoveryStarted;
    float sinceRefreshedPlayers;
    int lastPlayerCount;
    bool couldntCreateServer;
    float sinceStartedDiscovery;
    bool cantNat;
    string levelName;

    GUIStyle TextStyle;

    class ServerInfo
    {
        public int Id;
        public string Ip;
        public int PlayerCount;
        public DateTime Timestamp;
        public bool ConnectionFailed;
        public string LevelName;
    }

    public enum HostingState
    {
        WaitingForInput,
        ReadyToListServers,
        WaitingForServers,
        ReadyToChooseServer,
        ReadyToDiscoverNat,
        ReadyToConnect,
        WaitingForNat,
        ReadyToHost,
        Hosting,
        Connected
    }
    public static HostingState hostState = HostingState.WaitingForInput;

    void Start()
    {
        DontDestroyOnLoad(gameObject);

        Application.targetFrameRate = 60;
        TextStyle = new GUIStyle { normal = { textColor = new Color(1.0f, 138 / 255f, 0) }, padding = { left = 30, top = 12 } };

        levelName = RandomHelper.Probability(0.5) ? "rah" : "mar";
        ChangeLevelIfNeeded(levelName, true);
    }

    void Update()
    {
        // Automatic host/connect logic follows

        switch (hostState)
        {
            case HostingState.ReadyToListServers:
                lastStatus = "Listing servers...";
                QueryServerList();
                hostState = HostingState.WaitingForServers;
                break;

            case HostingState.WaitingForServers:
                if (!serverList.HasValue)
                    break;

                var shouldHost = serverList.Value.Sum(x => MaxPlayers - x.PlayerCount) < MaxPlayers / 2f;

                Debug.Log("Should host? " + shouldHost);

                if (shouldHost && !cantNat && !couldntCreateServer)
                    hostState = HostingState.ReadyToDiscoverNat;
                else
                    hostState = HostingState.ReadyToChooseServer;
                break;

            case HostingState.ReadyToChooseServer:
                chosenServer = serverList.Value.OrderBy(x => x.PlayerCount).ThenBy(x => Guid.NewGuid()).FirstOrDefault(x => !x.ConnectionFailed && x.PlayerCount < MaxPlayers);
                if (chosenServer == null)
                {
                    if (couldntCreateServer || cantNat)
                    {
                        Debug.Log("Tried to host, failed, tried to find server, failed. Returning to interactive state.");
                        serverList = null;
                        lastStatus = "No server found.";
                        hostState = HostingState.WaitingForInput;
                    }
                    else
                        hostState = HostingState.ReadyToDiscoverNat;
                }
                else
                    hostState = HostingState.ReadyToConnect;
                break;

            case HostingState.ReadyToDiscoverNat:
                lastStatus = "Trying to open port...";
                if (!natDiscoveryStarted || !wanIp.HasValue)
                {
                    Debug.Log("NAT discovery started");
                    StartNatDiscovery();
                    GetWanIP();
                }
                hostState = LocalMode ? HostingState.ReadyToHost : HostingState.WaitingForNat;
                break;

            case HostingState.WaitingForNat:
                sinceStartedDiscovery += Time.deltaTime;
                if (sinceStartedDiscovery > 10)
                {
                    NatUtility.StopDiscovery();
                    natDevice = null;
                    sinceStartedDiscovery = 0;
                    cantNat = true;

                    Debug.Log("No uPnP despite needing to host, will try to choose server");
                    hostState = HostingState.ReadyToChooseServer;
                }

                if (!udpMappingSuccess.HasValue || !tcpMappingSuccess.HasValue || !wanIp.HasValue)
                    break;

                sinceStartedDiscovery = 0;

                if (udpMappingSuccess.Value && tcpMappingSuccess.Value)
                {
                    Debug.Log("Ready to host!");
                    hostState = HostingState.ReadyToHost;
                }
                else
                {
                    NatUtility.StopDiscovery();
                    natDevice = null;

                    Debug.Log("No uPnP despite needing to host, will re-list servers");
                    hostState = HostingState.ReadyToListServers;
                    cantNat = true;
                }
                break;

            case HostingState.ReadyToHost:
                lastStatus = "Creating server...";
                couldntCreateServer = false;
                if (CreateServer())
                {
                    hostState = HostingState.Hosting;
                    AddServerToList();
                    lastPlayerCount = 0;
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
                if (!Network.isServer)
                {
                    Debug.Log("Hosting but is not the server...?");
                    break;
                }

                sinceRefreshedPlayers += Time.deltaTime;
                if (thisServerId.HasValue && (lastPlayerCount != Network.connections.Length || sinceRefreshedPlayers > 25))
                {
                    Debug.Log("Refreshing...");
                    RefreshListedServer();
                    sinceRefreshedPlayers = 0;
                    lastPlayerCount = Network.connections.Length;
                }
                break;

            case HostingState.ReadyToConnect:
                lastStatus = "Connecting...";
                if (Connect())
                    hostState = HostingState.Connected;
                else
                {
                    chosenServer.ConnectionFailed = true;
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
            Screen.showCursor = true;
            GUILayout.Window(0, new Rect(0, Screen.height - 70, 277, 70), Login, string.Empty);
        }
    }

    void Login(int windowId)
    {
        switch (PeerType)
        {
            case NetworkPeerType.Disconnected:
            case NetworkPeerType.Connecting:
                GUI.enabled = hostState == HostingState.WaitingForInput;

                GUILayout.Space(7);

				GUILayout.BeginHorizontal();
                {
                    chosenUsername = GUILayout.TextField(chosenUsername);
                    GUILayout.Label("USERNAME");
					SendMessage("SetChosenUsername", chosenUsername);
				}
				GUILayout.EndHorizontal();

                GUILayout.HorizontalSlider(0, 0, 1);

                GUILayout.Space(3);

                GUILayout.BeginHorizontal();
                {
                    GUI.enabled = true;
                    TextStyle.padding.left = 5;
                    TextStyle.margin.left = 5;
                    GUILayout.Label(lastStatus, TextStyle);
                    GUI.enabled = hostState == HostingState.WaitingForInput;

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("PRACTICE") && hostState == HostingState.WaitingForInput)
                    {
                        GlobalSoundsScript.PlayButtonPress();
                        hostState = HostingState.ReadyToHost;
                    }
                    if (GUILayout.Button("QUICKPLAY") && hostState == HostingState.WaitingForInput)
                    {
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
        if (serverList != null && serverList.HasValue)
        {
            blackList = serverList.Value.Where(x => x.ConnectionFailed).Select(x => x.Id).ToArray();
            Debug.Log("blacklisted servers : " + blackList);
        }

        serverList = ThreadPool.Instance.Evaluate(() =>
        {
            using (var client = new WebClient())
            {
                var response = client.DownloadString("http://api.xxiivv.com/?key=7377&cmd=list");
                Debug.Log("Got server list : ");
                try
                {
                    var list = response.Split('\n').Where(x => x.Trim().Length > 0 && x.Trim().Split('_').Length == 5).Select(x =>
                    {
                        var y = x.Trim().Split('_');
                        Debug.Log("Parts : '" + y[0] + "' '" + y[1] + "' '" + y[2] + "' '" + y[3] + "' '" + y[4]);
                        var id = int.Parse(y[0]);
                        return new ServerInfo
                        {
                            Id = id,
                            Ip = y[1],
                            PlayerCount = int.Parse(y[2]),
                            Timestamp = DateTime.FromFileTimeUtc(long.Parse(y[3])),
                            LevelName = y[4],
                            ConnectionFailed = blackList.Contains(id)
                        };
                    });

                    foreach (var s in list)
                        Debug.Log(s.Id + " is " + (DateTime.UtcNow - s.Timestamp).TotalSeconds + " seconds old");

                    return list.Where(x => (DateTime.UtcNow - x.Timestamp).TotalSeconds < 30).ToArray();
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.ToString());
                    return new ServerInfo[0];
                }
            }
        });
    }

    void AddServerToList()
    {
        thisServerId = ThreadPool.Instance.Evaluate(() =>
        {
            using (var client = new WebClient())
            {
                var response = client.DownloadString("http://api.xxiivv.com/?key=7377&cmd=add&value=" + wanIp.Value + "_1_" + DateTime.UtcNow.ToFileTimeUtc() + "_" + levelName);
                Debug.Log("Added server, got id = " + response);
                return int.Parse(response);
            }
        });
    }

    void RefreshListedServer()
    {
        var connections = Network.connections.Length;
        ThreadPool.Instance.Fire(() =>
        {
            string uri = "http://api.xxiivv.com/?key=7377&cmd=update&id=" + thisServerId.Value + "&value=" +
                         wanIp.Value + "_" + (connections + 1) + "_" + DateTime.UtcNow.ToFileTimeUtc() + "_" + levelName;
            using (var client = new WebClient())
            {
                client.DownloadString(uri);
                Debug.Log("Updated timestamp to " + DateTime.UtcNow.ToFileTimeUtc() + " and connection count to " +
                            (connections + 1));
            }
        });
    }

    void DeleteServer()
    {
        ThreadPool.Instance.Fire(() =>
        {
            using (var client = new WebClient())
            {
                client.DownloadString("http://api.xxiivv.com/?key=7377&cmd=delete&id=" + thisServerId.Value);
                Debug.Log("Deleted server " + thisServerId.Value);
            }
        });
    }

    bool CreateServer()
    {
        var result = Network.InitializeServer(MaxPlayers, Port, false);
        if (result == NetworkConnectionError.NoError)
        {
            //serverIp = ThreadPool.Instance.Evaluate<string>(GetIP);
            return true;
        }
        lastStatus = "Failed.";
        return false;
    }

    public void ChangeLevel()
    {
        ChangeLevelIfNeeded(levelName == "mar" ? "rah" : "mar", true);
        if (Network.isServer)
            RefreshListedServer();
    }

    void ChangeLevelIfNeeded(string newLevel, bool force)
    {
        if (force || levelName != newLevel)
        {
            Application.LoadLevel("pi_" + newLevel);
            levelName = newLevel;
        }
    }

    bool Connect()
    {
        lastStatus = "Connecting...";
        Debug.Log("Connecting to " + chosenServer.Ip + " (id = " + chosenServer.Id + ")");
        var result = Network.Connect(chosenServer.Ip, Port);
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
        ChangeLevelIfNeeded(chosenServer.LevelName, false);
    }

    void GetWanIP()
    {
        wanIp = ThreadPool.Instance.Evaluate(() =>
        {
            if (LocalMode)
                return "127.0.0.1";

            using (var client = new WebClient())
            {
                var response = client.DownloadString("http://checkip.dyndns.org");
                var ip = (new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b")).Match(response).Value;
                Debug.Log("Got IP : " + ip);
                return ip;
            }
        });
    }

    void StartNatDiscovery()
    {
        natDiscoveryStarted = true;

        if (LocalMode) return;

        NatUtility.DeviceFound += (s, ea) =>
        {
            natDevice = ea.Device;
            MapPort();

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
        NatUtility.DeviceLost += (s, ea) => { natDevice = null; };
        NatUtility.StartDiscovery();
    }

    void MapPort()
    {
        try
        {
            Debug.Log("Mapping port...");

            udpMapping = new Mapping(Protocol.Udp, Port, Port) { Description = "Horus (UDP)" };
            natDevice.BeginCreatePortMap(udpMapping, state =>
            {
                if (state.IsCompleted)
                {
                    Debug.Log("UDP Mapping complete!");
                    try
                    {
                        var m = natDevice.GetSpecificMapping(Protocol.Udp, Port);
                        if (m == null)
                            throw new InvalidOperationException("Mapping not found");
                        if (m.PrivatePort != Port || m.PublicPort != Port)
                            throw new InvalidOperationException("Mapping invalid");

                        Debug.Log("Success!");
                        udpMappingSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("Failed to validate UDP mapping :\n" + ex.ToString());
                        udpMappingSuccess = false;
                    }
                    //udpMappingSuccess = true;
                }
            }, null);

            tcpMapping = new Mapping(Protocol.Tcp, Port, Port) { Description = "Horus (TCP)" };
            natDevice.BeginCreatePortMap(tcpMapping, state =>
            {
                if (state.IsCompleted)
                {
                    Debug.Log("TCP Mapping complete!");
                    try
                    {
                        var m = natDevice.GetSpecificMapping(Protocol.Tcp, Port);
                        if (m == null)
                            throw new InvalidOperationException("Mapping not found");
                        if (m.PrivatePort != Port || m.PublicPort != Port)
                            throw new InvalidOperationException("Mapping invalid");

                        Debug.Log("Success!");
                        tcpMappingSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("Failed to validate TCP mapping :\n" + ex.ToString());
                        tcpMappingSuccess = false;
                    }
                    //tcpMappingSuccess = true;
                }
            }, null);
        }
        catch (Exception ex)
        {
            Debug.Log("Failed to map port :\n" + ex.ToString());
            tcpMappingSuccess = false;
            udpMappingSuccess = false;
        }
    }

    void OnApplicationQuit()
    {
        if (natDevice != null)
        {
            try
            {
                if (udpMapping != null)
                    natDevice.DeletePortMap(udpMapping);
                if (tcpMapping != null)
                    natDevice.DeletePortMap(tcpMapping);
                tcpMapping = udpMapping = null;
                Debug.Log("Deleted port mapping");
            }
            catch (Exception)
            {
                Debug.Log("Failed to delete port mapping");
            }
        }
        if (natDiscoveryStarted)
            NatUtility.StopDiscovery();

        Network.Disconnect();

        natDiscoveryStarted = false;
        natDevice = null;
        tcpMappingSuccess = udpMappingSuccess = null;
    }

    void OnFailedToConnect(NetworkConnectionError error)
    {
        if (error == NetworkConnectionError.TooManyConnectedPlayers)
            lastStatus = "Server full.";
        else
            lastStatus = "Failed.";

        chosenServer.ConnectionFailed = true;
        Debug.Log("Couldn't connect, will try choosing another server");
        hostState = HostingState.ReadyToListServers;

        connecting = false;
    }

    void OnDisconnectedFromServer(NetworkDisconnection info)
    {
        if (Network.isServer)
        {
            if (thisServerId.HasValue)
                DeleteServer();
        }
        hostState = HostingState.WaitingForInput;
        lastStatus = "";
    }
}
