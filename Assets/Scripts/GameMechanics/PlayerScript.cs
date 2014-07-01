using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerScript : MonoBehaviour
{
    const float JumpInputQueueTime = 0.2f;

    // tunable
    public float speed = 10;
    public float mouseSensitivity = 1.5f;
    public float lookAngleLimit = 80;
    public float gravity = -100;
    public float jumpVelocity = 65;
    public float timeBetweenDashes = 1;
    public float dashForwardVelocity = 70;
    public float dashUpwardVelocity = 30;
    // air velocity damping: 0.05f -> speed drops to 5% in one second
    public float airVelocityDamping = 0.05f;
    public float recoilDamping = 0.0005f;
	
	public float IdleTransitionFadeLength = 1.0f;

    public Transform cameraPivot;
    public Transform dashEffectPivot;
    public Renderer dashEffectRenderer;
    public CharacterController controller;
	public GameObject warningSphereFab;
    GameObject textBubble;

    Vector3 fallingVelocity;
    Vector3 lastFallingVelocity;
    Vector3 recoilVelocity;
	bool invertMouse = true;
    Vector3 inputVelocity;
    Vector3 lastInputVelocity;
    Vector3 lookRotationEuler;
    float lastJumpInputTime = -1;
    float dashCooldown = 0;
    Animation characterAnimation;
    string currentAnim;
    //public NetworkPlayer? owner;
    float sinceNotGrounded;
    bool activelyJumping;
    public bool TextBubbleVisible { get; set; }
    bool playJumpSound, playDashSound;
	int jumpsSinceGrounded = 0;
	
    public AudioSource warningSound;
    public AudioSource dashSound;
    public AudioSource landingSound;
    public AudioSource jumpSound;

    public PlayerShootingScript ShootingScript;
    public CameraScript CameraScript;
    public HealthScript HealthScript;

    public GameObject ObjectToUseForBounds;
    public Bounds Bounds { get { return ObjectToUseForBounds.renderer.bounds; } }

    public PlayerPresence Possessor { get; set; }

    public delegate void DeathHandler();
    // Invoked when the character dies
    public event DeathHandler OnDeath = delegate {};

    public EnemiesTargetingUs EnemiesTargetingUs { get; private set; }

    private float TimeSinceLastTargetedWarningPlayed = 0f;
    private const float TimeBetweenTargetedWarningNotification = 0.7f;

    // Flag visibility stuff
    public Renderer[] FlagParts;
    private bool _HasFlagVisible = false;
    private bool HasEverSetFlagVisibility = false;
    public bool HasFlagVisible
    {
        get { return _HasFlagVisible; }
        set
        {
            bool needsUpdate = (_HasFlagVisible != value) || (!HasEverSetFlagVisibility);
            if (needsUpdate)
            {
                _HasFlagVisible = value;
                foreach (var flagPart in FlagParts)
                    flagPart.enabled = _HasFlagVisible;
                // hack
                if (Network.isServer)
                    networkView.RPC("RemoteReceiveHasFlagVisible", RPCMode.Others, _HasFlagVisible);
                HasEverSetFlagVisibility = true;
            }
        }
    }

    [RPC]
    private void RemoteReceiveHasFlagVisible(bool visible, NetworkMessageInfo info)
    {
            HasFlagVisible = visible;
    }

    [RPC]
    private void ReceiveRemoteWantsFlagVisibility(NetworkMessageInfo info)
    {
        if (Network.isServer && HasEverSetFlagVisibility) // try to avoid wastefulness
        {
            networkView.RPC("RemoteReceiveHasFlagVisible", info.sender, _HasFlagVisible);
        }
    }

    public bool ShouldSendMessages
    {
        get
        {
            if (Possessor == null) return false;
            else return Possessor.Server != null;
        }
    }

    //private void RemoteSetPossessorByViewID(NetworkViewID playerPresenceViewID)
    //{
    //    Possessor = null;
    //    NetworkView view = NetworkView.Find(playerPresenceViewID);
    //    if (view == null) return;
    //    var presence = view.observed as PlayerPresence;
    //    if (presence) Possessor = presence;
    //}

    public bool Paused { get; set; }

    // Used as a global collection of all enabled PlayerScripts. Will help us
	// avoid iterating all GameObjects. It's possible to accidentally remove
	// PlayerScripts from this list while iterating it (for example, calling
	// Destroy() while iterating), so there is a safe copying variant below for
	// use when performance is not critical.
    public static readonly List<PlayerScript> UnsafeAllEnabledPlayerScripts = new List<PlayerScript>();

    // A safe (copied) list of all enabled PlayerScripts in the game world.
    public static IEnumerable<PlayerScript> AllEnabledPlayerScripts
    { get { return UnsafeAllEnabledPlayerScripts.ToList(); } }

    public delegate void PlayerScriptSpawnedHandler(PlayerScript newPlayerScript);
    // Invoked when any PlayerScript-attached gameobject is spawned
    public static event PlayerScriptSpawnedHandler OnPlayerScriptSpawned = delegate{};

    // This should really not be static
    public delegate void PlayerScriptDiedHandler(PlayerScript diedPlayerScript, PlayerPresence deathInstigator);
    public static event PlayerScriptDiedHandler OnPlayerScriptDied = delegate{};
        
    // for interpolation on remote computers only
    VectorInterpolator iPosition;
    Vector3 lastNetworkFramePosition;
    Quaternion smoothLookRotation;
    float smoothYaw;

    // Extra stuff we'll use to fix up Unity's bad character component collision
    private Vector3 LastNonCollidingPosition;
    public LayerMask SafetyCollisionMask;
    public float OverlapEjectionSpeed = 100.0f;
    public bool InstantOverlapEjection = true;

    private int OtherPlayerVisibilityLayerMask;

    // TODO unused 'get', intentional? No reason to ever set it, then.
	//List<NetworkPlayer> targetedBy { get; set; }

	List<GameObject> warningSpheres { get; set; }

    // Will be multiplied by the mouse sensitivity. We usually want to reduce
	// sensitivity when zoomed in.
    private float ZoomLookSensitivityMultiplier
    {
        get { return CameraScript.IsZoomedIn ? 0.5f : 1.0f; }
    }

    public void OnEnable()
    {
        UnsafeAllEnabledPlayerScripts.Add(this);
        ShootingScript.OnShotgunFired += ReceiveShotgunFired;
    }

    public void OnDisable()
    {
        UnsafeAllEnabledPlayerScripts.Remove(this);
        ShootingScript.OnShotgunFired -= ReceiveShotgunFired;
    }

    public void Awake() 
	{
        DontDestroyOnLoad(gameObject);

		//targetedBy = new List<NetworkPlayer>();
		warningSpheres = new List<GameObject>();

        controller = GetComponent<CharacterController>();
        characterAnimation = transform.Find("Animated Mesh Fixed").animation;
        characterAnimation.Play("idle");
	    textBubble = gameObject.FindChild("TextBubble");
        textBubble.renderer.material.color = new Color(1, 1, 1, 0);

	    characterAnimation["run"].speed = 1.25f;
        characterAnimation["backward"].speed = 1.75f;
        characterAnimation["strafeLeft"].speed = 1.5f;
        characterAnimation["strafeRight"].speed = 1.5f;

        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (!r.material.HasProperty("_Color")) continue;
            if (r.gameObject.name == "TextBubble") continue;
            if (r.gameObject.name == "flag_flag001") continue;
            r.tag = "PlayerMaterial";
        }

        iPosition = new VectorInterpolator();

        EnemiesTargetingUs = new EnemiesTargetingUs();
        EnemiesTargetingUs.OnStartedBeingLockedOnByEnemy += ReceiveStartedBeingLockedOnBy;
        EnemiesTargetingUs.OnStoppedBeingLockedOnByEnemy += ReceiveStoppedBeingLockedOnBy;

        OtherPlayerVisibilityLayerMask =
            (1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Default"));
	}

    public void Start()
    {
        if (networkView.isMine)
        {
            gameObject.layer = LayerMask.NameToLayer( "LocalPlayer" );
        }
        else
        {
            gameObject.layer = LayerMask.NameToLayer( "Player" );
        }

        OnPlayerScriptSpawned(this);

        // FIXME dirty hack
        if (networkView.isMine)
        {
            var indicator = Relay.Instance.MainCamera.GetComponent<WeaponIndicatorScript>();
            if (indicator != null)
                indicator.enabled = true;
        }

        if (!Network.isServer)
        {
            networkView.RPC("ReceiveRemoteWantsFlagVisibility", RPCMode.Server);
        }
    }

    public void OnGUI()
    {
        if(Event.current.type == EventType.KeyDown &&
           Event.current.keyCode == KeyCode.Escape)
        {
            Screen.lockCursor = false;
        }
    }

    public void GetTargetedBy(PlayerScript enemy)
    {
        Targeted(enemy);
        if (ShouldSendMessages)
        {
            networkView.RPC("RemoteReceiveTargetedBy", RPCMode.Others, enemy.networkView.viewID);
        }
    }

    public void GetUntargetedBy(PlayerScript enemy)
    {
        Untargeted(enemy);
        if (ShouldSendMessages)
        {
            networkView.RPC("RemoteReceiveUntargetedBy", RPCMode.Others, enemy.networkView.viewID);
        }
    }

    [RPC]
// ReSharper disable once UnusedMember.Local
    private void RemoteReceiveTargetedBy(NetworkViewID enemyPlayerScriptID, NetworkMessageInfo info)
    {
        try
        {
            var view = NetworkView.Find(enemyPlayerScriptID);
            var enemy = (PlayerScript)view.observed;
            if (view.owner == info.sender)
            {
                Targeted(enemy);
            }
        }
        catch (Exception)
        {
            // nope lol
        }
    }
    [RPC]
// ReSharper disable once UnusedMember.Local
    private void RemoteReceiveUntargetedBy(NetworkViewID enemyPlayerScriptID, NetworkMessageInfo info)
    {
        try
        {
            var view = NetworkView.Find(enemyPlayerScriptID);
            var enemy = (PlayerScript)view.observed;
            if (view.owner == info.sender)
            {
                Untargeted(enemy);
            }
        }
        catch (Exception)
        {
            // nope lol
        }
    }
	
    private void Targeted( PlayerScript targetingUs )
    {
        //ScreenSpaceDebug.AddMessage("RECEIVE TARGET BY", targetingUs.transform.position);

        EnemiesTargetingUs.TryAddEnemyLockingOnToUs(targetingUs);

        //print( "Targeted by: " + PlayerRegistry.For( aggressor ).Username );
		
		//GameObject sphere = (GameObject)Instantiate( warningSphereFab, transform.position, transform.rotation );
		//sphere.transform.parent = gameObject.transform;
        //sphere.GetComponent<Billboard>().target = PlayerRegistry.For( aggressor ).Location;
		
		//warningSpheres.Add( sphere );
    }
	
    private void Untargeted( PlayerScript enemy )
    {
        //ScreenSpaceDebug.AddMessage("RECEIVE UNTARGET BY", enemy.transform.position);

        EnemiesTargetingUs.TryRemoveEnemyLockingOnToUs(enemy);
		
        //print( "Untargeted by: " + PlayerRegistry.For( aggressor ).Username );
		
        //int id = warningSpheres.FindIndex( a => a.GetComponent<Billboard>().target == PlayerRegistry.For( aggressor ).Location );
        //if( id == -1 ) return;
		
        //Destroy( warningSpheres[id] );
        //warningSpheres.RemoveAt( id );
    }

    private void ReceiveStartedBeingLockedOnBy(PlayerScript enemy)
    {
        // Sometimes unity will call an RPC even if 'this' has already been
		// 'destroyed', and because unity overloads null comparison to mean
		// 'destroyed', well, we're going to do this check. Great.
        if (this == null) return;
        // Also check if enemy is null, might have been destroyed by the time
		// this RPC is called.
        if (networkView.isMine && enemy != null)
        {
            ScreenSpaceDebug.AddMessage("TARGETED BY", enemy.transform.position);
        }
    }

    private void ReceiveStoppedBeingLockedOnBy(PlayerScript enemy)
    {
        if (this == null) return;
        if (networkView.isMine && enemy != null)
        {
            ScreenSpaceDebug.AddMessage("UNTARGETED BY", enemy.transform.position);
        }
    }
	
    public void ResetWarnings()
    {
        if( !networkView.isMine ) return; 
		
		for( int i = 0; i < warningSpheres.Count; i++ ) Destroy( warningSpheres[i] );
		warningSpheres.Clear();
    }

    [RPC]
    public void AddRecoil(Vector3 impulse)
    {
        if (!networkView.isMine) return;
        recoilVelocity += impulse;
        if (impulse.y > 0)
            sinceNotGrounded = 0.25f;
        //Debug.Log("added recoil : " + impulse);
    }

    public void ResetVelocities()
    {
        if (!networkView.isMine) return;
        recoilVelocity = Vector3.zero;
        fallingVelocity = Vector3.zero;
    }


    private Vector3 RawAxisMovementDirection
    { 
        get {
            return (Input.GetAxisRaw("Strafe") * transform.right +
                    Input.GetAxisRaw("Thrust") * transform.forward).normalized;
        }
    }

    public void Update()
    {
        //if (Network.peerType == NetworkPeerType.Disconnected) return;
        if (Paused) return;

        // Update enemies targeting us and related stuff
        EnemiesTargetingUs.Update();

        // Only interested in playing sound effects locally
        if (networkView.isMine)
        {
            TimeSinceLastTargetedWarningPlayed += Time.deltaTime;
            // Play sound effect if necessary
            if (EnemiesTargetingUs.IsLockedByAnyEnemy &&
                TimeSinceLastTargetedWarningPlayed >= TimeBetweenTargetedWarningNotification)
            {
                TimeSinceLastTargetedWarningPlayed = 0f;
        		if( GlobalSoundsScript.soundEnabled )
        		   warningSound.Play(); 
            }
        }

        if (networkView.isMine)
        {
            //TextBubbleVisible = ChatScript.Instance.showChat;

            inputVelocity =
                Input.GetAxis("Strafe") * transform.right +
                Input.GetAxis("Thrust") * transform.forward;
            if(inputVelocity.sqrMagnitude > 1)
                inputVelocity.Normalize();

            inputVelocity *= speed;

            if (Input.GetButtonDown("Jump") && fallingVelocity.y <= 2 && !(sinceNotGrounded > 0 && jumpsSinceGrounded > 1 ) )
            {
				jumpsSinceGrounded++;
                lastJumpInputTime = Time.time;
            }

            if (!Input.GetButton("Jump"))
            {
                activelyJumping = false;
                if(fallingVelocity.y > 2)
                    fallingVelocity.y = 2;
            }
			
			if (Screen.lockCursor)
			{
                float invertMultiplier = invertMouse ? -1 : 1;
                lookRotationEuler +=
                    MouseSensitivityScript.Sensitivity *
                    ZoomLookSensitivityMultiplier *
                    new Vector3(
                        Input.GetAxis("Vertical Look") * invertMultiplier,
                        Input.GetAxis("Horizontal Look"),
                        0);
			}
			
			lookRotationEuler.x = Mathf.Clamp(
                lookRotationEuler.x, -lookAngleLimit, lookAngleLimit);

			if (Input.GetKeyDown("i"))
				invertMouse = !invertMouse;

            if (Input.GetMouseButtonUp(0))
                Screen.lockCursor = true;

            Screen.showCursor = !Screen.lockCursor;
            smoothYaw = lookRotationEuler.y;
            smoothLookRotation = Quaternion.Euler(lookRotationEuler);
        }
        else
        {
            // TODO fixme lol
            if (iPosition != null && iPosition.IsRunning)
            {
                //Debug.Log("Before correct : " + transform.position);
                transform.position += iPosition.Update();
                //Debug.Log("After correct : " + transform.position);
            }

            var amt = (float)Math.Pow(0.0000000001, Time.deltaTime);
            smoothLookRotation = Quaternion.Slerp(smoothLookRotation, Quaternion.Euler(lookRotationEuler), 1.0f - amt);
            smoothYaw = smoothLookRotation.eulerAngles.y;
        }

        // set up text bubble visibility
        if (!TextBubbleVisible)
        {
            var o = textBubble.renderer.material.color.a;
            textBubble.renderer.material.color = new Color(1, 1, 1, Mathf.Clamp(o - Time.deltaTime * 10, 0, 0.875f));
            if (o <= 0)
                textBubble.renderer.enabled = false;
        }
        else
        {
            textBubble.renderer.enabled = true;
            var o = textBubble.renderer.material.color.a;
            textBubble.renderer.material.color = new Color(1, 1, 1, Mathf.Clamp(o + Time.deltaTime * 10, 0, 0.875f));
        }
        textBubble.transform.LookAt(Camera.main.transform);
        textBubble.transform.localRotation = textBubble.transform.localRotation * Quaternion.Euler(90, 0, 0);

        // sync up actual player and camera transforms
        Vector3 euler = transform.rotation.eulerAngles;
        euler.y = smoothYaw;
        transform.rotation = Quaternion.Euler(euler);
        cameraPivot.rotation = smoothLookRotation;

        // dash animation
        Color color = dashEffectRenderer.material.GetColor("_TintColor");
        Vector3 dashVelocity = new Vector3(fallingVelocity.x, activelyJumping ? 0 : Math.Max(fallingVelocity.y, 0), fallingVelocity.z);
        if(dashVelocity.magnitude > 1/256.0)
        {
            color.a = dashVelocity.magnitude / dashForwardVelocity / 8;
            dashEffectPivot.LookAt(transform.position + dashVelocity.normalized);
        }
        else
        {
            color.a = 0;
        }
        dashEffectRenderer.material.SetColor("_TintColor", color);

        /*if (owner.HasValue && PlayerRegistry.Has(owner.Value))
        {
            var info = PlayerRegistry.For(owner.Value);

            transform.Find("Animated Mesh Fixed").Find("flag_pole001").Find("flag_flag001").renderer.material.color = info.Color;

            if (!networkView.isMine)
                GetComponentInChildren<TextMesh>().text = info.Username;
        }*/

        TimeSinceRocketJump += Time.deltaTime;

        if(!controller.enabled) return;
        if (Paused) return;
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        Vector3 smoothedInputVelocity = inputVelocity * 0.6f + lastInputVelocity * 0.45f;
        lastInputVelocity = smoothedInputVelocity;

        // jump and dash
        dashCooldown -= Time.deltaTime;
	 	bool justJumped = false;
        if((networkView.isMine) && Time.time - lastJumpInputTime <= JumpInputQueueTime)
        {
            bool groundedOrRecentRocketJump = controller.isGrounded || RecentlyDidRocketJump;
            bool recoilOk = recoilVelocity.y <= 0;
            // TODO ugly booleans
            if ((groundedOrRecentRocketJump || sinceNotGrounded < 0.25f) && (recoilOk || RecentlyDidRocketJump))
            {
                ConsumedRocketJump();
                //Debug.Log("Accepted jump");
                lastJumpInputTime = -1;
                justJumped = true;
                activelyJumping = true;
                fallingVelocity.y = jumpVelocity;
                characterAnimation.CrossFade(currentAnim = "jump", IdleTransitionFadeLength );
                playJumpSound = true;
				
				if( GlobalSoundsScript.soundEnabled )
	                jumpSound.Play();
				
                sinceNotGrounded = 0.25f;
            }
            else if(dashCooldown <= 0)
            {
                activelyJumping = false;
                lastJumpInputTime = -1;
                dashCooldown = timeBetweenDashes;

                if (currentAnim == "jump")
                    characterAnimation.Rewind("jump");
                characterAnimation.CrossFade(currentAnim = "jump", IdleTransitionFadeLength );
                playDashSound = true;
				
				if(GlobalSoundsScript.soundEnabled) {
                	dashSound.Play();
				}

                Vector3 dashDirection = RawAxisMovementDirection;

                // Dash upwards if no significant direction input
                if (dashDirection.magnitude < Mathf.Epsilon)
                    dashDirection = Vector3.up * 0.4f;

                fallingVelocity +=
                    dashDirection * dashForwardVelocity +
                    Vector3.up * dashUpwardVelocity;

                recoilVelocity.y *= 0.5f;
            }
        }

        if(controller.isGrounded)
        {
            if (!justJumped)
			{
                sinceNotGrounded = 0;
				jumpsSinceGrounded = 0;
			}
            // infinite friction
            if (fallingVelocity.y <= 0)
                fallingVelocity = Vector3.up * gravity * Time.deltaTime;
        }
        else
        {
            sinceNotGrounded += Time.deltaTime;
            // air drag / gravity
            fallingVelocity.y += gravity * Time.deltaTime;
            fallingVelocity.x *= Mathf.Pow(airVelocityDamping, Time.deltaTime);
            fallingVelocity.z *= Mathf.Pow(airVelocityDamping, Time.deltaTime);
        }

        // Update running animation
        if( controller.isGrounded && !justJumped )
        {
            if( MathHelper.AlmostEquals( smoothedInputVelocity, Vector3.zero, 0.1f ) && currentAnim != "idle" )
                    characterAnimation.CrossFade( currentAnim = "idle", IdleTransitionFadeLength );
            else
            {
                var xDir = Vector3.Dot( smoothedInputVelocity, transform.right );
                var zDir = Vector3.Dot( smoothedInputVelocity, transform.forward );

                const float epsilon = 15f;

                //Debug.Log("xDir : " + xDir + " | zDir : " + zDir);

                if (zDir > epsilon)
                {
                    if (currentAnim != "run")
                        characterAnimation.CrossFade(currentAnim = "run", IdleTransitionFadeLength );
                }
                else if (zDir < -epsilon)
                {
                    if (currentAnim != "backward")
                        characterAnimation.CrossFade(currentAnim = "backward", IdleTransitionFadeLength );
                }
                else if (xDir > epsilon)
                {
                    if (currentAnim != "strafeRight")
                        characterAnimation.CrossFade(currentAnim = "strafeRight", IdleTransitionFadeLength );
                }
                else if (xDir < -epsilon)
                {
                    if (currentAnim != "strafeLeft")
                        characterAnimation.CrossFade(currentAnim = "strafeLeft", IdleTransitionFadeLength );
                }
            }
        }

        var smoothFallingVelocity = fallingVelocity * 0.4f + lastFallingVelocity * 0.65f;
        lastFallingVelocity = smoothFallingVelocity;

        // damp recoil
        if (!controller.isGrounded)
        {
            recoilVelocity.x *= Mathf.Pow(recoilDamping * 10, Time.deltaTime);
            recoilVelocity.y *= Mathf.Pow(recoilDamping * 100, Time.deltaTime);
            recoilVelocity.z *= Mathf.Pow(recoilDamping * 10, Time.deltaTime);
        }
        else
        {
            recoilVelocity.x *= Mathf.Pow(recoilDamping / 25, Time.deltaTime);
            recoilVelocity.y *= Mathf.Pow(recoilDamping * 100, Time.deltaTime);
            recoilVelocity.z *= Mathf.Pow(recoilDamping / 25, Time.deltaTime);
        }

        // move!
        Vector3 movementVelocity = smoothFallingVelocity + smoothedInputVelocity + recoilVelocity;
        Vector3 movementVector = movementVelocity * Time.deltaTime;
        controller.Move(movementVector);
        bool doesOverlap = CheckOverlap(transform.position);
        if (doesOverlap)
        {
            fallingVelocity = Vector3.zero;
            if (InstantOverlapEjection)
                transform.position = LastNonCollidingPosition;
            else
                transform.position = Vector3.Lerp(transform.position, LastNonCollidingPosition, Time.deltaTime * OverlapEjectionSpeed);
        }
        else
        {
            LastNonCollidingPosition = transform.position;
        }

        if (sinceNotGrounded > 0.25f && controller.isGrounded) {
			if(GlobalSoundsScript.soundEnabled) {
            	landingSound.Play();
			}
		}

        if (sinceNotGrounded > 0.25f && controller.isGrounded) {
			if(GlobalSoundsScript.soundEnabled) {
            	landingSound.Play();
			}
		}

        if (controller.isGrounded)
            recoilVelocity.y = 0;
    }

    // Used by HealthScript in Respawn
    public void ResetAnimation()
    {
        characterAnimation.Play(currentAnim = "idle");
        lastInputVelocity = inputVelocity = Vector3.zero;
    }

    public void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        //var pOwner = owner.HasValue ? owner.Value : default(NetworkPlayer);
        //stream.Serialize(ref pOwner);
        //if (stream.isReading) owner = pOwner;

        Vector3 pPosition = stream.isWriting ? transform.position : Vector3.zero;

        stream.Serialize(ref pPosition);
        stream.Serialize(ref inputVelocity);
        stream.Serialize(ref fallingVelocity);
        stream.Serialize(ref activelyJumping);
        stream.Serialize(ref recoilVelocity);
        stream.Serialize(ref playDashSound);
        stream.Serialize(ref playJumpSound);
        stream.Serialize(ref lookRotationEuler);

        // Health script (should be moved here probably)
        stream.Serialize(ref HealthScript._Shield);
        stream.Serialize(ref HealthScript._Health);

        if (stream.isReading)
        {
            //Debug.Log("pPosition = " + pPosition + " / transform.position = " + transform.position);
            if (lastNetworkFramePosition == pPosition)
                transform.position = pPosition;

            // TODO iposition null check nope
            if (iPosition != null && !iPosition.Start(pPosition - transform.position))
                transform.position = pPosition;

            if (playDashSound && GlobalSoundsScript.soundEnabled) dashSound.Play();
            if (playJumpSound && GlobalSoundsScript.soundEnabled) jumpSound.Play();

            lastNetworkFramePosition = pPosition;

            HealthScript.UpdateShield();
        }

        playJumpSound = playDashSound = false;
    }

    public void OnDestroy( )
    {
        EnemiesTargetingUs.ClearAllEnemies();
        EnemiesTargetingUs.OnStartedBeingLockedOnByEnemy -= ReceiveStartedBeingLockedOnBy;
        EnemiesTargetingUs.OnStoppedBeingLockedOnByEnemy -= ReceiveStoppedBeingLockedOnBy;
        EnemiesTargetingUs.Destroy();

        // TODO this belongs earlier in the chain of death-related stuff
        OnDeath();
    }

    // TODO gross
    [RPC]
    public void PerformDestroy()
    {
        if (!Network.isServer)
        {
            networkView.RPC("PerformDestroy", RPCMode.Server);
        }
        else
        {
            Network.RemoveRPCs(networkView.owner, Relay.CharacterSpawnGroupID);
            Network.Destroy(networkView.viewID);
        }
    }

    // Try to guess capsule info for doing overlap tests and sweeps, because
	// Character Controller is a piece of shit and doesn't expose anything it
	// does. Probably wrong in many ways (not accounting for rotation, etc., and
	// probably getting 'skin' wrong, which we don't have access to, cool).
    private void GetControllerCapsuleGeometryAtPosition(Vector3 position, out Vector3 top, out Vector3 bottom, out float height, out float radius)
    {
        height = controller.height - (controller.height * 0.08f);
        radius = controller.radius - controller.radius * 0.08f;
        top = position + new Vector3(0, height/2, 0);
        bottom = position - new Vector3(0, height/2, 0);
    }

    private bool CheckOverlap(Vector3 position)
    {
        float height, radius;
        Vector3 top, bottom;
        GetControllerCapsuleGeometryAtPosition(position, out top, out bottom, out height, out radius);
        return Physics.CheckCapsule(top, bottom, radius, SafetyCollisionMask);
    }

    //private bool CheckSweep(Vector3 start, Vector3 end, out RaycastHit hitInfo)
    //{
    //    float height, radius;
    //    Vector3 top, bottom;
    //    GetControllerCapsuleGeometryAtPosition(start, out top, out bottom, out height, out radius);
    //    Vector3 movementVector = end - start;
    //    Vector3 movementDirection = movementVector.normalized;
    //    float movementDistance = movementVector.magnitude;
    //    return Physics.CapsuleCast(top, bottom, radius, movementDirection, out hitInfo, movementDistance);
    //}

    private float RecentRocketJumpThreshold = 0.2f;
    private float TimeSinceRocketJump = 0f;
    private bool HasAvailableRocketJump;

    public void ReceiveStartedRocketJump()
    {
        TimeSinceRocketJump = 0f;
        HasAvailableRocketJump = true;
    }

    private bool RecentlyDidRocketJump
    {
        get { return TimeSinceRocketJump < RecentRocketJumpThreshold && HasAvailableRocketJump; }
    }

    private void ConsumedRocketJump()
    {
        HasAvailableRocketJump = false;
    }

    private void ReceiveShotgunFired()
    {
        // Distance to check downward to see if we're on the ground
        Vector3 nearGround = new Vector3(0f, -3f, 0f);
        // If we're looking down, touching ground (or close to touching), and
		// fire the shotgun, we probably want to rocket jump.
        if (IsLookingDownFarEnoughForRocketJump && CheckOverlap(transform.position + nearGround))
        {
            ReceiveStartedRocketJump();
        }
    }

    // Dot product distance for when checking if we're looking down for rocket
	// jumping
    private const float RocketJumpLookingThreshold = 0.3f;

    public bool IsLookingDownFarEnoughForRocketJump
    {
        get
        {
            float difference = Vector3.Dot(CameraScript.LookingDirection, Vector3.down);
            return (difference > 1.0f - RocketJumpLookingThreshold);
        }
    }

    public void RequestedToDieByOwner(PlayerPresence instigator)
    {
        if (Network.isServer)
            OnPlayerScriptDied(this, instigator);
        else
            networkView.RPC("ServerRequestedToDie", RPCMode.Server, instigator.networkView.viewID);
    }

    [RPC]
    private void ServerRequestedToDie(NetworkViewID instigatorPresenceViewID)
    {
        OnPlayerScriptDied(this, PlayerPresence.TryGetPlayerPresenceFromNetworkViewID(instigatorPresenceViewID));
    }

    public bool CanSeeOtherPlayer(PlayerScript other)
    {
        Transform startTransform = CameraScript.CameraUsed != null ? CameraScript.CameraUsed.transform : transform;
        Vector3 start = startTransform.position;
        Vector3 direction = (other.transform.position - start).normalized;
        float distance = Vector3.Distance(start, other.transform.position);
        RaycastHit hitInfo;

        // Maybe
        if (Physics.Raycast(start, direction, out hitInfo, distance, OtherPlayerVisibilityLayerMask))
        {
            var hitPlayer = hitInfo.collider.GetComponentInParent<PlayerScript>();
            return hitPlayer != null && hitPlayer == other;
        }
        // Nope
        return false;
    }
}

