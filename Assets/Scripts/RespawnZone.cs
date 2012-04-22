using UnityEngine;
using System.Collections.Generic;

public class RespawnZone : MonoBehaviour
{
    static List<RespawnZone> respawnZones = new List<RespawnZone>();

    void Awake()
    {
        respawnZones.Add(this);
    }

    void OnDestroy()
    {
        respawnZones.Remove(this);
    }

    static int RandomIndex(int count)
    {
        return (int)Mathf.Min(count-1, Random.value * count);
    }

    public static Vector3 GetRespawnPoint() 
    {
        return respawnZones[RandomIndex(respawnZones.Count)].transform.position;
    }
}
