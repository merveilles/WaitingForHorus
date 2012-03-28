using UnityEngine;
using System.Collections;

public class HealthScript : MonoBehaviour
{
    public int maxShield = 4;
    public int maxHealth = 4;
    public float shieldRegenTime = 5;
    public GameObject deathPrefab;

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
            timeUntilShieldRegen -= Time.deltaTime;
            if(timeUntilShieldRegen < 0 && Shield < maxShield)
            {
                timeUntilShieldRegen += shieldRegenTime;
                Shield += 1;
            }
        }
    }

    [RPC]
    void DoDamage(int damage, NetworkPlayer shootingPlayer)
    {
        if (networkView.isMine)
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
                Network.Instantiate(deathPrefab, transform.position, transform.rotation, 0);
                networkView.RPC("ScheduleRespawn", RPCMode.All);
                Health = 0;
            }
        }
    }

    public float timeUntilRespawn = 5;

    [RPC]
    void ScheduleRespawn()
    {
        Hide();
        TaskManager.Instance.WaitFor(timeUntilRespawn).Then(Respawn);
    }
    void Hide()
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var r in GetComponentsInChildren<Collider>()) r.enabled = false;
        foreach (var r in GetComponentsInChildren<PlayerShootingScript>()) r.enabled = false;
    }
    void Respawn()
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
        foreach (var r in GetComponentsInChildren<Collider>()) r.enabled = true;
        foreach (var r in GetComponentsInChildren<PlayerShootingScript>()) r.enabled = true;

        transform.position = Vector3.up * 10;
        Shield = maxShield;
        Health = maxHealth;
    }
}
