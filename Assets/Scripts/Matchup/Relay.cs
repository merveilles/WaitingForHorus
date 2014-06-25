using UnityEngine;

// A single instance, as part of the startup scene, which is used to communicate
// with the connected server or clients.
public class Relay : MonoBehaviour
{
    public PlayerScript PlayerCharacterPrefab;

    public Server BaseServer;

    public Server CurrentServer { get; private set; }

    public void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public void Start()
    {
        if (Application.isEditor)
        {
            Network.InitializeServer(32, 5556, false);
            CurrentServer = (Server)Network.Instantiate(BaseServer, Vector3.zero, Quaternion.identity, 0 );
            CurrentServer.Relay = this;
        }
    }

    public void Update()
    {

    }

    public bool IsConnected { get { return false; } }


    [RPC]
    public void ClientSpawnCharacter()
    {
        var newPlayerCharacter = (PlayerScript)Network.Instantiate(PlayerCharacterPrefab, RespawnZone.GetRespawnPoint(), Quaternion.identity, 0);
        //var newPlayerCharacter = (PlayerScript)Instantiate(PlayerCharacterPrefab, RespawnZone.GetRespawnPoint(), Quaternion.identity);
        newPlayerCharacter.Relay = this;
    }
}