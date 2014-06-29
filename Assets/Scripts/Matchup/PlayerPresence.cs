using System;
using System.Collections.Generic;
using System.Linq;
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
            //var newName = value == null ? "null" : value.name;
            //Debug.Log("Setting presence " + Name + "'s possession to " + newName);

            // Do nothing if same
            if (_Possession == value) return;

            // Not dead yet, but we won't be interested anymore if it dies
            if (_Possession != null)
            {
                _Possession.OnDeath -= ReceivePawnDeath;
            }

            _Possession = value;
            if (_Possession != null)
            {
                _Possession.Possessor = this;
                _Possession.CameraScript.IsExteriorView = WantsExteriorView;
                if (networkView.isMine)
                {
                    _Possession.CameraScript.BaseFieldOfView = PlayerPrefs.GetFloat("fov",
                        CameraScript.DefaultBaseFieldOfView);
                    _Possession.CameraScript.AdjustCameraFOVInstantly();
                }
                _Possession.OnDeath += ReceivePawnDeath;

                if (_Possession.networkView != null)
                    PossessedCharacterViewID = _Possession.networkView.viewID;
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

    private int _Score;
    public int Score
    {
        get { return _Score; }
        set { _Score = value; }
    }

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

    private bool _IsDoingMenuStuff = false;

    private float TimeToHoldLeaderboardFor = 0f;
    private float DefaultAutoLeaderboardTime = 1.85f;

    public void DisplayScoreForAWhile()
    {
        TimeToHoldLeaderboardFor = DefaultAutoLeaderboardTime;
    }

    public bool IsDoingMenuStuff
    {
        get { return _IsDoingMenuStuff; }
        set { _IsDoingMenuStuff = value; }
    }

    public void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        NetworkViewID prevPossesedID = PossessedCharacterViewID;
        stream.Serialize(ref PossessedCharacterViewID);
        stream.Serialize(ref _IsDoingMenuStuff);
        stream.Serialize(ref _Score);
        if (stream.isReading)
        {
            if (Possession == null)
            {
                // see if possession id from network is not null
                // see if new possession object from that id is not null
                // then assign
                PlayerScript character = TryGetPlayerScriptFromNetworkViewID(PossessedCharacterViewID);
                if (character != null) Possession = character;
            }
            else
            {
                // see if new possession id is different from current possession id
                // assign new possession, even if null
                if (prevPossesedID != PossessedCharacterViewID)
                {
                    Possession = TryGetPlayerScriptFromNetworkViewID(PossessedCharacterViewID);
                }
            }
        }
    }

    // TODO factor out
    public static PlayerPresence TryGetPlayerPresenceFromNetworkViewID(NetworkViewID viewID)
    {
        if (viewID == NetworkViewID.unassigned) return null;
        NetworkView view = null;
        try
        {
            view = NetworkView.Find(viewID);
        }
        catch (Exception)
        {
            //Debug.Log(e);
        }
        if (view != null)
        {
            var presence = view.observed as PlayerPresence;
            return presence;
        }
        else
        {
            return null;
        }
    }

    private PlayerScript TryGetPlayerScriptFromNetworkViewID(NetworkViewID viewID)
    {
        if (viewID == NetworkViewID.unassigned) return null;
        NetworkView view = null;
        try
        {
            view = NetworkView.Find(viewID);
        }
        catch (Exception)
        {
            //Debug.Log(e);
        }
        if (view != null)
        {
            var character = view.observed as PlayerScript;
            return character;
        }
        else
        {
            return null;
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

            IsDoingMenuStuff = Relay.Instance.MessageLog.HasInputOpen;

            // Debug visibility info for other playerscripts
            if (Possession != null)
            {
                foreach (var character in PlayerScript.UnsafeAllEnabledPlayerScripts)
                {
                    if (character != Possession)
                    {
                        bool canSee = Possession.CanSeeOtherPlayer(character);
                        if (canSee)
                        {
                            ScreenSpaceDebug.AddMessageOnce("VISIBLE", character.transform.position);
                        }
                    }
                }
            }

            // Leaderboard show/hide
            // Always show when not possessing anything
            if (Possession == null || TimeToHoldLeaderboardFor >= 0f)
            {
                Server.Leaderboard.Show = true;
            }
            // Otherwise, show when holding tab
            else
            {
                Server.Leaderboard.Show = Input.GetKey("tab");
            }

            TimeToHoldLeaderboardFor -= Time.deltaTime;
        }

        if (Possession != null)
        {
            // toggle bubble
            Possession.TextBubbleVisible = IsDoingMenuStuff;
        }

        if (Input.GetKeyDown("f11"))
            ScreenSpaceDebug.LogMessageSizes();
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
        {
            DoActualSpawn(position);
        }
        else
        {
            networkView.RPC("RemoteSpawnCharacter", networkView.owner, position);
        }
    }

    // Used by owner of this Presence
    private void DoActualSpawn(Vector3 position)
    {
        // ondestroy will be bound in the setter
        Possession = (PlayerScript)Network.Instantiate(DefaultPlayerCharacterPrefab, position, Quaternion.identity, Relay.CharacterSpawnGroupID);
    }

    [RPC]
    private void RemoteSpawnCharacter(Vector3 position, NetworkMessageInfo info)
    {
        if (info.sender == Server.networkView.owner)
        {
            if (Possession != null)
            {
                Possession = null;
                CleanupOldCharacter(Possession);
            }
            DoActualSpawn(position);
        }
    }

    private void CleanupOldCharacter(PlayerScript character)
    {
        if (Relay.Instance.IsConnected)
        {
            Network.RemoveRPCs(character.networkView.owner, Relay.CharacterSpawnGroupID);
            Network.Destroy(character.gameObject);
        }
        else
        {
            // TODO Don't need to remove RPCs if offline? What if we start the server again ?? Fuck unity
            Destroy(character.gameObject);
        }
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

        // Draw player names
        if (networkView.isMine && Possession != null && Camera.current != null)
        {
            GUI.skin = Relay.Instance.BaseSkin;
            GUIStyle boxStyle = new GUIStyle(Relay.Instance.BaseSkin.customStyles[2])
            {
                fixedWidth = 0,
                fixedHeight = 18,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(5, 5, 3, 3),
            };
            foreach (var character in PlayerScript.UnsafeAllEnabledPlayerScripts)
            {
                if (character == null) continue;
                if (character == Possession) continue;
                if (!Possession.ShootingScript.CharacterIsInTargets(character)) continue;
                Vector3 screenPosition = Camera.current.WorldToScreenPoint(InfoPointForPlayerScript(character));
                screenPosition.y = Screen.height - screenPosition.y;
                if (screenPosition.z < 0) continue;
                bool isVisible = Possession.CanSeeOtherPlayer(character);
                if (!isVisible) continue;
                string otherPlayerName;
                if (character.Possessor == null)
                    otherPlayerName = "?";
                else
                    otherPlayerName = character.Possessor.Name;
                Vector2 baseNameSize = boxStyle.CalcSize(new GUIContent(otherPlayerName));
                float baseNameWidth = baseNameSize.x + 10;
                var rect = new Rect(screenPosition.x - baseNameWidth/2, screenPosition.y, baseNameWidth,
                    boxStyle.fixedHeight);
                GUI.Box(rect, otherPlayerName, boxStyle);
            }
        }
    }

    private void OnDrawDebugStuff()
    {
        if (networkView.isMine && Camera.current != null)
        {
            GUI.skin = Relay.Instance.BaseSkin;
            GUIStyle boxStyle = new GUIStyle(Relay.Instance.BaseSkin.box) {fixedWidth = 0};
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
                var rect = new Rect(screenPosition.x - 50, screenPosition.y, 75, 15);
                var healthComponent = playerScript.GetComponent<HealthScript>();
                if (healthComponent == null)
                {
                    GUI.Box(rect, "No health component");
                }
                else
                {
                    GUI.Box(rect, "H: " + healthComponent.Health + "   S: " + healthComponent.Shield, boxStyle);
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

    // Only works from server and owner
    public void SetScorePoints(int points)
    {
        if (networkView.isMine)
            OwnerSetScorePoints(points);
        else
            networkView.RPC("RemoteSetScorePoints", networkView.owner, points);
    }
    [RPC]
    private void RemoteSetScorePoints(int points, NetworkMessageInfo info)
    {
        if (info.sender != Server.networkView.owner) return;
        if (networkView.isMine)
            OwnerSetScorePoints(points);
        else
            networkView.RPC("RemoteSetScorePoints", networkView.owner, points);
    }

    private void OwnerSetScorePoints(int points)
    {
        Score = points;
        DisplayScoreForAWhile();
    }

    // Only works from server and owner
    public void ReceiveScorePoints(int points)
    {
        if (networkView.isMine)
        {
            OwnerReceiveScorePoints(points);
        }
        else
        {
            networkView.RPC("RemoteReceiveScorePoints", networkView.owner, points);
        }
    }

    private void OwnerReceiveScorePoints(int points)
    {
        Score += points;
        DisplayScoreForAWhile();
    }

    [RPC]
    private void RemoteReceiveScorePoints(int points, NetworkMessageInfo info)
    {
        if (info.sender != Server.networkView.owner) return;
        if (networkView.isMine)
        {
            OwnerReceiveScorePoints(points);
        }
        else
        {
            networkView.RPC("RemoteReceiveScorePoints", networkView.owner, points);
        }
    }
}