using UnityEngine;

public class EffectsScript : uLink.MonoBehaviour 
{
    public static EffectsScript Instance { get; private set; }

    public GameObject explosionPrefab;
    public GameObject explosionHitPrefab;
    public GameObject hitConePrefab;

    public GameObject WaterHitPrefab;

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

    public static void PlayerWaterHitEffect(Vector3 position)
    {
        Instance.networkView.UnreliableRPC("RemotePlayWaterHitEffect", uLink.RPCMode.Others, position);
        Instance.RemotePlayWaterHitEffect(position);
    }

    [RPC]
    protected void RemotePlayWaterHitEffect(Vector3 position)
    {
        Instantiate(WaterHitPrefab, position, Quaternion.Euler(-90, 0, 0));
    }

}
