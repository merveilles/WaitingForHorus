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

    public GameObject bulletCasingPrefab;
    public float speed = 900;
	float lifetime = 2;
    public int damage = 1;
    public float areaOfEffect = 0;
    public float recoil = 0;
    public float homing = 0;
	public float accelerationSpeed = 0.1f;
    public Transform target;
    bool dead;
	float acceleration = 1.0f;

    public NetworkPlayer Player { get; set; }

    void Awake()
    {
        GameObject casing = (GameObject)
            Instantiate(bulletCasingPrefab, transform.position, transform.rotation);
        casing.rigidbody.AddRelativeForce(
            new Vector3(1 + Random.value, Random.value + 1, 0),
            ForceMode.Impulse);
        casing.rigidbody.AddTorque(
            5 * new Vector3(-0.5f-Random.value, -Random.value*0.1f, -0.5f-Random.value),
            ForceMode.Impulse);
        casing.rigidbody.useGravity = true;
    }

    bool DoDamageTo(Transform t)
    {
        HealthScript health = t.GetComponent<HealthScript>();
        // err, kinda lame, this is so that the collider can be
        // directly inside the damaged object rather than on it,
        // useful when the damage collider is different from the
        // real collider
        if(health == null && t.parent != null)
           health = t.parent.GetComponent<HealthScript>();

        if(health != null)
        {
			audio.Play();
            health.networkView.RPC(
                "DoDamage", RPCMode.Others, damage, Network.player);
            return true;
        }
        return false;
    }

	void Update()
    {
        if (!dead)
        {
			acceleration += accelerationSpeed * Time.deltaTime;
            float distance = speed * Time.deltaTime;
			distance *= acceleration;

            RaycastHit hitInfo;
            Physics.Raycast(transform.position, transform.forward, out hitInfo,
                            distance, BulletCollisionLayers);

            if (hitInfo.transform)
            {
                if (Player == Network.player)
                {
                    if (hitInfo.transform != null &&
                        (hitInfo.transform.parent.networkView == null ||
                         hitInfo.transform.parent.networkView.owner != Network.player))
                    {
                        bool playerHit = false;

                        if (areaOfEffect > 0)
                        {
                            Collider[] colliders = Physics.OverlapSphere(
                                hitInfo.point, areaOfEffect,
                                (1 << LayerMask.NameToLayer("Player Hit")));
                            foreach (Collider c in colliders)
                            {
                                playerHit = playerHit || DoDamageTo(c.transform);
                            }
                        }
                        else
                        {
                            playerHit = DoDamageTo(hitInfo.transform);
                        }

                        if (recoil > 0)
                        {
                            Collider[] colliders = Physics.OverlapSphere(hitInfo.point, 15, (1 << LayerMask.NameToLayer("Player Hit")));
                            foreach (Collider c in colliders)
                            {
                                if (c.gameObject.name != "PlayerHit")
                                    continue;

                                var t = c.transform;
                                NetworkView view = t.networkView;
                                while (view == null)
                                {
                                    t = t.parent;
                                    view = t.networkView;
                                }

                                t = t.FindChild("mecha_gun");
                                var endpoint = t.position + t.forward;

                                var direction = (endpoint - hitInfo.point);
                                var dist = Mathf.Max(direction.magnitude, 0.5f);
                                direction.Normalize();

                                var impulse = direction * (45 / dist);
                                if (impulse.y > 0) impulse.y *= 2.25f;
                                else impulse.y = 0;

                                if (playerHit && hitInfo.transform == c.transform)
                                    impulse *= 10;

                                view.RPC("AddRecoil", RPCMode.All, impulse);
                            }
                        }

                        string effect = playerHit ? "ExplosionHit" : "Explosion";
                        if (areaOfEffect > 0)
                        {
                            effect += "Area";
                        }
                        EffectsScript.DoEffect(
                            effect,
                            hitInfo.point,
                            Quaternion.LookRotation(hitInfo.normal));

                        dead = true;
                        renderer.enabled = false;
                    }
                }
                else
                {
                    dead = true;
                    renderer.enabled = false;
                }
            }

            transform.position += transform.forward * distance;

            // homing
            if (target != null && homing > 0)
            {
                //Debug.Log("Is homing @ " + homing);
                var lookVec = (target.position - transform.position).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookVec),
                                                      Mathf.Clamp01(homing * Time.deltaTime * 6));
            }
        }

	    var o = lifetime / 2f * 0.75f;
        GetComponent<TrailRenderer>().material.SetColor("_TintColor", new Color(o, o, o, 1));

	    // max lifetime
		lifetime -= Time.deltaTime;
		if (lifetime <= 0)
			Destroy(gameObject);
	}
}
