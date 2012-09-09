using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class SpawnScript : MonoBehaviour 
{	
    public static SpawnScript Instance { get; private set; }

	public GameObject PlayerTemplate;
    public GameObject PlayerRegistryPrefab;

	string chosenUsername;
	
    void Awake()
    {
        Instance = this;
    }

	void OnServerInitialized() 
    {
        Network.Instantiate(PlayerRegistryPrefab, Vector3.zero, Quaternion.identity, 0);
		Spawn();
	}

	void OnConnectedToServer()
	{
        ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, Network.player, "connected", true, false);
	}
	
    public void WaitAndSpawn()
    {
        StartCoroutine(Co_WaitAndSpawn());
    }
    IEnumerator Co_WaitAndSpawn()
    {
        while (ServerScript.IsAsyncLoading)
            yield return new WaitForSeconds(1 / 30f);
        Spawn();
    }

	void Spawn()
	{
        if (ServerScript.Spectating) return;

        TaskManager.Instance.WaitUntil(_ => PlayerRegistry.Instance != null).Then(() => PlayerRegistry.RegisterCurrentPlayer(chosenUsername));
        Network.Instantiate(PlayerTemplate, RespawnZone.GetRespawnPoint(), Quaternion.identity, 0);
    }
	
	void OnPlayerDisconnected(NetworkPlayer player) 
    {
        if (Network.isServer)
            ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, player, "disconnected", true, false);

        Debug.Log("Clean up after player " + player);
		Network.RemoveRPCs(player);
		Network.DestroyPlayerObjects(player);
	}
	
	void OnDisconnectedFromServer(NetworkDisconnection info) 
    {
        if (Network.isServer)
            Debug.Log("Local server connection disconnected");
        else
            if (info == NetworkDisconnection.LostConnection)
                Debug.Log("Lost connection to the server");
            else
                Debug.Log("Successfully diconnected from the server");

        foreach (var p in FindObjectsOfType(typeof(PlayerScript)).Cast<PlayerScript>())
            Destroy(p.gameObject);
    }
	
	public void SetChosenUsername(string chosenUsername) 
	{
		this.chosenUsername = chosenUsername;
	}
}
