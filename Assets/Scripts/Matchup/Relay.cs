using System;
using UnityEngine;

// A single instance, as part of the startup scene, which is used to communicate
// with the connected server or clients.
public class Relay : MonoBehaviour
{
    // Global, ewww, but probably the only one we'll need in the end.
    public static Relay Instance { get; private set; }
    public Server BaseServer;

    public Server CurrentServer { get; set; }

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
    }

    public void Connect(RunMode mode)
    {
        switch (mode)
        {
            case RunMode.Client:
                Network.Connect(ConnectingServerHostname, Port);
                break;
            case RunMode.Server:
                Network.InitializeServer(32, Port, false);
                Network.Instantiate(BaseServer, Vector3.zero, Quaternion.identity, 0 );
                //CurrentServer = (Server)Network.Instantiate(BaseServer, Vector3.zero, Quaternion.identity, 0 );
                //CurrentServer.Relay = this;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Update()
    {
        if (CurrentServer == null)
        {
            if (Input.GetKeyDown("s"))
            {
                Connect(RunMode.Server);
            }
            else if (Input.GetKeyDown("c"))
            {
                Connect(RunMode.Client);
            }
        }
    }


    public bool IsConnected { get { return false; } }
}