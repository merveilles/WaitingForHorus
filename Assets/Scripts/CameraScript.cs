using System;
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
	
	bool aimingAtPlayer;
    PlayerScript player;

    Camera mainCamera;

    Quaternion actualCameraRotation;

    public bool IsZoomedIn { get; set; }

    public float BaseFieldOfView = 85.0f;
    public float ZoomedFieldOfViewRatio = 0.42f;

    private float SmoothedBaseFieldOfView = 85.0f;
    private float SmoothedFieldOfView = 85.0f;
    public float DesiredFieldOfView
    {
        get { return SmoothedBaseFieldOfView * (IsZoomedIn ? ZoomedFieldOfViewRatio : 1.0f); }
    }

    public bool IsExteriorView { get; set; }
    public Vector3 ExteriorViewOffset = new Vector3(0f, 2.5f, -6f);
    public Vector3 InteriorViewOffset = new Vector3(0f, 1.0f, 0f);
    private Vector3 SmoothedViewOffset = new Vector3(0f, 2.5f, -6f);

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

    public void Start()
    {
        IsExteriorView = true;

        player = transform.parent.parent.GetComponent<PlayerScript>();
        if(player.networkView.isMine)
        {
            mainCamera = Camera.main;
        }
        SmoothedCrosshairPosition = GetCrosshairPosition();

        // Grab whatever the camera currently has. We'll remove 'LocalPlayer' from it when in interior view.
        InitialCameraCullingMask = mainCamera.cullingMask;
        InitialCameraNearClipPlane = mainCamera.nearClipPlane;
    }

    public void Update()
    {
        if (Input.GetButtonDown("DecreaseFOV"))
        {
            BaseFieldOfView -= 5.0f;
        }
        if (Input.GetButtonDown("IncreaseFOV"))
        {
            BaseFieldOfView += 5.0f;
        }
        if (Input.GetKeyDown("x"))
        {
            IsExteriorView = !IsExteriorView;

            if (IsExteriorView)
            {
                mainCamera.cullingMask = InitialCameraCullingMask;
                mainCamera.nearClipPlane = InitialCameraNearClipPlane;
            }
            else
            {
                mainCamera.cullingMask = InitialCameraCullingMask ^ LayerMask.GetMask("LocalPlayer");
                mainCamera.nearClipPlane = InitialCameraNearClipPlane / 2.0f;
            }
        }

        // Prevent crazy values
        BaseFieldOfView = Mathf.Clamp(BaseFieldOfView, 45f, 150f);

        IsZoomedIn = Input.GetButton("Zoom");

    }

    public void FixedUpdate()
	{
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
        if (player.Paused && mainCamera != null)
        {
            mainCamera.transform.localPosition = new Vector3(-85.77416f, 32.8305f, -69.88891f);
            mainCamera.transform.localRotation = Quaternion.Euler(16.48679f, 21.83607f, 6.487632f);
            return;
        }

        if(player.networkView.isMine)
        {
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

            if (HasSmoothedRotation)
                // Old, not working with uncapped frame limit, but left for
                // reference in case someone wants to match its settings exactly
                // in the future.
                //actualCameraRotation = Quaternion.Lerp(transform.rotation, actualCameraRotation,
                //Easing.EaseOut(Mathf.Pow(smoothing, Time.deltaTime), EasingType.Quadratic));

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
            //float magnitude = direction.magnitude;
            // TODO what is this assignment? Should it be above? is it a mistake?
            //direction /= magnitude;

           /* RaycastHit hitInfo;
            if(Physics.SphereCast(player.transform.position, collisionRadius,
                                  direction, out hitInfo, magnitude))
            {
                cameraPosition = player.transform.position +
                    direction * Mathf.Max(minDistance, hitInfo.distance);
            }*/

            /*var distance = Vector3.Distance(cameraPosition, player.transform.position);
            var o = Mathf.Clamp01((distance - 2) / 8);
            foreach (var r in player.GetComponentsInChildren<Renderer>())
            {
                if (!r.material.HasProperty("_Color")) continue;
                if (r.gameObject.name == "TextBubble") continue;
                var c = r.material.color;
                r.material.color = new Color(c.r, c.g, c.b, o);
            }*/

            // TODO can mainCamera be null here?
            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.rotation = actualCameraRotation;


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
        if (UsesRaycastCrosshair)
        {
            position = transform.position;
            forward = transform.forward;
        }
        else
        {
            position = mainCamera.transform.position;
            forward = mainCamera.transform.forward;
        }
        if(Physics.Raycast(position, forward, out hitInfo,
                           Mathf.Infinity, 1<<LayerMask.NameToLayer("Default")))
            return hitInfo.point;
        else
            return transform.position + transform.forward * 1000;
    }
}
