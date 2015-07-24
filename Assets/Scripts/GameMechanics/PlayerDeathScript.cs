using UnityEngine;

public class PlayerDeathScript : MonoBehaviour
{
    ParticleSystem p;

    public void Awake()
    {
        p = GetComponentInChildren<ParticleSystem>();
    }

    public void Update()
    {
        if(!p.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}
