using UnityEngine;
using System.Collections;

public class HealthScript : MonoBehaviour
{
    public int maxShield = 4;
    public int maxHealth = 4;
    public float shieldRegenTime = 5;
    public GameObject deathPrefab;
    bool dead;

    public int Shield { get; private set; }
    public int Health { get; private set; }

    float timeUntilShieldRegen;

    void Awake()
    {
        Shield = maxShield;
        Health = maxHealth;
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
                Shield += 1;
            }

            //if(Input.GetKeyDown("z"))
            //{
            //    DoDamage(1, Network.player);
            //}
        }
    }

    [RPC]
    void DoDamage(int damage, NetworkPlayer shootingPlayer)
    {
        if (networkView.isMine && !dead)
        {
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
            }
        }
    }

    public float timeUntilRespawn = 5;

    [RPC]
    void ScheduleRespawn(Vector3 position)
    {
        Hide();
        Instantiate(deathPrefab, transform.position, transform.rotation);
        TaskManager.Instance.WaitFor(timeUntilRespawn).Then(delegate {Respawn(position);});
    }

    void Hide()
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var r in GetComponentsInChildren<Collider>()) r.enabled = false;
        foreach (var r in GetComponentsInChildren<PlayerShootingScript>()) r.enabled = false;
    }
    void Respawn(Vector3 position)
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
        foreach (var r in GetComponentsInChildren<Collider>()) r.enabled = true;
        foreach (var r in GetComponentsInChildren<PlayerShootingScript>()) r.enabled = true;

        transform.position = position;
        Shield = maxShield;
        Health = maxHealth;
        dead = false;
    }
}
