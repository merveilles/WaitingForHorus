using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Com.EpixCode.Util.WeakReference.WeakDictionary;
using UnityEngine;

public class PlayerPresence : MonoBehaviour
{
    public Server Server { get; set; }

    private string _Name = "Nameless";
    public string Name {
        get
        {
            return _Name;
        }
        private set
        {
            ReceiveNameSent(value);
            if (networkView.isMine)
            {
                if (Relay.Instance.IsConnected)
                {
                    networkView.RPC("ReceiveNameSent", RPCMode.Others, value);
                }
            }
        }
    }

    public static IEnumerable<PlayerPresence> AllPlayerPresences { get { return UnsafeAllPlayerPresences.ToList(); }}
    public static List<PlayerPresence> UnsafeAllPlayerPresences = new List<PlayerPresence>();

    public delegate void PlayerPresenceExistenceHandler(PlayerPresence newPlayerPresence);
    public static event PlayerPresenceExistenceHandler OnPlayerPresenceAdded = delegate {};
    public static event PlayerPresenceExistenceHandler OnPlayerPresenceRemoved = delegate {};

    public PlayerScript DefaultPlayerCharacterPrefab;

    public NetworkViewID PossessedCharacterViewID;

    private PlayerScript _Possession;

    public PlayerScript Possession
    {
        get
        {
            return _Possession;
        }
        set
        {
            _Possession = value;
            if (_Possession != null)
            {
                _Possession.CameraScript.IsExteriorView = WantsExteriorView;
            }
        }
    }

    public delegate void PlayerPresenceWantsRespawnHandler();
    public event PlayerPresenceWantsRespawnHandler OnPlayerPresenceWantsRespawn = delegate {};

    private WeakDictionary<PlayerScript, Vector2> LastGUIDebugPositions;

    public bool HasBeenNamed { get { return Name != "Nameless"; } }
    public delegate void NameChangedHandler();

    public event NameChangedHandler OnNameChanged = delegate {}; 
    public event NameChangedHandler OnBecameNamed = delegate {};

    private bool wasMine = false;

    private bool _WantsExteriorView;

    public bool WantsExteriorView
    {
        get
        {
            return _WantsExteriorView;
        }
        set
        {
            _WantsExteriorView = value;
            if (networkView.isMine)
            {
                int asNumber = _WantsExteriorView ? 1 : 0;
                PlayerPrefs.SetInt("thirdperson", asNumber);
            }
        }
    }

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
                    NetworkView view = null;
                    try
                    {
                        view = NetworkView.Find(prevPossesedID);
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e);
                    }
                    if (view != null) Destroy(view.gameObject);
                }
                if (PossessedCharacterViewID != NetworkViewID.unassigned)
                    PerformSpawnForViewID(PossessedCharacterViewID);
            }
        }
    }

    public void Awake()
    {
        DontDestroyOnLoad(this);
        PossessedCharacterViewID = NetworkViewID.unassigned;
        LastGUIDebugPositions = new WeakDictionary<PlayerScript, Vector2>();

        if (networkView.isMine)
        {
            _WantsExteriorView = PlayerPrefs.GetInt("thirdperson", 1) > 0;
        }

        // Ladies and gentlemen, the great and powerful Unity
        wasMine = networkView.isMine;

        if (networkView.isMine)
        {
            Name = PlayerPrefs.GetString("username", "Anonymous");

            // TODO will obviously send messages to server twice if there are two local players, fix
            Relay.Instance.MessageLog.OnMessageEntered += ReceiveMessageEntered;
        }
    }

    private void ReceiveMessageEntered(string text)
    {
        BroadcastChatMessageFrom(text);
    }

    public void Start()
    {
        UnsafeAllPlayerPresences.Add(this);
        OnPlayerPresenceAdded(this);
    }

    public void Update()
    {
        if (networkView.isMine)
        {
            if (Possession == null)
            {
                if (Input.GetButtonDown("Fire"))
                {
                    IndicateRespawn();
                }
            }

            // Update player labels
            if (Camera.current != null)
            {
                foreach (var playerScript in PlayerScript.UnsafeAllEnabledPlayerScripts)
                {
                    if (playerScript == null) continue;
                    Vector3 position = Camera.current.WorldToScreenPoint(InfoPointForPlayerScript(playerScript));
                    Vector2 prevScreenPosition;
                    if (!LastGUIDebugPositions.TryGetValue(playerScript, out prevScreenPosition))
                    {
                        prevScreenPosition = (Vector2) position;
                    }
                    Vector2 newScreenPosition = Vector2.Lerp(prevScreenPosition, (Vector2) position,
                        1f - Mathf.Pow(0.0000000001f, Time.deltaTime));
                    LastGUIDebugPositions[playerScript] = newScreenPosition;
                }
            }

        }
    }

    private Vector3 InfoPointForPlayerScript(PlayerScript playerScript)
    {
        Vector3 start = playerScript.gameObject.transform.position;
        start.y -= playerScript.Bounds.extents.y;
        return start;
    }

    private void IndicateRespawn()
    {
        if (Network.isServer)
            OnPlayerPresenceWantsRespawn();
        else
            networkView.RPC("ServerIndicateRespawn", RPCMode.Server);
    }

    [RPC]
