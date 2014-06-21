using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class PlayerShootingScript : MonoBehaviour
{
    public const float AimingTime = 0.75f;

	public int BurstCount = 8;
	public float ShotCooldown = 0.045f;
	public float ReloadTime = 0.45f;
    public float BurstSpread = 1.5f;
    public float ShotgunSpreadBase = 0.375f;
    public float ShotgunSpread = 10;
    public float ShotgunBulletSpeedMultiplier = 0.25f;
    public float ShotgunHomingSpeed = 0.675f;
    public float CannonChargeTime = 0.5f;
    public float HeatAccuracyFudge = 0.5f;
	
    public AudioSource reloadSound;
    public AudioSource targetSound;
    public AudioSource pepperGunSound;
    public AudioSource burstGunSound;

    public BulletScript bulletPrefab;
    public BulletScript cannonBulletPrefab;
    public Transform gun;

    public Texture2D cannonIndicator;
    public AnimationCurve cannonOuterScale;
    public AnimationCurve cannonInnerScale;

	public float heat = 0.0f;
	float cooldownLeft = 0.0f;
    int bulletsLeft;

    public delegate void ShotFiredHandler();
    // Invoked when a shot is fired (either primary or secondary)
    public event ShotFiredHandler OnShotFired = delegate {};

    //float cannonChargeCountdown = CannonChargeTime;
    WeaponIndicatorScript weaponIndicator;
    // public because HealthScript accesses it :x
    public List<WeaponIndicatorScript.PlayerData> targets;

    CameraScript playerCamera;
    PlayerScript playerScript;

    public void Awake()
    {
		bulletsLeft = BurstCount;
        playerCamera = GetComponentInChildren<CameraScript>();
        weaponIndicator = Camera.main.GetComponent<WeaponIndicatorScript>();
        targets = weaponIndicator.Targets;
        playerScript = GetComponent<PlayerScript>();
    }

    /*void Update()
    {
        gun.LookAt(playerCamera.GetTargetPosition());

        if (playerScript.Paused)
            bulletsLeft = BurstCount;
    }*/
	
    // TODO unused, looks like it should go below in the homing firing
    //WeaponIndicatorScript.PlayerData GetFirstTarget()
    //{
    //    var aimedAt = targets.Where(x => x.SinceInCrosshair >= AimingTime );
    //    return aimedAt.OrderBy( x => Guid.NewGuid() ).First();
    //}

    public void Update()
    {
        gun.LookAt(playerCamera.GetTargetPosition());

        if (playerScript.Paused)
            bulletsLeft = BurstCount;
		
        if ( networkView.isMine && Screen.lockCursor && !playerScript.Paused )
		{
			cooldownLeft = Mathf.Max( 0, cooldownLeft - Time.deltaTime );
			heat = Mathf.Clamp01( heat - Time.deltaTime );
            weaponIndicator.CooldownStep = 1 - Math.Min( Math.Max(cooldownLeft - ShotCooldown, 0) / ReloadTime, 1 );

		    if( cooldownLeft <= Mathf.Epsilon )
            {
                // Shotgun
                if( Input.GetButton( "Alternate Fire") )
                {
                    OnShotFired();

                    // find homing target(s)
					var aimedAt = targets.Where( x => x.SinceInCrosshair >= AimingTime ).ToArray();

					var bulletsShot = bulletsLeft;
                    var first = true;
                    while( bulletsLeft > 0 )
                    {
                        if( !aimedAt.Any() )
                            DoHomingShot( ShotgunSpread, null, 0, first );
                        else
						{
        					var pd = aimedAt.OrderBy( x => Guid.NewGuid() ).First();
                            DoHomingShot( ShotgunSpread, pd.Script, Mathf.Clamp01( pd.SinceInCrosshair / AimingTime ) * ShotgunHomingSpeed, first );
						}
						
                        cooldownLeft += ShotCooldown;
                        first = false;
                    }
                    cooldownLeft += ReloadTime;

                    var recoilImpulse = -gun.forward * ((float)bulletsShot / BurstCount);
                    recoilImpulse *= playerScript.controller.isGrounded ? 25 : 87.5f;
                    recoilImpulse.y *= playerScript.controller.isGrounded ? 0.1f : 0.375f;
                    playerScript.AddRecoil(recoilImpulse);

                    //cannonChargeCountdown = CannonChargeTime;
                }

                // Burst
                else if (Input.GetButton("Fire")) // burst fire
                {
                    OnShotFired();

                    DoShot( BurstSpread );
                    cooldownLeft += ShotCooldown;
                    if( bulletsLeft <= 0 )
                        cooldownLeft += ReloadTime;
                }

                if (bulletsLeft != BurstCount && Input.GetButton("Reload"))
                {
                    bulletsLeft = BurstCount;
					
					if( GlobalSoundsScript.soundEnabled )
                    	reloadSound.Play();
					
                    cooldownLeft += ReloadTime;
                }
            }

            if( bulletsLeft <= 0 ) 
            {
                bulletsLeft = BurstCount;
				if( GlobalSoundsScript.soundEnabled )
                	reloadSound.Play();
            }

		    var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            var allowedDistance = 130 * Screen.height / 1500f;

            foreach ( var v in targets ) v.Found = false;
            //Debug.Log(targets.Values.Count + " targets to find");

            // Test for players in crosshair
            foreach (var ps in PlayerScript.EnabledPlayerScripts)
            {
                if( ps == GetComponent<PlayerScript>() ) // Is targeting self?
                    continue;

                var health = ps.gameObject.GetComponent<HealthScript>();
                var position = ps.transform.position;
                var screenPos = Camera.main.WorldToScreenPoint(position);

                if (health.Health > 0 && screenPos.z > 0 && ( screenPos.XY() - screenCenter ).magnitude < allowedDistance)
                {
                    WeaponIndicatorScript.PlayerData data;
                    if ( (data = targets.FirstOrDefault( x => x.Script == ps ) ) == null )
                        targets.Add( data = new WeaponIndicatorScript.PlayerData { Script = ps, WasLocked = false } );

                    data.ScreenPosition = screenPos.XY();
                    data.SinceInCrosshair += Time.deltaTime;
                    data.Found = true;
					
                    if ( !data.WasLocked && data.Locked ) // Send target notification
					{	
						if( GlobalSoundsScript.soundEnabled )
                        	targetSound.Play();
						
						data.Script.networkView.RPC( "Targeted", RPCMode.All, gameObject.networkView.owner );
					}
                }
            }
			
			CheckTargets();
		}
    }

    public void OnApplicationQuit()
    {
		CheckTargets();
	}
	
	public void CheckTargets()
	{
        if( targets.Count > 0 )
		{
			for( int i = 0; i < targets.Count; i++ )
			{
				if( targets[i].Script != null )
				{
					if( targets[i].WasLocked && !targets[i].Found ) 
						targets[i].Script.networkView.RPC( "Untargeted", RPCMode.All, gameObject.networkView.owner );
					targets[i].WasLocked = targets[i].Locked;
					
	           		if( !targets[i].Found || gameObject.GetComponent<HealthScript>().Health < 1 || targets[i].Script == null ) // Is player in target list dead, or unseen? Am I dead?
						targets.RemoveAt(i);
				}
				else 
				{
					targets.RemoveAt( i );
				}
			}
		}	
	}
	
    void DoShot(float spread)
    {
        bulletsLeft -= 1;
		spread += heat * HeatAccuracyFudge;
		heat += 0.25f;

        float roll = Random.value * 360;
        Quaternion spreadRotation =
            Quaternion.Euler( 0, 0, roll ) *
            Quaternion.Euler( Random.value * spread, 0, 0 ) *
            Quaternion.Euler( 0, 0, -roll );

        networkView.RPC("Shoot", RPCMode.Others,
            gun.position + gun.forward * 4.0f, gun.rotation * spreadRotation,
            Network.player );
        Shoot( gun.position + gun.forward * 4.0f, gun.rotation * spreadRotation, Network.player );
    }

    public void InstantReload()
    {
        bulletsLeft = BurstCount;
    }

    void DoHomingShot(float spread, PlayerScript target, float homing, bool doSound)
    {
        bulletsLeft -= 1;

        spread *= ( ShotgunSpreadBase + homing * 5 );

        float roll = RandomHelper.Between(homing * 90, 360 - homing * 90);
        Quaternion spreadRotation =
            Quaternion.Euler(0, 0, roll) *
            Quaternion.Euler(Random.value * spread, 0, 0) *
            Quaternion.Euler(0, 0, -roll);

        var lastKnownPosition = Vector3.zero;
        NetworkPlayer targetOwner = Network.player;
        if( target != null )
        {
            targetOwner = target.owner ?? Network.player;
            lastKnownPosition = target.transform.position;
        }

        networkView.RPC("ShootHoming", RPCMode.Others,
			gun.position + gun.forward * 4.0f, gun.rotation * spreadRotation, 
			Network.player, targetOwner, lastKnownPosition, homing, doSound );
        ShootHoming(
            gun.position + gun.forward * 4.0f, gun.rotation * spreadRotation,
            Network.player, targetOwner, lastKnownPosition, homing, doSound );
    }

    [RPC]
    void Shoot(Vector3 position, Quaternion rotation, NetworkPlayer player)
    {
        BulletScript bullet = (BulletScript)Instantiate( bulletPrefab, position, rotation );
        bullet.Player = player;
		
		if( GlobalSoundsScript.soundEnabled )
        	burstGunSound.Play();
    }

    // TODO very inefficient
    [RPC]
    void ShootHoming(Vector3 position, Quaternion rotation, NetworkPlayer player, NetworkPlayer target, Vector3 lastKnownPosition, float homing, bool doSound)
    {
        BulletScript bullet = (BulletScript)Instantiate( bulletPrefab, position, rotation );
        bullet.Player = player;

		PlayerScript targetScript;
        try
        {
            targetScript = PlayerScript.EnabledPlayerScripts.Where(
                x => x.owner == target).OrderBy(x => Vector3.Distance(x.transform.position, lastKnownPosition)).FirstOrDefault();
        }
        catch (Exception) { targetScript = null; }

        bullet.target = targetScript == null ? null : targetScript.transform;
        bullet.homing = homing;
        bullet.speed *= ShotgunBulletSpeedMultiplier;
        bullet.recoil = 1;

        if( doSound )
			if( GlobalSoundsScript.soundEnabled )
				pepperGunSound.Play();
    }
}
