using UnityEngine;
using System.Collections;

public class BulletScript : MonoBehaviour 
{
    public static int BulletCollisionLayers
    {
        get
        {
            return (1<<LayerMask.NameToLayer("Default")) |
                   (1<<LayerMask.NameToLayer("Player Hit"));
        }
    }

    public GameObject explosionPrefab;
    public GameObject explosionHitPrefab;
    public GameObject bulletCasingPrefab;
    public float speed = 900;
	public float lifetime = 2;
    public int damage = 1;

    public NetworkPlayer Player { get; set; }

    void Awake()
    {
        GameObject casing = (GameObject)
            Instantiate(bulletCasingPrefab, transform.position, transform.rotation);
        casing.rigidbody.AddRelativeForce(
            new Vector3(1 + Random.value, Random.value, 0),
            ForceMode.Impulse);
        casing.rigidbody.AddTorque(
            5 * new Vector3(-0.5f-Random.value, -Random.value*0.1f, -0.5f-Random.value),
            ForceMode.Impulse);
    }

	void Update()
    {
        float distance = speed * Time.deltaTime;

        RaycastHit hitInfo;
        Physics.Raycast(transform.position, transform.forward, out hitInfo,
                distance, BulletCollisionLayers);

        if(hitInfo.transform)
        {
            if(Player == Network.player)
            {
                if(hitInfo.transform != null &&
                    (hitInfo.transform.networkView == null ||
                     hitInfo.transform.networkView.owner != Network.player))
                {
                    HealthScript health = hitInfo.transform.GetComponent<HealthScript>();
                    // err, kinda lame, this is so that the collider can be
                    // directly inside the damaged object rather than on it,
                    // useful when the damage collider is different from the
                    // real collider
                    if(health == null && hitInfo.transform.parent != null)
                       health = hitInfo.transform.parent.GetComponent<HealthScript>();

                    string effect = health == null ? "Explosion" : "ExplosionHit";
                    EffectsScript.DoEffect(
                        effect,
                        hitInfo.point,
                        Quaternion.LookRotation(hitInfo.normal));

                    if(health != null)
                    {
                        health.networkView.RPC(
                            "DoDamage", RPCMode.Others, damage, Network.player);
                    }

                    Destroy(gameObject);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        transform.position += transform.forward * distance;
		
        // max lifetime
		lifetime -= Time.deltaTime;
		if (lifetime <= 0)
			Destroy(gameObject);
	}
}
