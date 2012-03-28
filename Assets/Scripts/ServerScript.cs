using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;

public class ServerScript : MonoBehaviour 
{	
	public string IP = "127.0.0.1";
	public const int Port = 10000;
    public NetworkPeerType PeerType;

    public GUISkin Skin;

    IFuture<string> serverIp;
    bool connecting;
    string lastStatus;
	string chosenUsername = "Anon";

    GUIStyle TextStyle;

    void Awake()
    {
        Application.targetFrameRate = 60;

        TextStyle = new GUIStyle { normal = { textColor = new Color(1.0f, 138 / 255f, 0) }, padding = { left = 30, top = 12 } };
    }
	
	void OnGUI() 
    {
        PeerType = Network.peerType;
        if (connecting) PeerType = NetworkPeerType.Connecting;

        GUI.skin = Skin;

        if (PeerType == NetworkPeerType.Connecting || PeerType == NetworkPeerType.Disconnected)
        {
            Screen.showCursor = true;
            GUILayout.Window(0, new Rect(0, Screen.height - 97, 277, 97), Login, string.Empty);
        }
    }

    void OnConnectedToServer()
    {
        connecting = false;
        PeerType = NetworkPeerType.Client;
    }

    void Login(int windowId)
    {
        switch (PeerType)
        {
            //case NetworkPeerType.Client:
            //    connecting = false;
            //    //lastStatus = "Connected to " + IP + ":" + Port + ", ping is " + Network.GetLastPing(Network.player);
            //    if (GUILayout.Button("Disconnect"))
            //    {
            //        Network.Disconnect();
            //        lastStatus = null;
            //    }
            //    break;

            case NetworkPeerType.Disconnected:
            case NetworkPeerType.Connecting:
                GUI.enabled = PeerType != NetworkPeerType.Connecting;

                GUILayout.Space(7);

				GUILayout.BeginHorizontal();
                {
                    chosenUsername = GUILayout.TextField(chosenUsername);
                    GUILayout.Label("USERNAME");
					SendMessage("SetChosenUsername", chosenUsername);
				}
				GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    IP = GUILayout.TextField(IP);
                    GUILayout.Label("SERVER IP");
                }
                GUILayout.EndHorizontal();

                GUILayout.HorizontalSlider(0, 0, 1);

                GUILayout.Space(3);

                GUILayout.BeginHorizontal();
                {
                    GUI.enabled = true;
                    GUILayout.Label(lastStatus, TextStyle);
                    GUI.enabled = PeerType != NetworkPeerType.Connecting;

                    GUILayout.FlexibleSpace();

                    if (Input.GetKey("w") && Input.GetKey("f") &&  Input.GetKey("h"))
                    {
                        if (GUILayout.Button("CREATE"))
                        {
                            var result = Network.InitializeServer(32, Port, false);
                            if (result == NetworkConnectionError.NoError)
                            {
                                //serverIp = ThreadPool.Instance.Evaluate<string>(GetIP);
                            }
                            else
                                lastStatus = "Failed.";
                        }
                    }

                    if (GUILayout.Button("CONNECT"))
                    {
                        lastStatus = "Connecting...";

                        var result = Network.Connect(IP, Port);
                        if (result != NetworkConnectionError.NoError)
                            lastStatus = "Failed.";
                        else
                            connecting = true;
                    }
                }

                GUILayout.EndHorizontal();

                GUI.enabled = true;
                break;
        }
    }

    static string GetIP()
    {
        string strIP;
        using (var wc = new WebClient())
        {
            strIP = wc.DownloadString("http://checkip.dyndns.org");
            strIP = (new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b")).Match(strIP).Value;
        }
        return strIP;
    }

    void OnFailedToConnect(NetworkConnectionError error)
    {
        lastStatus = "Failed.";
        connecting = false;
    }
}
