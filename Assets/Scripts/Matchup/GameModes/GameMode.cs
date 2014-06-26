using UnityEngine;


public class GameMode : MonoBehaviour
{
    //public delegate void GameModeCompleteHandler();
    //public event GameModeCompleteHandler OnGameModeComplete = delegate { };

    public virtual void Awake()
    {
        DontDestroyOnLoad(this);
    }
    public virtual void Start() { }
    public Server Server { get; set; }

    public virtual void ReceiveMapChanged() {}

    public virtual void OnNewConnection(NetworkPlayer newPlayer) { }
}