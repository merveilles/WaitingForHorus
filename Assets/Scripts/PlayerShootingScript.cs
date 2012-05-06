using UnityEngine;

public class PlayerShootingScript : MonoBehaviour
{
	const int BurstCount = 8;
	const float ShotCooldown = 0.045f;
	const float ReloadTime = 0.6f;
    const float BurstSpread = 1.5f;
    const float ShotgunSpread = 10;

    public AudioSource reloadSound;

	float cooldownLeft = 0;
    int bulletsLeft = BurstCount;

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
    }

    void FixedUpdate()
    {
        if (networkView.isMine && Screen.lockCursor)
		{
			cooldownLeft = Mathf.Max(0, cooldownLeft - Time.deltaTime);

            if(cooldownLeft == 0)
            {
                if(bulletsLeft == 0) // reload delay
                {
                    bulletsLeft = BurstCount;
                    cooldownLeft += ReloadTime;
                    reloadSound.Play();
                }
                else
                {
                    if (Input.GetButton("Alternate Fire")) // shotgun shot
                    {
                        while(bulletsLeft > 0)
                            DoShot(ShotgunSpread);
                    }
                    else if (Input.GetButton("Fire")) // burst fire
                    {
                        DoShot(BurstSpread);
                    }
                }
            }
        }
    }

    void DoShot(float spread)
    {
        bulletsLeft -= 1;
        cooldownLeft += ShotCooldown;

        float roll = Random.value * 360;
        Quaternion spreadRotation =
            Quaternion.Euler(0, 0, roll) *
            Quaternion.Euler(Random.value * spread, 0, 0) *
            Quaternion.Euler(0, 0, -roll);
        networkView.RPC("Shoot", RPCMode.All,
            gun.position + gun.forward, gun.rotation * spreadRotation, Network.player);
    }

    [RPC]
    void Shoot(Vector3 position, Quaternion rotation, NetworkPlayer player)
    {
        BulletScript bullet =
            (BulletScript)Instantiate(bulletPrefab, position, rotation);
        bullet.Player = player;
    }
}
