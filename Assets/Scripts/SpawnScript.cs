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
	    Spawn();
	}
	
	public void Spawn()
	{
        TaskManager.Instance.WaitUntil(_ => PlayerRegistry.Instance != null).Then(() => PlayerRegistry.RegisterCurrentPlayer(chosenUsername));
	    Network.Instantiate(PlayerTemplate,
            RespawnZone.GetRespawnPoint(), Quaternion.identity, 0);
    }
	
	void OnPlayerDisconnected(NetworkPlayer player) 
    {
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

        if (Network.isServer)
        {
            Network.RemoveRPCs(PlayerRegistry.Instance.networkView.viewID);
            Network.Destroy(PlayerRegistry.Instance.networkView.viewID);
        }

		Application.LoadLevel(Application.loadedLevel);
	}
	
	public void SetChosenUsername(string chosenUsername) 
	{
		this.chosenUsername = chosenUsername;
	}
}
