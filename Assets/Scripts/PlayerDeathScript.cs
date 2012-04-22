using UnityEngine;
using System.Collections;

public class PlayerDeathScript : MonoBehaviour
{
    ParticleSystem p;

    void Awake()
    {
        p = GetComponentInChildren<ParticleSystem>();
    }

    void Update()
    {
        if(!p.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}