abstract class Interpolator<T>
{
    const float InterpolateOver = 1;

    public T Delta { get; protected set; }

    public abstract bool Start(T delta);
    public abstract T Update();
    public bool IsRunning { get; protected set; }

    protected void UpdateInternal()
    {
        if (!IsRunning) return;
        SinceStarted += Time.deltaTime;
        if (SinceStarted >= InterpolationTime)
            IsRunning = false;
    }

    protected float InterpolationTime
    {
        get { return (1.0f / Network.sendRate) * InterpolateOver; }
    }
    protected float SinceStarted { get; set; }
}
class VectorInterpolator : Interpolator<Vector3>
{
    public override bool Start(Vector3 delta)
    {
        IsRunning = !MathHelper.AlmostEquals(delta, Vector3.zero, 0.01f);
        //if (IsRunning) Debug.Log("vector interpolator started, delta == " + delta);
        SinceStarted = 0;
        Delta = delta;
        return IsRunning;
    }
    public override Vector3 Update()
    {
        UpdateInternal();
        if (!IsRunning) return Vector3.zero;
        //Debug.Log("Correcting for " + Delta + " with " + (Delta * Time.deltaTime / InterpolationTime));
        return Delta * Time.deltaTime / InterpolationTime;
    }
}

public class EnemyTargetingUsInfo
{
    public float TimeSinceLastNotification;
}

