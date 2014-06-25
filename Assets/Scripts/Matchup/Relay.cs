using UnityEngine;

// A single instance, as part of the startup scene, which is used to communicate
// with the connected server or clients.
public class Relay : MonoBehaviour
{
    public PlayerScript PlayerCharacterPrefab;

    public void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public void Start()
    {
        Application.LoadLevel("pi_mar");
    }

    public void Update()
    {

    }

    public bool IsConnected { get { return false; } }

    public void OnLevelWasLoaded(int levelIndex)
    {
        var localPlayerPresence = new LocalPlayerPresence(this);
        var newPlayerCharacter = (PlayerScript)Instantiate(PlayerCharacterPrefab, RespawnZone.GetRespawnPoint(), Quaternion.identity);
        newPlayerCharacter.Possessor = localPlayerPresence;
        //var newPlayerCharacter = (PlayerScript)Network.Instantiate(PlayerCharacterPrefab, RespawnZone.GetRespawnPoint(), Quaternion.identity, 0);
    }
}
