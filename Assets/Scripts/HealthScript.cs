using UnityEngine;
using System.Collections;

public class HealthScript : MonoBehaviour
{
    public int maxShield = 1;
    public int maxHealth = 2;
    public float shieldRegenTime = 5;
    public float invulnerabilityTime = 2;
    public GameObject deathPrefab;
    bool dead;

    readonly static Color DefaultShieldColor = new Color(110 / 255f, 190 / 255f, 255 / 255f, 30f / 255f);
    readonly static Color HitShieldColor = new Color(1, 1, 1, 1f); // new Color(1, 0, 0, 1f);
    readonly static Color RecoverShieldColor = new Color(1, 1, 1, 1f);

    public int Shield { get; private set; }
    public int Health { get; private set; }

    public Renderer shieldRenderer;

    float timeUntilShieldRegen;
    float timeSinceRespawn;
    public float timeUntilRespawn = 5;

    bool invulnerable;
    bool firstSet;

    Renderer bigCell;
    Renderer[] smallCells;

    public const float KillHeight = -104;

    public void Awake()
    {
        Shield = maxShield;
        Health = maxHealth;

        var graphics = gameObject.FindChild("Animated Mesh Fixed");
        bigCell = graphics.FindChild("healthsphere_rear").GetComponentInChildren<Renderer>();
        smallCells = new[] { graphics.FindChild("healthsphere_left").GetComponentInChildren<Renderer>(), graphics.FindChild("healthsphere_right").GetComponentInChildren<Renderer>() };
    }

    public void Update()
    {
        // TODO magical -104 number, what does it do?
        if( networkView.isMine && transform.position.y < KillHeight )
        {
            NetworkPlayer? networkPlayer = GetComponent<PlayerScript>().owner;
            if (networkPlayer.HasValue)
                DoDamage( 1, networkPlayer.Value );
        }

        if (!firstSet && shieldRenderer != null)
        {
            shieldRenderer.material.SetColor("_TintColor", DefaultShieldColor);
            firstSet = true;
        }

        if(networkView.isMine)
        {
            timeUntilShieldRegen -= Time.deltaTime;
            if(timeUntilShieldRegen < 0 && Shield < maxShield)
            {
                timeUntilShieldRegen += shieldRegenTime;
                if (Shield == 0)
                    networkView.RPC("SetShield", RPCMode.All, true, false);
                Shield += 1;
            }

            if (invulnerable)
            {
                timeSinceRespawn += Time.deltaTime;
                if (timeSinceRespawn > invulnerabilityTime)
                    invulnerable = false;
            }
        }
    }

    public void OnEnable()
    {
        var shootingScript = GetComponent<PlayerShootingScript>();
        if (shootingScript != null)
        {
            shootingScript.OnShotFired += ShotFired;
        }
    }

    public void OnDisable()
    {
        var shootingScript = GetComponent<PlayerShootingScript>();
        if (shootingScript != null)
        {
            shootingScript.OnShotFired -= ShotFired;
        }
    }

    // Called from delegate/event after any shot is fired.
    private void ShotFired()
    {
        invulnerable = false;
    }

    [RPC]
    public void SetShield(bool on, bool immediate)
    {
        if (immediate)
        {
            shieldRenderer.enabled = on;
            shieldRenderer.material.SetColor("_TintColor", DefaultShieldColor);
            return;
        }

        TaskManager.Instance.WaitUntil(t =>
        {
            if (shieldRenderer == null)
                return true;

            var p = Easing.EaseIn(Mathf.Clamp01(t / 0.75f), EasingType.Quadratic);
            p = on ? p : 1 - p;

            shieldRenderer.enabled = RandomHelper.Probability(Mathf.Clamp01(p));
            shieldRenderer.material.SetColor("_TintColor", on ? Color.Lerp(RecoverShieldColor, DefaultShieldColor, p) : HitShieldColor);

            return t >= 1;
        }).Then(() =>
        {
            if (shieldRenderer != null)
            {
                shieldRenderer.enabled = on;
                shieldRenderer.material.SetColor("_TintColor", DefaultShieldColor);
            }
        });
    }
    [RPC]
    public void SetHealth(int health)
    {
        bigCell.enabled = health >= 2;
        foreach (var r in smallCells)
            r.enabled = health >= 2;
    }

