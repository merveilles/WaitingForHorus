using System;
using UnityEngine;

public class PlayerShootingScript : MonoBehaviour
{
	const int BurstCount = 8;
	const float BurstCooldown = 0.6f;
	const float ShotCooldown = 0.045f;

	float sinceLastShot;
	int shotsInARow;

    public BulletScript bulletPrefab;
    public Transform gun;
	
    CameraScript playerCamera;

    void Awake()
    {
        playerCamera = GetComponentInChildren<CameraScript>();
    }

    void Update()
    {
        gun.LookAt(playerCamera.GetTargetPosition());

        if (networkView.isMine && Screen.lockCursor)
		{
			sinceLastShot -= Time.deltaTime;
			
			if (Input.GetButton("Fire"))
			{
				if (sinceLastShot <= 0)
				{
					shotsInARow++;

                    networkView.RPC("Shoot", RPCMode.All,
                        gun.position + gun.forward, gun.rotation, Network.player);
					if (shotsInARow >= BurstCount)
					{	
						sinceLastShot = BurstCooldown;
						shotsInARow = 0;
					}
					else
						sinceLastShot = ShotCooldown;
				}
			}
			else if (sinceLastShot <= 0)
				shotsInARow = Mathf.Max(shotsInARow - 1, 0);
        }
    }

    [RPC]
    void Shoot(Vector3 position, Quaternion rotation, NetworkPlayer player)
    {
        BulletScript bullet =
            (BulletScript)Instantiate(bulletPrefab, position, rotation);
        bullet.Player = player;
    }
}
