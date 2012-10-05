using System.Linq;
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

    public int Shield { get; private set; }
    public int Health { get; private set; }

    public Renderer shieldRenderer;

    float timeUntilShieldRegen;
    float timeSinceRespawn;
    public float timeUntilRespawn = 5;

    bool invulnerable;

    Renderer bigCell;
    Renderer[] smallCells;

    void Awake()
    {
        Shield = maxShield;
        Health = maxHealth;

        var graphics = gameObject.FindChild("Animated Mesh Fixed");
        bigCell = graphics.FindChild("healthsphere_rear").GetComponentInChildren<Renderer>();
        smallCells = new[] { graphics.FindChild("healthsphere_left").GetComponentInChildren<Renderer>(), graphics.FindChild("healthsphere_right").GetComponentInChildren<Renderer>() };
    }

    void Update()
    {
        if(networkView.isMine)
        {
            if(transform.position.y < -104)
            {
                DoDamage(1, Network.player);
            }

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

    void ShotFired()
    {
        invulnerable = false;
    }

    [RPC]
    void SetShield(bool on, bool immediate)
    {
        if (immediate)
        {
            shieldRenderer.enabled = on;
            return;
        }

        TaskManager.Instance.WaitUntil(t =>
        {
            if (shieldRenderer == null)
                return true;
            var p = Easing.EaseIn(Mathf.Clamp01(t / 0.75f), EasingType.Quadratic);
            p = on ? p : 1 - p;
            shieldRenderer.enabled = RandomHelper.Probability(Mathf.Clamp01(p));
            return t >= 1;
        }).Then(() => { if (shieldRenderer != null) shieldRenderer.enabled = on; });
    }
    [RPC]
    void SetHealth(int health)
    {
        bigCell.enabled = health >= 2;
        foreach (var r in smallCells)
            r.enabled = health >= 2;
    }

    [RPC]
    void DoDamage(int damage, NetworkPlayer shootingPlayer)
    {
        if (networkView.isMine && !dead)
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
                NetworkLeaderboard.Instance.networkView.RPC("RegisterKill", RPCMode.All, shootingPlayer, Network.player);
                networkView.RPC("ScheduleRespawn", RPCMode.All,
                        RespawnZone.GetRespawnPoint());
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

    [RPC]
    void ScheduleRespawn(Vector3 position)
    {
        Hide();
        Instantiate(deathPrefab, transform.position, transform.rotation);
        TaskManager.Instance.WaitFor(timeUntilRespawn).Then(() => Respawn(position));
    }

    [RPC]
    void ImmediateRespawn()
    {
        StartCoroutine(WaitAndRespawn());
    }

    IEnumerator WaitAndRespawn()
    {
        Hide();

        while (ServerScript.IsAsyncLoading)
            yield return new WaitForSeconds(1 / 30f);

        Respawn(RespawnZone.GetRespawnPoint());
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
            r.targets.Clear();
            r.enabled = false;
        }
    }
    public void UnHide()
    {
        if (!(ServerScript.hostState == ServerScript.HostingState.Hosting || ServerScript.hostState == ServerScript.HostingState.Connected))
            return;

        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
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
        if (isSpectating)   Hide();
        else                UnHide();

        PlayerRegistry.For(networkView.owner).Spectating = isSpectating;
    }

    public void Respawn(Vector3 position)
    {
        if (!(ServerScript.hostState == ServerScript.HostingState.Hosting || ServerScript.hostState == ServerScript.HostingState.Connected))
            return;

        networkView.RPC("ToggleSpectate", RPCMode.All, false);

        transform.position = position;
    }
}
