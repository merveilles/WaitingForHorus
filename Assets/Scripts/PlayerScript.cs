using System;
using UnityEngine;

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

    public Transform cameraPivot;
    public Transform dashEffectPivot;
    public Renderer dashEffectRenderer;

    CharacterController controller;
    Vector3 fallingVelocity;
	bool invertMouse = true;
    Vector3 inputVelocity;
    Vector3 lastInputVelocity;
    Vector3 lookRotationEuler;
    float lastJumpInputTime = -1;
    float dashCooldown = 0;
    Animation characterAnimation;
    bool isRunning;
    public NetworkPlayer? owner;
    float sinceNotGrounded;
    bool activelyJumping;

    public bool Paused { get; set; }

    // for interpolation on remote computers only
    VectorInterpolator iPosition;
	
	void Awake() 
	{
        controller = GetComponent<CharacterController>();
        characterAnimation = transform.Find("Graphics").animation;
        characterAnimation.AddClip(characterAnimation.GetClip("Run"), "Run", 0, 20, true);
        //characterAnimation.AddClip(characterAnimation.GetClip("Idle"), "Idle", 0, 20, true);
        characterAnimation.Play("Idle");
	}

    void OnNetworkInstantiate(NetworkMessageInfo info)
    {
        if (!networkView.isMine)
            iPosition = new VectorInterpolator();
        else
            owner = networkView.owner;
    }

    void OnGUI()
    {
        if(Event.current.type == EventType.KeyDown &&
           Event.current.keyCode == KeyCode.Escape)
        {
            Screen.lockCursor = false;
        }
    }

    void Update()
    {
        if (Network.peerType == NetworkPeerType.Disconnected) return;
        if (Paused) return;

        if (networkView.isMine)
        {
            inputVelocity =
                Input.GetAxisRaw("Strafe") * transform.right +
                Input.GetAxisRaw("Thrust") * transform.forward;
            if(inputVelocity.sqrMagnitude > 1)
                inputVelocity.Normalize();

            inputVelocity *= speed;

            if (Input.GetButtonDown("Jump"))
            {
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
                lookRotationEuler += new Vector3(
                    Input.GetAxis("Vertical Look") * mouseSensitivity * invertMultiplier,
                    Input.GetAxis("Horizontal Look") * mouseSensitivity,
                    0);
			}
			
			lookRotationEuler.x = Mathf.Clamp(
                lookRotationEuler.x, -lookAngleLimit, lookAngleLimit);

			if (Input.GetKeyDown("i"))
				invertMouse = !invertMouse;

            if (Input.GetMouseButtonUp(0))
                Screen.lockCursor = true;

            Screen.showCursor = !Screen.lockCursor;
		}
        else
        {
            if (iPosition.IsRunning) transform.position += iPosition.Update();
        }

        // sync up actual player and camera transforms
        Vector3 euler = transform.rotation.eulerAngles;
        euler.y = lookRotationEuler.y;
        transform.rotation = Quaternion.Euler(euler);
        cameraPivot.rotation = Quaternion.Euler(lookRotationEuler);

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

        PlayerRegistry.PlayerInfo info;
        if (PlayerRegistry.For.TryGetValue(owner.Value, out info))
        {
            transform.Find("Graphics").Find("mecha_flag").Find("flag_flag").renderer.material.color = info.Color;

            if (!networkView.isMine)
                GetComponentInChildren<TextMesh>().text = info.Username;
        }
    }

    void FixedUpdate()
    {
        if(!controller.enabled) return;
        if (Paused) return;

        Vector3 smoothedInputVelocity = (lastInputVelocity + inputVelocity)/2;
        lastInputVelocity = inputVelocity;

        // jump and dash
        dashCooldown -= Time.deltaTime;
        bool justJumped = false;
        if(networkView.isMine && Time.time - lastJumpInputTime <= JumpInputQueueTime)
        {
            if (controller.isGrounded || sinceNotGrounded < 0.25f)
            {
                lastJumpInputTime = -1;
                justJumped = true;
                activelyJumping = true;
                fallingVelocity.y = jumpVelocity;
                characterAnimation.Play("Jump");
                sinceNotGrounded = 0.25f;
            }
            else if(dashCooldown <= 0)
            {
                activelyJumping = false;
                lastJumpInputTime = -1;
                dashCooldown = timeBetweenDashes;

                var dashDirection = smoothedInputVelocity.normalized;
                if (dashDirection == Vector3.zero)
                    dashDirection = Vector3.up * 0.35f;

                fallingVelocity +=
                    dashDirection * dashForwardVelocity +
                    Vector3.up * dashUpwardVelocity;
            }
        }

        if(controller.isGrounded)
        {
            if (!justJumped)
                sinceNotGrounded = 0;
            // infinite friction
            if(fallingVelocity.y <= 0)
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
        if (controller.isGrounded)
        {
            if (!MathHelper.Approximately(smoothedInputVelocity, Vector3.zero))
            {
                if (!isRunning)
                {
                    characterAnimation.CrossFade("Run");
                    isRunning = true;
                }
            }
            else if (isRunning || !characterAnimation.isPlaying)
            {
                characterAnimation.Play("Idle");
                isRunning = false;
            }
        }
        else
            isRunning = false;

        // move!
        controller.Move((fallingVelocity + smoothedInputVelocity) * Time.deltaTime);
    }

    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        var pOwner = owner.HasValue ? owner.Value : default(NetworkPlayer);
        stream.Serialize(ref pOwner);
        if (stream.isReading) owner = pOwner;

        Vector3 pPosition = stream.isWriting ? transform.position : Vector3.zero;

        stream.Serialize(ref pPosition);

        stream.Serialize(ref inputVelocity);
        stream.Serialize(ref fallingVelocity);
        stream.Serialize(ref activelyJumping);

        stream.Serialize(ref lookRotationEuler);

        if (stream.isReading)
        {
            if (!iPosition.Start(pPosition - transform.position))
                transform.position = pPosition;
        }
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
