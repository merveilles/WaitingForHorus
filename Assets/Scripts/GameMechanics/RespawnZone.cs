using UnityEngine;
using System.Collections.Generic;

public class RespawnZone : MonoBehaviour
{
    static readonly List<RespawnZone> respawnZones = new List<RespawnZone>();

    public void Awake()
    {
        respawnZones.Add(this);
    }

    public void OnDestroy()
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
