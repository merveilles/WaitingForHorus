using System;
using Cancel.Interpolation;
using Cancel.RateLimit;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Texture2D crosshair;
    public float collisionRadius = 0.7f;
    public float minDistance = 1;
    public float smoothing = 0.1f;

    public bool HasSmoothedRotation = true;
    public bool UsesRaycastCrosshair = true;
    public float CrosshairSmoothingSpeed = 8.5f;

    public const float MinimumFieldOfView = 45.0f;
    public const float MaximumFieldOfView = 150.0f;
	
	bool aimingAtPlayer;
    public PlayerScript player;

    Camera mainCamera;

    public Camera CameraUsed { get { return mainCamera; } }

    Quaternion actualCameraRotation;

    public GameObject[] ObjectsToHideInFirstPerson;
    // Used to disable shadows on stuff like the gun until we're able to draw
	// shadows for everything, because a floating gun's shadow looks silly.
    public GameObject[] HackDisableShadowsObjects;

    public Vector3 LookingDirection
    {
        get { return transform.rotation * Vector3.forward; }
    }

    public bool IsZoomedIn { get; private set; }

    public delegate void CameraIsZoomedChangedHandler(bool isZoomed);
    // Invoked when the camera becomes or un-becomes zoomed.
    public event CameraIsZoomedChangedHandler OnCameraIsZoomedChanged = delegate { };

    private float _BaseFieldOfView;

    public float BaseFieldOfView
    {
        get
        {
            return _BaseFieldOfView;
        }
        set
        {
            _BaseFieldOfView = value;
        }
    }

    // 0 .. 1
    public float ZoomedAmount
    {
        get
        {
            float min = ZoomedFieldOfViewRatio * SmoothedBaseFieldOfView;
            float max = SmoothedBaseFieldOfView;
            float current = SmoothedFieldOfView;
            float amt = (current - min) / (max - min);
            if (amt < 0.00001f)
                amt = 0f;
            else if (amt > 0.9999f)
                amt = 1f;
            return 1 - amt;
        }
    }

    public const float DefaultBaseFieldOfView = 85.0f;
    public float ZoomedFieldOfViewRatio = 0.42f;

    private float SmoothedBaseFieldOfView = 85.0f;
    private float SmoothedFieldOfView = 85.0f;

    // Looks better if there is some 'travel time' between the shot and the impulse reaching the 'camera'.
    private Delayer<Vector3> QueuedScreenRecoils;
    private ThrottledRotationalSpring CosmeticSpring;

    // Used to calculate inferred velocity
    private Vector3 LastInferredBodyPosition;
    private Vector3 LastInferredVelocity;
    private ScalarSpring YSpring;
    private RotationalSpring ViewBobSpring;

    public float DesiredFieldOfView
    {
        get { return SmoothedBaseFieldOfView * (IsZoomedIn ? ZoomedFieldOfViewRatio : 1.0f); }
    }

    // Indicates whether the camera is in third person (exterior) or first
	// person (interior) view.
    private bool _IsExteriorView;

    public bool IsExteriorView
    {
        get
        {
            return _IsExteriorView;
        }
        set
        {
            if (_IsExteriorView != value)
            {
                _IsExteriorView = value;
                // Invoke listeners
                OnCameraIsExteriorChanged(IsExteriorView);
                // Update which objects we want to be hidden via layers
                UpdateCameraObjectVisibiliy();
            }
        }
    }

    public Vector3 ExteriorViewOffset = new Vector3(0f, 2.5f, -6f);
    public Vector3 InteriorViewOffset = new Vector3(0f, 1.0f, 0f);
    private Vector3 SmoothedViewOffset = new Vector3(0f, 2.5f, -6f);

    public delegate void CameraIsExteriorChangedHandler(bool isExteriorView);
    // Invoked when the camera mode changes
    public event CameraIsExteriorChangedHandler OnCameraIsExteriorChanged = delegate {};

    private Vector3 DesiredViewOffset
    {
        get { return IsExteriorView ? ExteriorViewOffset : InteriorViewOffset; }
    }

    // We're going to cache some initial values from the camera, because we're
	// going to modify them but also need to restore the originals.
    private int InitialCameraCullingMask;
    private float InitialCameraNearClipPlane;

    // Used only for drawing the crosshair on screen. Actual aiming raycast will
	// not use this.
    private Vector2 SmoothedCrosshairPosition;

    // Used to cosmetically update barrel position late into Update cycle (keep in sync with first-person camera).
    public Transform BarrelFirstPersonOffsetTransform;

    // Generally ~ 0.5 - 1.5
    public void AddGunShotImpulse(float amount)
    {
        // Add to the queue instead of directly into spring, because we want to
        // delay recoil before it 'hits' the camera. Will be put into the actual
		// spring in LateUpdate().
        QueuedScreenRecoils.Add(CalculateGunShotImpulse(amount));
    }

    public Vector3 CalculateGunShotImpulse(float amount)
    {
        float verticalBase = amount * 1500;
        float verticalExtra = UnityEngine.Random.Range(150, 400) * amount;
        float lateralSign = Mathf.Sign(UnityEngine.Random.Range(-1f, 1f));
        float lateral = amount * UnityEngine.Random.Range(100f, 500f) * lateralSign;
        float roll = amount * UnityEngine.Random.Range(-100f, 100f);
        return new Vector3(-(verticalBase + verticalExtra), lateral, roll);
    }

    public void Awake()
    {
        if (HackDisableShadowsObjects == null)
            HackDisableShadowsObjects = new GameObject[0];

        QueuedScreenRecoils = new Delayer<Vector3>();

        CosmeticSpring = new ThrottledRotationalSpring(Quaternion.identity);
        CosmeticSpring.Damping = 0.0000001f;
        CosmeticSpring.Strength = 900f;
        CosmeticSpring.ImpulseQueueLimit = 1;

        Relay.Instance.OptionsMenu.OnFOVOptionChanged += ReceiveFOVChanged;
        BaseFieldOfView = Relay.Instance.OptionsMenu.FOVOptionValue;
        Relay.Instance.OptionsMenu.OnExteriorViewOptionChanged += ReceiveExteriorViewOptionChanged;

        // Used for view bob and jump/landing etc
        YSpring = new ScalarSpring(0f);
        YSpring.Strength = 800f;
        YSpring.Damping = 0.000000000001f;

        ViewBobSpring = new RotationalSpring(Quaternion.identity);
        ViewBobSpring.Strength = 500f;
        ViewBobSpring.Damping = 0.0000001f;
    }

    public void OnDestroy()
    {
        Relay.Instance.OptionsMenu.OnFOVOptionChanged -= ReceiveFOVChanged;
        Relay.Instance.OptionsMenu.OnExteriorViewOptionChanged -= ReceiveExteriorViewOptionChanged;
    }

    private void ReceiveFOVChanged(float fov)
    {
        BaseFieldOfView = fov;
    }

    public void AdjustCameraFOVInstantly()
    {
        SmoothedBaseFieldOfView = _BaseFieldOfView;
        SmoothedFieldOfView = _BaseFieldOfView;
    }

    public void AddYSpringImpulse(float impulse)
    {
        // TODO should probably apply external view modifier when applying the rotation, not here, oh well.
        float scale = IsExteriorView ? 0.75f : 1.0f;
        YSpring.AddImpulse(-impulse * 18.2f * scale);
        Vector3 shotImpulse = CalculateGunShotImpulse(impulse * 0.0072f * scale);
        // Reduce amount of yaw
        shotImpulse.y *= 0.2f;
        ViewBobSpring.AddImpulse(shotImpulse);
    }

    public void Start()
    {
        if(player.networkView.isMine)
        {
            mainCamera = Camera.main;
        }
        SmoothedCrosshairPosition = GetCrosshairPosition();

        // We should use a better way to get these values.
        InitialCameraCullingMask = -1;
        InitialCameraNearClipPlane = 1f;

        UpdateCameraObjectVisibiliy();

        // More hacks
        if (player.networkView.isMine)
        {
            int layerID = LayerMask.NameToLayer("LocalPlayer");
            foreach (var objectToHide in ObjectsToHideInFirstPerson)
            {
                objectToHide.layer = layerID;
            }
        }

        LastInferredBodyPosition = player.transform.position;
        LastInferredVelocity = Vector3.zero;
    }

    public void Update()
    {
        if (!mainCamera) return;
        if (!player.networkView.isMine) return;

        if (Input.GetButtonDown("DecreaseFOV"))
        {
            Relay.Instance.OptionsMenu.FOVOptionValue -= 5.0f;
        }
        if (Input.GetButtonDown("IncreaseFOV"))
        {
            Relay.Instance.OptionsMenu.FOVOptionValue += 5.0f;
        }
        if (Input.GetKeyDown("x"))
        {
            IsExteriorView = !IsExteriorView;

            // TODO Hack, I'm feeling lazy right now
            if (player.Possessor != null)
            {
                player.Possessor.WantsExteriorView = IsExteriorView;
            }
        }

        // Prevent crazy values
        BaseFieldOfView = Mathf.Clamp(BaseFieldOfView, MinimumFieldOfView, MaximumFieldOfView);

        // Handle zoom changing
        bool newZoom = Input.GetButton("Zoom");
        // Not just assigning blindly since we don't want to spam the event if
		// it hasn't changed.
        if (newZoom != IsZoomedIn)
        {
            IsZoomedIn = newZoom;
            // Notify listeners of zoom change
            OnCameraIsZoomedChanged(IsZoomedIn);
        }

    }

    // Hide or show objects based on what camera mode we're in. Uses layer masks.
    private void UpdateCameraObjectVisibiliy()
    {
        if (!mainCamera) return;
        if (IsExteriorView)
        {
            mainCamera.cullingMask = InitialCameraCullingMask;
            mainCamera.nearClipPlane = InitialCameraNearClipPlane;

            foreach (var hackDisableShadowsObject in HackDisableShadowsObjects)
            {
                hackDisableShadowsObject.renderer.castShadows = true;
            }
        }
        else
        {
            mainCamera.cullingMask = InitialCameraCullingMask ^ (1 << LayerMask.NameToLayer("LocalPlayer"));
            mainCamera.nearClipPlane = InitialCameraNearClipPlane / 2.0f;

            foreach (var hackDisableShadowsObject in HackDisableShadowsObjects)
            {
                hackDisableShadowsObject.renderer.castShadows = false;
            }
        }
    }

    public void FixedUpdate()
	{
        if (!mainCamera) return;
        RaycastHit hitInfo;

        player.gameObject.FindChild("PlayerHit").collider.enabled = false;

        aimingAtPlayer = Physics.Raycast(transform.position, transform.forward, out hitInfo,
                                             Mathf.Infinity, (1 << LayerMask.NameToLayer("Default")) |
                                                             (1 << LayerMask.NameToLayer("Player Hit"))) &&
                             hitInfo.transform.gameObject.layer == LayerMask.NameToLayer("Player Hit");

        player.gameObject.FindChild("PlayerHit").collider.enabled = true;	
	}

    public void LateUpdate()
    {
        if (!mainCamera) return;
        if (player.Paused && mainCamera != null)
        {
            mainCamera.transform.localPosition = new Vector3(-85.77416f, 32.8305f, -69.88891f);
            mainCamera.transform.localRotation = Quaternion.Euler(16.48679f, 21.83607f, 6.487632f);
            return;
        }

        if(player.networkView.isMine)
        {
            // Update inferred position and velocity
            Vector3 newPosition = player.transform.position;
            Vector3 newVelocity = (newPosition - LastInferredBodyPosition) / Time.deltaTime;
            Vector3 velocityDelta = newVelocity - LastInferredVelocity;
            LastInferredVelocity = newVelocity;
            LastInferredBodyPosition = newPosition;
            AddYSpringImpulse(velocityDelta.y);
            YSpring.Update();
            // Clamp magnitude to prevent camera from clipping through ground on hard landings
            YSpring.CurrentValue = Mathf.Clamp(YSpring.CurrentValue, -4f, 4f);
            if (IsExteriorView)
                BarrelFirstPersonOffsetTransform.localPosition = Vector3.zero;
            else
                // Values between 0.3f and 0.8f seem to look best for scaling the gun offset Y.
                BarrelFirstPersonOffsetTransform.localPosition = new Vector3(0f, YSpring.CurrentValue * 0.3f, 0f);

            ViewBobSpring.Update();

            // Higher delay time when the camera is further from the gun
            QueuedScreenRecoils.DelayTime = IsExteriorView ? 0.071f : 0.048f;
            //QueuedScreenRecoils.DelayTime = 0.06f;
            // Remove stuff from the lag queue and put it in the actual spring
            foreach (var eulerAngles in QueuedScreenRecoils.Update())
                CosmeticSpring.AddImpulse(eulerAngles);
            CosmeticSpring.Update();

            if (Input.GetButtonDown("ToggleCameraSmoothing"))
            {
                HasSmoothedRotation = !HasSmoothedRotation;
            }
            if (Input.GetButtonDown("ToggleRaycastCrosshair"))
            {
                UsesRaycastCrosshair = !UsesRaycastCrosshair;
            }

            // Smooth changes in base field of view (if player adjusts FOV, we
			// want it to be a slower smoothing than zooming in/out)
            SmoothedBaseFieldOfView = Mathf.Lerp(SmoothedBaseFieldOfView, BaseFieldOfView,
                1.0f - Mathf.Pow(0.0001f, Time.deltaTime));
            // Interpolate field of view, set on main camera if necessary
            SmoothedFieldOfView = Mathf.Lerp(SmoothedFieldOfView, DesiredFieldOfView,
                1.0f - Mathf.Pow(0.000001f, Time.deltaTime));
            if (mainCamera != null)
            {
                if (!Mathf.Approximately(mainCamera.fieldOfView, SmoothedFieldOfView))
                    mainCamera.fieldOfView = SmoothedFieldOfView;
            }

            // Update and smooth view position
            SmoothedViewOffset = Vector3.Lerp(SmoothedViewOffset, DesiredViewOffset,
                1.0f - Mathf.Pow(0.00001f, Time.deltaTime));
            transform.localPosition = SmoothedViewOffset;

            // TODO we need smarter handling for toggilng smoothing and first/third person at the same time
            if (HasSmoothedRotation && IsExteriorView)
            {
                // TODO make a nicer interface for goofy power curve
                var amt = (float)Math.Pow(0.0000000000001, Time.deltaTime);
                actualCameraRotation = Quaternion.Slerp(actualCameraRotation, transform.rotation, 1.0f - amt);
            }
            else
                actualCameraRotation = transform.rotation;

            Vector3 scaledLocalPosition = Vector3.Scale(transform.localPosition, transform.lossyScale);
            Vector3 direction = actualCameraRotation * scaledLocalPosition;
            Vector3 cameraPosition = transform.parent.position + direction;

            // Modify Y for spring
            cameraPosition.y += YSpring.CurrentValue;

            // We don't want to use view bob when zoomed in: we want the
			// reticule to be very responsive.
            Quaternion usedViewBob = Quaternion.Lerp(
                ViewBobSpring.CurrentValue, Quaternion.identity, ZoomedAmount);

            // TODO can mainCamera be null here?
            if (mainCamera != null)
            {
                mainCamera.transform.position = cameraPosition;
                mainCamera.transform.rotation =
                    actualCameraRotation *
                    usedViewBob *
                    CosmeticSpring.CurrentValue;
            }


            var rawCrosshairPosition = GetCrosshairPosition();
            SmoothedCrosshairPosition = Vector2.Lerp(SmoothedCrosshairPosition, rawCrosshairPosition,
                1.0f - Mathf.Pow(CrosshairSmoothingSpeed, -CrosshairSmoothingSpeed * Time.deltaTime));
            Camera.main.GetComponent<WeaponIndicatorScript>()
                .CrosshairPosition = SmoothedCrosshairPosition;
        }
    }
	
	void Render( float size, Color color )
	{
        var scale = ( Screen.height / 1750f ) * size;

	    Vector2 center = SmoothedCrosshairPosition;
        Rect position = new Rect(
            center.x - crosshair.width / 2f * scale,
            Screen.height - center.y - crosshair.height / 2f * scale,
            crosshair.width * scale,
            crosshair.height * scale);
		
		GUI.color = color;
		GUI.DrawTexture(position, crosshair);
	}

    public void OnGUI()
    {
        if(player.networkView.isMine)
        {
			var color = Color.white;
	        if( aimingAtPlayer )
	            GUI.color = Color.red;
			Render( 1.0f, color );
			Render( 0.75f, Color.black );
        }
    }

    public Vector2 GetCrosshairPosition()
    {
        return Camera.main.WorldToScreenPoint(GetTargetPosition());
    }

    public Vector3 GetTargetPosition()
    {
        RaycastHit hitInfo;
        Vector3 position, forward;
        // TODO we need smarter handling for toggilng raycast crosshair and first/third person at the same time
        if ((UsesRaycastCrosshair && IsExteriorView) || mainCamera == null)
        {
            position = transform.position;
            forward = transform.forward;
        }
        else
        {
            position = mainCamera.transform.position;
            forward = actualCameraRotation * Vector3.forward;
        }
        if(Physics.Raycast(position, forward, out hitInfo,
                           Mathf.Infinity, 1<<LayerMask.NameToLayer("Default")))
            return hitInfo.point;
        else
            return transform.position + transform.forward * 1000;
    }

    private void ReceiveExteriorViewOptionChanged(bool newIsExterior)
    {
        IsExteriorView = newIsExterior;
    }
}
