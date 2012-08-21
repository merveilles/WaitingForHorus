using UnityEngine;
using System.Collections;

public class EffectsScript : MonoBehaviour 
{
    public static EffectsScript Instance { get; private set; }

    public static void DoEffect(string effect, params object[] args)
    {
        Instance.networkView.RPC(effect, RPCMode.All, args);
    }

    public GameObject explosionPrefab;
    public GameObject explosionHitPrefab;
    public GameObject areaExplosionPrefab;
    public GameObject hitConePrefab;

    void Awake()
    {
        Instance = this;
    }

    [RPC]
    void Explosion(Vector3 position, Quaternion rotation)
    {
        Instantiate(explosionPrefab, position, rotation);

        var count = 3;//RandomHelper.Random.Next(1, 4);
        for (int i = 0; i < count; i++)
            Instantiate(hitConePrefab, position, rotation);
    }

    [RPC]
    void ExplosionHit(Vector3 position, Quaternion rotation)
    {
        Instantiate(explosionHitPrefab, position, rotation);
        Instantiate(hitConePrefab, position, rotation);
    }

    [RPC]
    void ExplosionArea(Vector3 position, Quaternion rotation)
    {
        Instantiate(areaExplosionPrefab, position, rotation);
    }

    [RPC]
    void ExplosionHitArea(Vector3 position, Quaternion rotation)
    {
        ExplosionArea(position, rotation);
    }
}
