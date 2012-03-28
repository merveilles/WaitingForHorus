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
            Rect position = new Rect(
                Screen.width/2 - crosshair.width/2,
                Screen.height/2 - crosshair.width/2,
                crosshair.width,
                crosshair.height);
            GUI.DrawTexture(position, crosshair);
        }
    }

    public Vector3 GetTargetPosition()
    {
        RaycastHit hitInfo;
        if(Physics.Raycast(transform.position, transform.forward, out hitInfo))
            return hitInfo.point;
        else
            return transform.position + transform.forward * 1000;
    }
}
