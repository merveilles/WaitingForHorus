using UnityEngine;

public class PlayerShootingScript : MonoBehaviour
{
	const int BurstCount = 8;
	const float ShotCooldown = 0.045f;
	const float ReloadTime = 0.6f;
    const float BurstSpread = 1.5f;
    const float ShotgunSpread = 10;
    const float CannonChargeTime = 1.0f;

    public AudioSource reloadSound;

    public BulletScript bulletPrefab;
    public BulletScript cannonBulletPrefab;
    public Transform gun;

    public Texture2D cannonIndicator;
    public AnimationCurve cannonOuterScale;
    public AnimationCurve cannonInnerScale;

	float cooldownLeft = 0;
    int bulletsLeft = BurstCount;

    bool cannonCharging = false;
    float cannonChargeCountdown = CannonChargeTime;

    CameraScript playerCamera;

    void Awake()
    {
        playerCamera = GetComponentInChildren<CameraScript>();
    }

    void Update()
    {
        gun.LookAt(playerCamera.GetTargetPosition());
    }

    void DrawCannonIndicator(float scale)
    {
        Rect position = new Rect(
            Screen.width/2 - cannonIndicator.width/2*scale,
            Screen.height/2 - cannonIndicator.width/2*scale,
            cannonIndicator.width*scale,
            cannonIndicator.height*scale);
        GUI.DrawTexture(position, cannonIndicator);
    }

    void OnGUI()
    {
        if (networkView.isMine)
        {
            if(cannonCharging)
            {
                float t = (1 - cannonChargeCountdown/CannonChargeTime);
                DrawCannonIndicator(cannonOuterScale.Evaluate(t));
                DrawCannonIndicator(cannonInnerScale.Evaluate(t));
            }
        }
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
                    if(Input.GetButtonDown("Alternate Fire"))
                    {
                        cannonCharging = true;
                        cannonChargeCountdown = CannonChargeTime;
                    }

                    if(cannonCharging &&
                       Input.GetButton("Alternate Fire")) // cannon shot
                    {
                        cannonChargeCountdown -= Time.deltaTime;
                        if(cannonChargeCountdown <= 0)
                        {
                            DoCannonShot();
                            cooldownLeft += ShotCooldown * BurstCount;
                            cannonChargeCountdown = CannonChargeTime;
                            cannonCharging = false;
                        }
                    }

                    if(cannonCharging && Input.GetButtonUp("Alternate Fire"))
                    {
                        if((CannonChargeTime-cannonChargeCountdown) <= 0.1f)
                        {
                            while(bulletsLeft > 0)
                            {
                                DoShot(ShotgunSpread);
                                cooldownLeft += ShotCooldown;
                            }
                        }
                    }

                    if(cannonCharging && !Input.GetButton("Alternate Fire"))
                    {
                        cannonCharging = false;
                    }

                    if(cooldownLeft == 0 && Input.GetButton("Fire")) // burst fire
                    {
                        DoShot(BurstSpread);
                        cooldownLeft += ShotCooldown;
                    }
                }
            }
        }
    }

    void DoShot(float spread)
    {
        bulletsLeft -= 1;

        float roll = Random.value * 360;
        Quaternion spreadRotation =
            Quaternion.Euler(0, 0, roll) *
            Quaternion.Euler(Random.value * spread, 0, 0) *
            Quaternion.Euler(0, 0, -roll);
        networkView.RPC("Shoot", RPCMode.All,
            gun.position + gun.forward, gun.rotation * spreadRotation,
            Network.player);
    }

    void DoCannonShot()
    {
        bulletsLeft = 0;
        HealthScript health = GetComponent<HealthScript>();
        health.networkView.RPC(
            "DoDamage", RPCMode.All, health.Shield, Network.player);

        networkView.RPC("ShootCannon", RPCMode.All,
            gun.position + gun.forward, gun.rotation, Network.player);
    }

    [RPC]
    void Shoot(Vector3 position, Quaternion rotation, NetworkPlayer player)
    {
        BulletScript bullet =
            (BulletScript)Instantiate(bulletPrefab, position, rotation);
        bullet.Player = player;
    }

    [RPC]
    void ShootCannon(Vector3 position, Quaternion rotation, NetworkPlayer player)
    {
        BulletScript bullet =
            (BulletScript)Instantiate(cannonBulletPrefab, position, rotation);
        bullet.Player = player;
    }
}
