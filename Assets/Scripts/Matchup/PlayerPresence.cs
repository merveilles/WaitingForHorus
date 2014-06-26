using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPresence : MonoBehaviour
{
    public Server Server { get; set; }
    public bool HasAuthority { get { return networkView.isMine; } }

    public static IEnumerable<PlayerPresence> AllPlayerPresences { get { return UnsafeAllPlayerPresences.ToList(); }}
    public static List<PlayerPresence> UnsafeAllPlayerPresences = new List<PlayerPresence>();

    public delegate void PlayerPresenceExistenceHandler(PlayerPresence newPlayerPresence);
    public static event PlayerPresenceExistenceHandler OnPlayerPresenceAdded = delegate {};
    public static event PlayerPresenceExistenceHandler OnPlayerPresenceRemoved = delegate {};

    public PlayerScript DefaultPlayerCharacterPrefab;

    public NetworkViewID PossessedCharacterViewID;

    public void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        NetworkViewID prevPossesedID = PossessedCharacterViewID;
        stream.Serialize(ref PossessedCharacterViewID);
        if (stream.isReading)
        {
            if (prevPossesedID != PossessedCharacterViewID)
            {
                if (prevPossesedID != NetworkViewID.unassigned)
                {
                    var obj = NetworkView.Find(prevPossesedID);
                    if (obj != null) Destroy(obj);
                }
                PerformSpawnForViewID(PossessedCharacterViewID);
            }
        }
    }

    public void Awake()
    {
        DontDestroyOnLoad(this);
        PossessedCharacterViewID = NetworkViewID.unassigned;
    }

    public void Start()
    {
        UnsafeAllPlayerPresences.Add(this);
        OnPlayerPresenceAdded(this);
    }

    public void OnDestroy()
    {
        OnPlayerPresenceRemoved(this);
    }

    public void SpawnCharacter(Vector3 position)
    {
        if (networkView.isMine)
            ClientSpawnCharacter(position);
        else
            networkView.RPC("ClientSpawnCharacter", networkView.owner, position);
    }

    private PlayerScript PerformSpawnForViewID(NetworkViewID characterViewId)
    {
        var newPlayerCharacter =
            (PlayerScript) Instantiate(DefaultPlayerCharacterPrefab, Vector3.zero, Quaternion.identity);
        newPlayerCharacter.networkView.viewID = characterViewId;
        newPlayerCharacter.Possessor = this;
        return newPlayerCharacter;
    }

    [RPC]
    private void ClientSpawnCharacter(Vector3 spawnPosition)
    {
        var newViewID = Network.AllocateViewID();
        Debug.Log("Will spawn character with view ID: " + newViewID);
        var newCharacter = PerformSpawnForViewID(newViewID);
        newCharacter.transform.position = spawnPosition;
        PossessedCharacterViewID = newViewID;
    }

}