public class EnemiesTargetingUs
{
    public Dictionary<PlayerScript, EnemyTargetingUsInfo> Enemies { get; private set; }

    private readonly List<PlayerScript> RemovalCache;

    public float AutoRemoveTime = 2f;

    public delegate void EnemyLockOnStateChangedHandler(PlayerScript enemy);

    public event EnemyLockOnStateChangedHandler OnStartedBeingLockedOnByEnemy = delegate {};
    public event EnemyLockOnStateChangedHandler OnStoppedBeingLockedOnByEnemy = delegate {};

    public bool IsLockedByEnemy(PlayerScript enemy)
    {
        return Enemies.ContainsKey(enemy);
    }

    public bool IsLockedByAnyEnemy
    {
        get { return Enemies.Count > 0; }
    }

    public IEnumerable<PlayerScript> EnemiesLockingUs
    {
        get { return Enemies.Keys; }
    }

    public EnemiesTargetingUs()
    {
        Enemies = new Dictionary<PlayerScript, EnemyTargetingUsInfo>();
        RemovalCache = new List<PlayerScript>();
        PlayerScript.OnPlayerScriptDied += ReceiveEnemyRemoved;
    }

    public void TryAddEnemyLockingOnToUs(PlayerScript enemy)
    {
        EnemyTargetingUsInfo info;
        if (Enemies.TryGetValue(enemy, out info))
        {
            info.TimeSinceLastNotification = 0f;
        }
        else
        {
            info = new EnemyTargetingUsInfo {TimeSinceLastNotification = 0f};
            Enemies.Add(enemy, info);
            OnStartedBeingLockedOnByEnemy(enemy);
        }
    }

