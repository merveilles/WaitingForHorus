using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Texture2D crosshair;
    public float collisionRadius = 0.7f;
    public float minDistance = 1;
    public float smoothing = 0.1f;

    public bool HasSmoothedRotation = true;
    public bool UsesRaycastCrosshair = true;
    public float CrosshairSmoothingSpeed = 20.0f;
	
	bool aimingAtPlayer;
    PlayerScript player;

    Camera mainCamera;

    Quaternion actualCameraRotation;

    // Used only for drawing the crosshair on screen. Actual aiming raycast will
	// not use this.
    private Vector2 SmoothedCrosshairPosition;

    public void Start()
    {
        player = transform.parent.parent.GetComponent<PlayerScript>();
        if(player.networkView.isMine)
        {
            mainCamera = Camera.main;
        }
        SmoothedCrosshairPosition = GetCrosshairPosition();
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

            if (HasSmoothedRotation)
                actualCameraRotation = Quaternion.Lerp(transform.rotation, actualCameraRotation,
                    Easing.EaseOut(Mathf.Pow(smoothing, Time.deltaTime), EasingType.Quadratic));
            else
                actualCameraRotation = transform.rotation;

            Vector3 scaledLocalPosition = Vector3.Scale(transform.localPosition, transform.lossyScale);
            Vector3 direction = actualCameraRotation * scaledLocalPosition;
            Vector3 cameraPosition = transform.parent.position + direction;
            float magnitude = direction.magnitude;
            // TODO what is this assignment? Should it be above? is it a mistake?
            direction /= magnitude;

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
            SmoothedCrosshairPosition = Vector2.Lerp(SmoothedCrosshairPosition, rawCrosshairPosition, Time.deltaTime * CrosshairSmoothingSpeed);
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