    //[RPC]
    public void DoDamage( int damage, NetworkPlayer shootingPlayer ) //, NetworkPlayer hitPlayer 
    {
        if ( !dead )  //!networkView.isMine &&
        {
            //Debug.Log("Got " + damage + " damage");
            //Debug.Log("Before hit : Shield = " + Shield + ", Health = " + Health);

            if (invulnerable)
                return;

            int oldShield = Shield;
            Shield -= damage;
            timeUntilShieldRegen = shieldRegenTime;
            if(Shield < 0)
            {
                Health += Shield;
                Shield = 0;
            }
            if(Health <= 0)
            {
                if( Network.player == shootingPlayer ) 
				{
				    if (NetworkLeaderboard.Instance != null)
				    {
    					NetworkLeaderboard.Instance.networkView.RPC( "RegisterKill", RPCMode.All, shootingPlayer, GetComponent<PlayerScript>().owner );
				    }
               		networkView.RPC("ScheduleRespawn", RPCMode.All,
                        RespawnZone.GetRespawnPoint());
				}
				
                Health = 0;
                dead = true;
                Camera.main.GetComponent<WeaponIndicatorScript>().CooldownStep = 0;
            }

            //Debug.Log("Shield = " + Shield + ", Health = " + Health);

            networkView.RPC("SetHealth", RPCMode.All, Health);

            if((Shield != 0) != (oldShield != 0))
            {
                networkView.RPC("SetShield", RPCMode.All, Shield > 0, dead);
            }
        }
    }

    // TODO there is some clearly broken code here. It previously checked for
	// 'this != null' which is always impossible in C#. Probably someone was
	// having it crash and they didn't understand why, so they added that. I'm
	// guessing the timeout will sometimes perform an invalid action when the
	// game has gone into certain states and it tries to do something.
    //
    // Most likely, a timeout shouldn't be used, and it should simply count time
	// in Update or something like that.
    object respawnLock;
    [RPC]
    public void ScheduleRespawn(Vector3 position)
    {
        Hide();
        Instantiate(deathPrefab, transform.position, transform.rotation);
        var thisLock = new object();
        respawnLock = thisLock;
        TaskManager.Instance.WaitFor(timeUntilRespawn).Then(() =>
        {
            //Debug.Log("Spectating? " + ServerScript.Spectating);
            if (respawnLock == thisLock && !ServerScript.Spectating && !RoundScript.Instance.RoundStopped)
                Respawn(position);
        });
    }

    public void Hide()
    {
        if (!(ServerScript.hostState == ServerScript.HostingState.Hosting || ServerScript.hostState == ServerScript.HostingState.Connected))
            return;

        Health = 0;
        dead = true;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var r in GetComponentsInChildren<Collider>()) r.enabled = false;
        foreach (var r in GetComponentsInChildren<PlayerShootingScript>())
        {
            r.CheckTargets();
			r.targets.Clear();
            r.enabled = false;
        }
    }
    public void UnHide()
    {
        if (!(ServerScript.hostState == ServerScript.HostingState.Hosting || ServerScript.hostState == ServerScript.HostingState.Connected))
            return;

        //Debug.Log("UnHid");

        foreach (var r in GetComponentsInChildren<Renderer>()) if( r.name != "Canon" && r.name != "flag_flag" && r.name != "Cube" && r.name != "flag_pole" ) r.enabled = true; // Reenable non glitched renderers
        foreach (var r in GetComponentsInChildren<Collider>()) r.enabled = true;
        foreach (var r in GetComponentsInChildren<PlayerShootingScript>()) r.enabled = true;

        GetComponent<PlayerScript>().ResetVelocities();
        GetComponent<PlayerShootingScript>().InstantReload();

        Shield = maxShield;
        Health = maxHealth;
        dead = false;
        timeSinceRespawn = 0;
    }

    [RPC]
    public void ToggleSpectate(bool isSpectating)
    {
        //Debug.Log("Toggled spectate to " + isSpectating);

        if (isSpectating)   Hide();
        else                UnHide();

        PlayerRegistry.For(networkView.owner).Spectating = isSpectating;
    }

    public void Respawn(Vector3 position)
    {
        if (!(ServerScript.hostState == ServerScript.HostingState.Hosting || ServerScript.hostState == ServerScript.HostingState.Connected))
            return;

        //Debug.Log("Respawned");

        networkView.RPC("ToggleSpectate", RPCMode.All, false);

        SendMessage("ResetAnimation");
		SendMessage("ResetWarnings");

        transform.position = position;
    }
}
