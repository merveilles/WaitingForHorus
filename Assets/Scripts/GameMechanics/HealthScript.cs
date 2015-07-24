using UnityEngine;

public class HealthScript : uLink.MonoBehaviour
{
    public int maxShield = 1;
    public int maxHealth = 2;
    public float shieldRegenTime = 5;
    public float invulnerabilityTime = 2;
    public GameObject deathPrefab;
    bool dead;

    readonly static Color DefaultShieldColor = new Color(110 / 255f, 190 / 255f, 255 / 255f, 30f / 255f);
    //readonly static Color HitShieldColor = new Color(1, 1, 1, 1f); // new Color(1, 0, 0, 1f);
    //readonly static Color RecoverShieldColor = new Color(1, 1, 1, 1f);

    public int _Shield;
    public int Shield { get { return _Shield; } private set { _Shield = value; } }
    public int _Health;
    public int Health { get { return _Health; } private set { _Health = value; } }

    public Renderer shieldRenderer;

    float timeUntilShieldRegen;
    float timeSinceRespawn;
    public float timeUntilRespawn = 5;

    bool invulnerable;
    bool firstSet;

    public Renderer[] HealthSpheres;

    public PlayerScript PlayerScript;

    public const float KillHeight = -104;
    private bool isInWater = false;
    private bool justBouncedPlayer = false;
    private float bounceCooldown = 0.0f;

    public void Awake()
    {
        Shield = maxShield;
        Health = maxHealth;

        if (HealthSpheres == null)
            HealthSpheres = new Renderer[0];
    }

    private void UpdateHealthSphereVisibility()
    {
        int visualHealth = Shield + Health;
        for (int i = HealthSpheres.Length - 1; i >= 0; i--)
        {
            HealthSpheres[i].enabled = visualHealth > 0;
            visualHealth--;
        }
    }

    public void Update()
    {
        if( bounceCooldown > 0.0f && justBouncedPlayer )
            bounceCooldown -= Time.deltaTime;
        else
        {
            justBouncedPlayer = false;
        }

        // TODO magical -104 number, what does it do?
        if( networkView.isMine && transform.position.y < KillHeight && !isInWater )
        {
            isInWater = true;

            /*if( !justBouncedPlayer )
            {
                justBouncedPlayer = true;*/
                PlayerScript.StopFalling();
                PlayerScript.AddRecoil( Vector3.up * 275.0f );
                DoDamageOwner( 1, transform.position, PlayerScript.Possessor );
            // Don't play water effect on death
                if (!dead)
                    EffectsScript.PlayerWaterHitEffect(transform.position);
                /*bounceCooldown = 0.5f;
            }
            else
            {*
                justBouncedPlayer = false;
                DoDamageOwner( 3, transform.position, PlayerScript.Possessor );
            }*/
        }
        else
        {
            isInWater = false;
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
                timeUntilShieldRegen = shieldRegenTime;
                Shield += 1;
                UpdateShield();
            }

            if (invulnerable)
            {
                timeSinceRespawn += Time.deltaTime;
                if (timeSinceRespawn > invulnerabilityTime)
                    invulnerable = false;
            }
        }
        UpdateHealthSphereVisibility();
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


    public void UpdateShield()
    {
        bool shouldBeEnabled = Shield > 0;
        shieldRenderer.enabled = shouldBeEnabled;
    }

    public void DeclareHitToOthers(int damage, Vector3 point, PlayerPresence instigator)
    {
        networkView.RPC("OthersReceiveHit", uLink.RPCMode.Others, damage, point, instigator.networkView.viewID);
    }

    [RPC]
// ReSharper disable once UnusedMember.Local
    protected void OthersReceiveHit(int damage, Vector3 point, uLink.NetworkViewID instigatorPresenceViewID)
    {
		EffectsScript.ExplosionHit( point, Quaternion.LookRotation( Vector3.up ) );
        if (networkView.isMine)
        {
            DoDamageOwner(damage, point,
                PlayerPresence.TryGetPlayerPresenceFromNetworkViewID(instigatorPresenceViewID));
        }
    }

    [RPC]
    public void DoDamageOwner( int damage, Vector3 point, PlayerPresence instigator)
    {
        ScreenSpaceDebug.AddMessage("DAMAGE", point, Color.red);
        if ( !dead )
        {
            if (invulnerable)
                return;

            Shield -= damage;
            timeUntilShieldRegen = shieldRegenTime;
            if(Shield < 0)
            {
                Health += Shield;
                Shield = 0;
            }
            if(Health <= 0)
            {
                Health = 0;
                dead = true;
                PlayDeathPrefab();
                GetComponent<PlayerScript>().RequestedToDieByOwner(instigator);
                Camera.main.GetComponent<WeaponIndicatorScript>().CooldownStep = 0;
            }
        }
    }


    // Call from server or client
    public void PlayDeathPrefab()
    {
        // TODO is this the same as uLink.RPCMode.All ?
        RemotePlayDeathPrefab();
        networkView.RPC("RemotePlayDeathPrefab", uLink.RPCMode.Others);
    }
    [RPC]
    protected void RemotePlayDeathPrefab()
    {
        Instantiate(deathPrefab, transform.position, transform.rotation);
    }
}
