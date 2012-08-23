using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class PlayerShootingScript : MonoBehaviour
{
    public const float AimingTime = 0.75f;

	const int BurstCount = 8;
	const float ShotCooldown = 0.045f;
	const float ReloadTime = 0.45f;
    const float BurstSpread = 1.5f;
    const float ShotgunSpread = 10;
    const float CannonChargeTime = 0.5f;

    public AudioSource reloadSound;

    public BulletScript bulletPrefab;
    public BulletScript cannonBulletPrefab;
    public Transform gun;

    public Texture2D cannonIndicator;
    public AnimationCurve cannonOuterScale;
    public AnimationCurve cannonInnerScale;

    Material mat;

	float cooldownLeft = 0;
    int bulletsLeft = BurstCount;

    float cannonChargeCountdown = CannonChargeTime;
    List<WeaponIndicatorScript.PlayerData> targets;

    CameraScript playerCamera;
    PlayerScript playerScript;

    void Awake()
    {
        playerCamera = GetComponentInChildren<CameraScript>();
        targets = Camera.main.GetComponent<WeaponIndicatorScript>().Targets;
        playerScript = GetComponent<PlayerScript>();
    }

    void Update()
    {
        gun.LookAt(playerCamera.GetTargetPosition());

        if (playerScript.Paused)
            bulletsLeft = BurstCount;
    }

    void FixedUpdate()
    {
        if (networkView.isMine && Screen.lockCursor && !playerScript.Paused)
		{
			cooldownLeft = Mathf.Max(0, cooldownLeft - Time.deltaTime);
            Camera.main.GetComponent<WeaponIndicatorScript>().CooldownStep = 1 - Math.Min(Math.Max(cooldownLeft - ShotCooldown, 0) / ReloadTime, 1);

		    if(cooldownLeft == 0)
            {
                // Shotgun
                if (Input.GetButton("Alternate Fire"))
                {
                    // find homing target(s)
                    var aimedAt = targets.Where(x => x.SinceInCrosshair >= AimingTime);

                    while (bulletsLeft > 0)
                    {
                        if (!aimedAt.Any())
                            DoHomingShot(ShotgunSpread, null, 0);
                        else
                        {
                            var chosen = aimedAt.OrderBy(x => Guid.NewGuid()).First();
                            DoHomingShot(ShotgunSpread, chosen.Script, Mathf.Clamp01(chosen.SinceInCrosshair / AimingTime));
                        }
                        cooldownLeft += ShotCooldown;
                    }
                    cooldownLeft += ReloadTime;

                    cannonChargeCountdown = CannonChargeTime;
                }

                // Burst
                else if (Input.GetButton("Fire")) // burst fire
                {
                    DoShot(BurstSpread);
                    cooldownLeft += ShotCooldown;
                    if (bulletsLeft <= 0)
                        cooldownLeft += ReloadTime;
                }
            }

            if (bulletsLeft <= 0) 
            {
                bulletsLeft = BurstCount;
                reloadSound.Play();
            }

		    var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            var allowedDistance = 130 * Screen.height / 1500f;

            foreach (var v in targets) v.Found = false;
            //Debug.Log(targets.Values.Count + " targets to find");

            // Test for players in crosshair
            foreach (var p in FindSceneObjectsOfType(typeof(PlayerScript)))
            {
                var ps = p as PlayerScript;

                if (p == gameObject.GetComponent<PlayerScript>())
                    continue;

                var health = ps.gameObject.GetComponent<HealthScript>();

                var position = ps.transform.position;
                var screenPos = Camera.main.WorldToScreenPoint(position);

                if (health.Health > 0 && (screenPos.XY() - screenCenter).magnitude < allowedDistance)
                {
                    WeaponIndicatorScript.PlayerData data;
                    if ((data = targets.FirstOrDefault(x => x.Script == ps)) == null)
                        targets.Add(data = new WeaponIndicatorScript.PlayerData { Script = ps });

                    data.ScreenPosition = screenPos.XY();
                    data.SinceInCrosshair += Time.deltaTime;
                    data.Found = true;

                    //Debug.Log("Found target at " + data.ScreenPosition);
                }
            }

            if (targets.Count > 0)
                targets.RemoveAll(x => x.Script == null || !x.Found);
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

    void DoHomingShot(float spread, PlayerScript target, float homing)
    {
        bulletsLeft -= 1;

        spread *= (1 + homing * 2);

        float roll = RandomHelper.Between(homing * 90, 360 - homing * 90);
        Quaternion spreadRotation =
            Quaternion.Euler(0, 0, roll) *
            Quaternion.Euler(Random.value * spread, 0, 0) *
            Quaternion.Euler(0, 0, -roll);

        var lastKnownPosition = Vector3.zero;
        NetworkPlayer targetOwner = Network.player;
        if (target != null)
        {
            targetOwner = target.owner ?? Network.player;
            lastKnownPosition = target.transform.position;
        }

        networkView.RPC("ShootHoming", RPCMode.All,
            gun.position + gun.forward, gun.rotation * spreadRotation, Network.player, targetOwner, lastKnownPosition, homing);
    }

    [RPC]
    void Shoot(Vector3 position, Quaternion rotation, NetworkPlayer player)
    {
        BulletScript bullet = (BulletScript) Instantiate(bulletPrefab, position, rotation);
        bullet.Player = player;
    }

    [RPC]
    void ShootHoming(Vector3 position, Quaternion rotation, NetworkPlayer player, NetworkPlayer target, Vector3 lastKnownPosition, float homing)
    {
        BulletScript bullet = (BulletScript) Instantiate(bulletPrefab, position, rotation);
        bullet.Player = player;
        var targetScript = FindSceneObjectsOfType(typeof (PlayerScript)).Cast<PlayerScript>().Where(
            x => x.owner == target).OrderBy(x => Vector3.Distance(x.transform.position, lastKnownPosition)).First();
        bullet.target = targetScript == null ? null : targetScript.transform;
        bullet.homing = homing;
        bullet.speed = 400;
    }
}