// ReSharper disable once UnusedMember.Local
    private void ServerIndicateRespawn(NetworkMessageInfo info)
    {
        if (info.sender == networkView.owner)
            OnPlayerPresenceWantsRespawn();
    }

    public void OnDestroy()
    {
        OnPlayerPresenceRemoved(this);
        UnsafeAllPlayerPresences.Remove(this);
        if (Possession != null)
        {
            Destroy(Possession.gameObject);
        }

        if (wasMine)
        {
            Relay.Instance.MessageLog.OnMessageEntered -= ReceiveMessageEntered;
        }

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
        Possession = newPlayerCharacter;
        newPlayerCharacter.OnDeath += ReceivePawnDeath;
        return newPlayerCharacter;
    }

    [RPC]
    private void ClientSpawnCharacter(Vector3 spawnPosition)
    {
        var newViewID = Network.AllocateViewID();
        //Debug.Log("Will spawn character with view ID: " + newViewID);
        var newCharacter = PerformSpawnForViewID(newViewID);
        newCharacter.transform.position = spawnPosition;
        newCharacter.Possessor = this;
        Possession = newCharacter;
        newCharacter.OnDeath += ReceivePawnDeath;
        PossessedCharacterViewID = newViewID;
    }

    private void ReceivePawnDeath()
    {
        if (Possession != null)
        {
            Possession.OnDeath -= ReceivePawnDeath;
            Possession = null;
            PossessedCharacterViewID = NetworkViewID.unassigned;
        }
    }

    public void OnGUI()
    {
        if (ScreenSpaceDebug.Instance.ShouldDraw)
        {
            OnDrawDebugStuff();
        }
    }

    private void OnDrawDebugStuff()
    {
        if (networkView.isMine && Camera.current != null)
        {
            foreach (var playerScript in PlayerScript.UnsafeAllEnabledPlayerScripts)
            {
                if (playerScript == null) continue;
                Vector3 newScreenPosition = Camera.current.WorldToScreenPoint(InfoPointForPlayerScript(playerScript));
                if (newScreenPosition.z < 0) continue;
                Vector2 screenPosition;
                if (!LastGUIDebugPositions.TryGetValue(playerScript, out screenPosition))
                {
                    screenPosition = newScreenPosition;
                }
                // Good stuff, great going guys
                screenPosition.y = Screen.height - screenPosition.y;
                var rect = new Rect(screenPosition.x - 50, screenPosition.y, 100, 25);
                var healthComponent = playerScript.GetComponent<HealthScript>();
                if (healthComponent == null)
                {
                    GUI.Box(rect, "No health component");
                }
                else
                {
                    GUI.Box(rect, "H: " + healthComponent.Health + "   S: " + healthComponent.Shield);
                }
            }
        }
    }

    public void SendMessageTo(string text)
    {
        if (Server != null)
        {
            Server.SendMessageFromServer(text, networkView.owner);
        }
        else
        {
            Debug.LogWarning("Unable to send message to " + this + " because Server is null");
        }
    }

    public void OnNetworkInstantiate(NetworkMessageInfo info)
    {
        WantNameSentBack();
    }

    private void WantNameSentBack()
    {
        if (!networkView.isMine)
        {
            networkView.RPC("SendNameBack", networkView.owner);
        }
    }

    [RPC]
    private void SendNameBack(NetworkMessageInfo info)
    {
        networkView.RPC("ReceiveNameSent", info.sender, Name);
    }

    [RPC]
    private void ReceiveNameSent(string text)
    {
        bool wasNamed = HasBeenNamed;
        _Name = text;
        if (HasBeenNamed)
        {
            OnNameChanged();
            if (!wasNamed)
            {
                OnBecameNamed();
            }
        }
    }

    public void BroadcastChatMessageFrom(string text)
    {
        if (Server.networkView.isMine)
        {
            Server.BroadcastChatMessageFromServer(text, this);
        }
        else
        {
            networkView.RPC("ServerBroadcastChatMessageFrom", Server.networkView.owner, text);
        }
    }

    [RPC]
    private void ServerBroadcastChatMessageFrom(string text, NetworkMessageInfo info)
    {
        if (Server.networkView.isMine && info.sender == networkView.owner)
        {
            Server.BroadcastChatMessageFromServer(text, this);
        }
    }
}