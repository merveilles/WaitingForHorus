using UnityEngine;

public class ParticleSystemAutoDestructScript : MonoBehaviour 
{
    public void Update()
    {
        if(!GetComponent<ParticleSystem>().IsAlive())
        {
            Destroy(gameObject);
        }
    }
}
