using System;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Texture2D crosshair;
    public float collisionRadius = 0.7f;
    public float minDistance = 1;

    PlayerScript player;

    Camera mainCamera;

    void Awake()
    {
        player = transform.parent.parent.GetComponent<PlayerScript>();
        if(player.networkView.isMine)
        {
            mainCamera = Camera.main;
        }
    }

    void LateUpdate()
    {
        if(player.networkView.isMine)
        {
            Vector3 cameraPosition = transform.position;

            Vector3 direction = transform.position - player.transform.position;
            float magnitude = direction.magnitude;
            direction /= magnitude;

            RaycastHit hitInfo;
            if(Physics.SphereCast(player.transform.position, collisionRadius,
                                  direction, out hitInfo, magnitude))
            {
                cameraPosition = player.transform.position +
                    direction * Mathf.Max(minDistance, hitInfo.distance);
            }

            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.rotation = transform.rotation;
        }
    }

    void OnGUI()
    {
        if(player.networkView.isMine)
        {
            var scale = Screen.height / 1750f;

            Rect position = new Rect(
                Screen.width / 2 - crosshair.width / 2f * scale,
                Screen.height / 2 - crosshair.width / 2f * scale,
                crosshair.width * scale,
                crosshair.height * scale);

            RaycastHit hitInfo;

            player.gameObject.FindChild("PlayerHit").collider.enabled = false;

            var aimingAtPlayer = Physics.Raycast(transform.position, transform.forward, out hitInfo,
                                                 Mathf.Infinity, (1 << LayerMask.NameToLayer("Default")) |
                                                                 (1 << LayerMask.NameToLayer("Player Hit"))) &&
                                 hitInfo.transform.gameObject.layer == LayerMask.NameToLayer("Player Hit");

            if (aimingAtPlayer)
                GUI.color = Color.red;
            GUI.DrawTexture(position, crosshair);
            if (aimingAtPlayer)
                GUI.color = Color.white;

            player.gameObject.FindChild("PlayerHit").collider.enabled = true;
        }
    }

    public Vector3 GetTargetPosition()
    {
        RaycastHit hitInfo;
        if(Physics.Raycast(transform.position, transform.forward, out hitInfo,
                           Mathf.Infinity, 1<<LayerMask.NameToLayer("Default")))
            return hitInfo.point;
        else
            return transform.position + transform.forward * 1000;
    }
}