    public void TryRemoveEnemyLockingOnToUs(PlayerScript enemy)
    {
        if (Enemies.ContainsKey(enemy))
        {
            Enemies.Remove(enemy);
            OnStoppedBeingLockedOnByEnemy(enemy);
        }
    }

    public void Update()
    {
        RemovalCache.Clear();
        foreach (var enemyTargetingUs in Enemies)
        {
            enemyTargetingUs.Value.TimeSinceLastNotification += Time.deltaTime;
            if (enemyTargetingUs.Value.TimeSinceLastNotification > AutoRemoveTime)
                RemovalCache.Add(enemyTargetingUs.Key);
        }
        foreach (var enemyTargetingUs in RemovalCache)
        {
            Enemies.Remove(enemyTargetingUs);
            OnStoppedBeingLockedOnByEnemy(enemyTargetingUs);
        }
    }

    public void ClearAllEnemies()
    {
        foreach (var enemy in Enemies.Keys)
        {
            OnStoppedBeingLockedOnByEnemy(enemy);
        }
        Enemies.Clear();
    }

    public void Destroy()
    {
        PlayerScript.OnPlayerScriptDied -= ReceiveEnemyRemoved;
    }

    private void ReceiveEnemyRemoved(PlayerScript enemy, PlayerPresence instigator)
    {
        TryRemoveEnemyLockingOnToUs(enemy);
    }
}