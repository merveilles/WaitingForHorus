using UnityEngine;
using System.Collections;

public class PlayerDeathScript : MonoBehaviour
{
    public float timeUntilRespawn = 5;

    void Update()
    {
        timeUntilRespawn -= Time.deltaTime;
        if(timeUntilRespawn <= 0)
        {
            if(networkView.isMine)
            {
                Network.RemoveRPCs(networkView.viewID);
                Network.Destroy(gameObject);
            }
        }
    }
}
