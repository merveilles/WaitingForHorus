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

    Material mat;

	float cooldownLeft = 0;
    int bulletsLeft = BurstCount;

    float cannonChargeCountdown = CannonChargeTime;
    Dictionary<PlayerScript, WeaponIndicatorScript.PlayerData> targets;

    CameraScript playerCamera;

    void Awake()
    {
        playerCamera = GetComponentInChildren<CameraScript>();
        targets = Camera.main.GetComponent<WeaponIndicatorScript>().Targets;
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
            Camera.main.GetComponent<WeaponIndicatorScript>().CooldownStep = 1 - Math.Min(Math.Max(cooldownLeft - ShotCooldown, 0) / ReloadTime, 1);

		    if(cooldownLeft == 0)
            {
                // Shotgun
                if (Input.GetButton("Alternate Fire"))
                {
                    // find homing target(s)
                    var aimedAt = targets.Where(x => x.Value.SinceInCrosshair >= AimingTime);

                    while (bulletsLeft > 0)
                    {
                        if (!aimedAt.Any())
                            DoHomingShot(ShotgunSpread, null, 0);
                        else
                        {
                            var chosen = aimedAt.OrderBy(x => Guid.NewGuid()).First();
                            DoHomingShot(ShotgunSpread, chosen.Key.networkView.owner, Mathf.Clamp01(chosen.Value.SinceInCrosshair / AimingTime));
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

            foreach (var v in targets.Values) v.Found = false;

            // Test for players in crosshair
            foreach (var p in FindSceneObjectsOfType(typeof(PlayerScript)))
            {
                var playerScript = p as PlayerScript;

                if (p == gameObject.GetComponent<PlayerScript>())
                    continue;

                var health = playerScript.gameObject.GetComponent<HealthScript>();

                var position = playerScript.transform.position;
                var screenPos = Camera.main.WorldToScreenPoint(position);

                if (health.Health > 0 && (screenPos.XY() - screenCenter).magnitude < allowedDistance)
                {
                    WeaponIndicatorScript.PlayerData data;
                    if (!targets.TryGetValue(playerScript, out data))
                        targets.Add(playerScript, data = new WeaponIndicatorScript.PlayerData());

                    data.ScreenPosition = screenPos.XY();
                    data.SinceInCrosshair += Time.deltaTime;
                    data.Found = true;
                }
                else
                    targets.Remove(playerScript);
            }

            if (targets.Count > 0)
                foreach (var p in targets.Keys.ToArray())
                    if (!targets[p].Found)
                        targets.Remove(p);
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

    void DoHomingShot(float spread, NetworkPlayer? target, float homing)
    {
        bulletsLeft -= 1;

        spread *= (1 + homing);

        float roll = Random.value * 360;
        Quaternion spreadRotation =
            Quaternion.Euler(0, 0, roll) *
            Quaternion.Euler(Random.value * spread, 0, 0) *
            Quaternion.Euler(0, 0, -roll);

        networkView.RPC("ShootHoming", RPCMode.All,
            gun.position + gun.forward, gun.rotation * spreadRotation, Network.player, target ?? Network.player, homing);
    }

    [RPC]
    void Shoot(Vector3 position, Quaternion rotation, NetworkPlayer player)
    {
        BulletScript bullet = (BulletScript) Instantiate(bulletPrefab, position, rotation);
        bullet.Player = player;
    }

    [RPC]
    void ShootHoming(Vector3 position, Quaternion rotation, NetworkPlayer player, NetworkPlayer target, float homing)
    {
        BulletScript bullet = (BulletScript) Instantiate(bulletPrefab, position, rotation);
        bullet.Player = player;
        var targetScript = FindSceneObjectsOfType(typeof(PlayerScript)).Cast<PlayerScript>().First(x => x.networkView.owner == target);
        bullet.target = targetScript == null ? null : targetScript.transform;
        bullet.homing = homing;
        bullet.speed = 500;
    }
}
