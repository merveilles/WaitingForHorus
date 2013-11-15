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

    void Update()
    {
        
    }

   // [RPC]
    public static void Explosion(Vector3 position, Quaternion rotation)
    {
        GameObject exp = (GameObject) Instantiate(Instance.explosionPrefab, position, rotation);
			
			//sounds disabled? don't play this one then
		exp.GetComponent<AudioSource>().mute = !GlobalSoundsScript.soundEnabled;

        var count = RandomHelper.Random.Next(1, 4);
        for (int i = 0; i < count; i++)
            Instantiate(Instance.hitConePrefab, position, rotation);
    }

   // [RPC]
    public static  void ExplosionHit(Vector3 position, Quaternion rotation)
    {
        Instantiate(Instance.explosionHitPrefab, position, rotation);

        var count = RandomHelper.Random.Next(1, 4);
        for (int i = 0; i < count; i++)
            Instantiate(Instance.hitConePrefab, position, rotation);
    }

    //[RPC]
    public static void ExplosionArea(Vector3 position, Quaternion rotation)
    {
        Instantiate(Instance.areaExplosionPrefab, position, rotation);
    }

    //[RPC]
    public static void ExplosionHitArea(Vector3 position, Quaternion rotation)
    {
        ExplosionArea(position, rotation);
    }
}
