using UnityEngine;

public class HealthScript : MonoBehaviour
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

    //Renderer bigCell;
    //Renderer[] smallCells;

    public PlayerScript PlayerScript;

    public const float KillHeight = -104;

    public void Awake()
    {
        Shield = maxShield;
        Health = maxHealth;

        //var graphics = gameObject.FindChild("Animated Mesh Fixed");
        //bigCell = graphics.FindChild("healthsphere_rear").GetComponentInChildren<Renderer>();
        //smallCells = new[] { graphics.FindChild("healthsphere_left").GetComponentInChildren<Renderer>(), graphics.FindChild("healthsphere_right").GetComponentInChildren<Renderer>() };
    }

    public void Update()
    {
        // TODO magical -104 number, what does it do?
        if( networkView.isMine && transform.position.y < KillHeight )
        {
            DoDamageOwner(1, transform.position);
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

    public void DeclareHitToOthers(int damage, Vector3 point)
    {
        networkView.RPC("OthersReceiveHit", RPCMode.Others, damage, point);
    }

    [RPC]
// ReSharper disable once UnusedMember.Local
    private void OthersReceiveHit(int damage, Vector3 point)
    {
		EffectsScript.ExplosionHit( point, Quaternion.LookRotation( Vector3.up ) );
        if (networkView.isMine)
        {
            DoDamageOwner(damage, point);
        }
    }

    [RPC]
    private void DoDamageOwner( int damage, Vector3 point)
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
                GetComponent<PlayerScript>().RequestedToDieByOwner();
                Camera.main.GetComponent<WeaponIndicatorScript>().CooldownStep = 0;
            }
        }
    }


    // Call from server or client
    public void PlayDeathPrefab()
    {
        // TODO is this the same as RPCMode.All ?
        RemotePlayDeathPrefab();
        networkView.RPC("RemotePlayDeathPrefab", RPCMode.Others);
    }
    [RPC]
    private void RemotePlayDeathPrefab()
    {
        Instantiate(deathPrefab, transform.position, transform.rotation);
    }
}
