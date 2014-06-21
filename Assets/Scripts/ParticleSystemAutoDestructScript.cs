using UnityEngine;

public class ParticleSystemAutoDestructScript : MonoBehaviour 
{
    public void Update()
    {
        if(!particleSystem.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}
