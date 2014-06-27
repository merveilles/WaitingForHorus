using System;
using System.Text;
using UnityEngine;

// A single instance, as part of the startup scene, which is used to communicate
// with the connected server or clients.
public class Relay : MonoBehaviour
{
    // Global, ewww, but probably the only one we'll need in the end.
    public static Relay Instance { get; private set; }
    public Server BaseServer;

    public GameObject MainCamera;

    private Server _CurrentServer;

    private bool TryingToConnect = false;

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
            TryingToConnect = false;
        }
    }

    public GUISkin BaseSkin;
    public MessageLog MessageLog { get; private set; }

    [Serializable]
    public enum RunMode
    {
        Client, Server
    }

    public const int Port = 31415;
    public string ConnectingServerHostname = "127.0.0.1";

    public void Awake()
    {
        DontDestroyOnLoad(this);
        Instance = this;
        MessageLog = new MessageLog();
        MessageLog.Skin = BaseSkin;

        Network.natFacilitatorIP = "107.170.78.82";
    }

    public void Start()
    {
        Application.LoadLevel("pi_mar");
    }

    public void Connect(RunMode mode)
    {
        switch (mode)
        {
            case RunMode.Client:
                TryingToConnect = true;
                Network.Connect(ConnectingServerHostname, Port);
                MessageLog.AddMessage("Connecting to " + ConnectingServerHostname + ":" + Port);
                break;
            case RunMode.Server:
                TryingToConnect = true;
                Network.InitializeServer(32, Port, true); // true = use nat facilitator
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void OnServerInitialized()
    {
        MessageLog.AddMessage("Started server on port " + Port);
        var server = (Server)Network.Instantiate(BaseServer, Vector3.zero, Quaternion.identity, 0 );
        server.NetworkGUID = Network.player.guid;
        MessageLog.AddMessage("Server GUID: " + server.NetworkGUID);
        // Old method, would still be useful if we ever have multiple servers per Unity process (wha?)
        //CurrentServer = (Server)Network.Instantiate(BaseServer, Vector3.zero, Quaternion.identity, 0 );
        //CurrentServer.Relay = this;
    }

    public void OnFailedToConnect(NetworkConnectionError error)
    {
        MessageLog.AddMessage("Failed to connect: " + error);
        TryingToConnect = false;
    }

    public void OnDisconnectedFromServer(NetworkDisconnection error)
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
                Network.Disconnect();
            }
        }
    }

    public void OnGUI()
    {
        if (ScreenSpaceDebug.Instance.ShouldDraw)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(PlayerScript.UnsafeAllEnabledPlayerScripts.Count + " PlayerScripts");
            sb.AppendLine(PlayerPresence.UnsafeAllPlayerPresences.Count + " PlayerPresences");
            GUI.Label(new Rect(10, 10, 500, 500), sb.ToString());
        }

        MessageLog.OnGUI();

        // Display name setter and other stuff when not connected
        if (CurrentServer == null)
        {
            GUI.skin = BaseSkin;
            GUILayout.Window(0, new Rect( ( Screen.width / 2 ) - 122, Screen.height - 110, 77, 35), DrawLoginWindow, string.Empty);
        }
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
			GUILayout.Box( "", new GUIStyle( BaseSkin.box ) { fixedWidth = 1 } );
            if(GUILayout.Button("HOST"))
            {
                GlobalSoundsScript.PlayButtonPress();
                Connect(RunMode.Server);
            }
			GUILayout.Box( "", new GUIStyle( BaseSkin.box ) { fixedWidth = 1 } );
            if(GUILayout.Button("JOIN"))
            {
                GlobalSoundsScript.PlayButtonPress();
                Connect(RunMode.Client);
            }
            GUI.enabled = true;
        }
        GUILayout.EndHorizontal();
    }

    public bool IsConnected { get { return false; } }

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
}