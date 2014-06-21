using System;
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
    public NetworkPlayer? owner;
    float sinceNotGrounded;
    bool activelyJumping;
    bool textBubbleVisible;
    bool playJumpSound, playDashSound;
	int jumpsSinceGrounded = 0;
	
    public AudioSource warningSound;
    public AudioSource dashSound;
    public AudioSource landingSound;
    public AudioSource jumpSound;

    public bool Paused { get; set; }

    // Used as a global collection of all enabled PlayerScripts. Will help us
	// avoid iterating all GameObjects.
    public static List<PlayerScript> EnabledPlayerScripts = new List<PlayerScript>();

    // for interpolation on remote computers only
    VectorInterpolator iPosition;
    Vector3 lastNetworkFramePosition;
    Quaternion smoothLookRotation;
    float smoothYaw;

    // TODO unused 'get', intentional? No reason to ever set it, then.
	//List<NetworkPlayer> targetedBy { get; set; }

	List<GameObject> warningSpheres { get; set; }

    public void OnEnable()
    {
        EnabledPlayerScripts.Add(this);
    }

    public void OnDisable()
    {
        EnabledPlayerScripts.Remove(this);
    }

    public void Awake() 
	{
		//targetedBy = new List<NetworkPlayer>();
		warningSpheres = new List<GameObject>();
		
        DontDestroyOnLoad(gameObject);

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
	}

    public void OnNetworkInstantiate(NetworkMessageInfo info)
    {
        if( Network.isServer )
        {
            foreach( NetworkView nv in GetComponents<NetworkView>() )
                foreach( NetworkPlayer np in Network.connections )
                    nv.SetScope( np, true );
        }

        if( !networkView.isMine )
        {
            owner = networkView.owner;
            iPosition = new VectorInterpolator();
            //enabled = false;
        }
        else
        {
            owner = Network.player;
            gameObject.layer = LayerMask.NameToLayer( "LocalPlayer" );
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
	
    [RPC]
    public void Targeted( NetworkPlayer aggressor )
    {
        if( !networkView.isMine ) return;
		
		if( GlobalSoundsScript.soundEnabled )
		   warningSound.Play(); 
		
		print( "Targeted by: " + PlayerRegistry.For( aggressor ).Username );
		
		GameObject sphere = (GameObject)Instantiate( warningSphereFab, transform.position, transform.rotation );
		sphere.transform.parent = gameObject.transform;
		sphere.GetComponent<Billboard>().target = PlayerRegistry.For( aggressor ).Location;
		
		warningSpheres.Add( sphere );
    }
	
    [RPC]
    public void Untargeted( NetworkPlayer aggressor )
    {
        if( !networkView.isMine  ) return;
		
		print( "Untargeted by: " + PlayerRegistry.For( aggressor ).Username );
		
		int id = warningSpheres.FindIndex( a => a.GetComponent<Billboard>().target == PlayerRegistry.For( aggressor ).Location );
		if( id == -1 ) return;
		
		Destroy( warningSpheres[id] );
		warningSpheres.RemoveAt( id );
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

    public void Update()
    {
        if (Network.peerType == NetworkPeerType.Disconnected) return;
        if (Paused) return;

        if (networkView.isMine)
        {
            textBubbleVisible = ChatScript.Instance.showChat;

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
                lookRotationEuler += MouseSensitivityScript.Sensitivity * new Vector3(
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
            if (iPosition.IsRunning)
            {
                //Debug.Log("Before correct : " + transform.position);
                transform.position += iPosition.Update();
                //Debug.Log("After correct : " + transform.position);
            }

            smoothYaw = Mathf.LerpAngle(smoothYaw, lookRotationEuler.y, 0.4f);
            smoothLookRotation = Quaternion.Slerp(smoothLookRotation, Quaternion.Euler(lookRotationEuler), 0.3f);
        }

        // set up text bubble visibility
        if (!textBubbleVisible)
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
    }

    public void FixedUpdate()
    {
        if(!controller.enabled) return;
        if (Paused) return;

        Vector3 smoothedInputVelocity = inputVelocity * 0.6f + lastInputVelocity * 0.45f;
        lastInputVelocity = smoothedInputVelocity;

        // jump and dash
        dashCooldown -= Time.deltaTime;
	 	bool justJumped = false;
        if(networkView.isMine && Time.time - lastJumpInputTime <= JumpInputQueueTime)
        {
            if ((controller.isGrounded || sinceNotGrounded < 0.25f) && recoilVelocity.y <= 0)
            {
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

                var dashDirection = inputVelocity.normalized;
                if (dashDirection == Vector3.zero)
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
        controller.Move((smoothFallingVelocity + smoothedInputVelocity + recoilVelocity) * Time.deltaTime);

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
        var pOwner = owner.HasValue ? owner.Value : default(NetworkPlayer);
        stream.Serialize(ref pOwner);
        if (stream.isReading) owner = pOwner;

        Vector3 pPosition = stream.isWriting ? transform.position : Vector3.zero;

        stream.Serialize(ref pPosition);
        stream.Serialize(ref inputVelocity);
        stream.Serialize(ref fallingVelocity);
        stream.Serialize(ref activelyJumping);
        stream.Serialize(ref recoilVelocity);
        stream.Serialize(ref textBubbleVisible);
        stream.Serialize(ref playDashSound);
        stream.Serialize(ref playJumpSound);
        stream.Serialize(ref lookRotationEuler);

        if (stream.isReading)
        {
            //Debug.Log("pPosition = " + pPosition + " / transform.position = " + transform.position);
            if (lastNetworkFramePosition == pPosition)
                transform.position = pPosition;

            if (!iPosition.Start(pPosition - transform.position))
                transform.position = pPosition;

            if (playDashSound && GlobalSoundsScript.soundEnabled) dashSound.Play();
            if (playJumpSound && GlobalSoundsScript.soundEnabled) jumpSound.Play();

            lastNetworkFramePosition = pPosition;
        }

        playJumpSound = playDashSound = false;
    }

    public void OnDestroy( )
    {
        Network.RemoveRPCs( networkView.viewID );
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
class QuaternionInterpolator : Interpolator<Quaternion>
{
    public override bool Start(Quaternion delta)
    {
        IsRunning = !Mathf.Approximately(
            Quaternion.Angle(delta, Quaternion.identity), 0);
        //if (IsRunning)
        //    Debug.Log("quaternion interpolator started, angle == " +
        //    Quaternion.Angle(delta, Quaternion.identity));
        SinceStarted = 0;
        Delta = delta;
        return IsRunning;
    }
    public override Quaternion Update()
    {
        UpdateInternal();
        if (!IsRunning) return Quaternion.identity;
        return Quaternion.Slerp(
            Quaternion.identity, Delta, Time.deltaTime / InterpolationTime);
    }
}
