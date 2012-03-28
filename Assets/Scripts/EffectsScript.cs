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

    void Awake()
    {
        Instance = this;
    }

    [RPC]
    void Explosion(Vector3 position, Quaternion rotation)
    {
        Instantiate(explosionPrefab, position, rotation);
    }

    [RPC]
    void ExplosionHit(Vector3 position, Quaternion rotation)
    {
        Instantiate(explosionHitPrefab, position, rotation);
    }
}
