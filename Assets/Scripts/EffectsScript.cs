using UnityEngine;

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

    private static float LastExplosionSoundTime;

    public void Awake()
    {
        Instance = this;
    }

   // [RPC]
    public static void Explosion(Vector3 position, Quaternion rotation)
    {
        var exp = (GameObject) Instantiate(Instance.explosionPrefab, position, rotation);

        const float window = 0.01f;
        // TODO this is a bit of a hack
        bool enoughTimeElapsed = LastExplosionSoundTime + window < Time.realtimeSinceStartup;
        if (enoughTimeElapsed)
            LastExplosionSoundTime = Time.realtimeSinceStartup;
		//sounds disabled or already played the sound very recently? don't play this one then
		exp.GetComponent<AudioSource>().mute = !(GlobalSoundsScript.soundEnabled && enoughTimeElapsed);

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
