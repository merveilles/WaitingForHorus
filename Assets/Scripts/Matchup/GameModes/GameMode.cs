using UnityEngine;


// GameModes are only spawned on the server, and are not replicated. Use the
// Server for communication to players.
public class GameMode : MonoBehaviour
{
    //public delegate void GameModeCompleteHandler();
    //public event GameModeCompleteHandler OnGameModeComplete = delegate { };

    public virtual void Awake()
    {
        DontDestroyOnLoad(this);
    }
    public virtual void Start() { }

    public virtual void Update() { }
    public Server Server { get; set; }

    public virtual void ReceiveMapChanged() {}

    public virtual void OnNewConnection(uLink.NetworkPlayer newPlayer) { }
}