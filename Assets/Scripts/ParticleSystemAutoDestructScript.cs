using UnityEngine;
using System.Collections;

public class ParticleSystemAutoDestructScript : MonoBehaviour 
{
    void Update()
    {
        if(!particleSystem.IsAlive())
        {
            Object.Destroy(gameObject);
        }
    }
}
