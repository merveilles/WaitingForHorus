using UnityEngine;
using System.Collections;

public class AutoDestructScript : MonoBehaviour 
{
    public float timeToLive = 1.0f;

    void Update()
    {
        timeToLive -= Time.deltaTime;
        if(timeToLive <= 0)
            Object.Destroy(gameObject);
    }
}
