using System.Collections;
using System.Linq;
using UnityEngine;

public class SpawnScript : MonoBehaviour 
{	
    public static SpawnScript Instance { get; private set; }

	public GameObject PlayerTemplate;
    public GameObject PlayerRegistryPrefab;
    public GameObject LeaderboardViewerPrefab;
    //public GameObject ChatScriptPrefab;

	public string chosenUsername;

    public void Awake()
    {
        Instance = this;
    }

    public void OnServerInitialized() 
    {
        RegistrySpawn();
	}
	
    public void WaitAndSpawn()
    {
        StartCoroutine(Co_WaitAndSpawn());
    }
    IEnumerator Co_WaitAndSpawn()
    {
        while (ServerScript.IsAsyncLoading)
            yield return new WaitForSeconds(1 / 30f);
        FinishSpawn();
    }

    void RegistrySpawn( )
	{
        Network.Instantiate( PlayerRegistryPrefab, Vector3.zero, Quaternion.identity, 0 );
        Network.Instantiate( LeaderboardViewerPrefab, Vector3.zero, Quaternion.identity, 0 );
       // Network.Instantiate( ChatScriptPrefab, Vector3.zero, Quaternion.identity, 0 );
        //TaskManager.Instance.WaitUntil( _ => PlayerRegistry.Propagated ).Then( ( ) => PlayerRegistry.RegisterCurrentPlayer( chosenUsername, networkView.owner.guid ) );
		//player.name = GameObject player =
    }

    public void FinishSpawn()
    {
        if( ServerScript.Spectating ) return;
        Network.Instantiate( PlayerTemplate, RespawnZone.GetRespawnPoint(), Quaternion.identity, 0 );
        ChatScript.Instance.networkView.RPC( "LogChat", RPCMode.All, Network.player, "connected", true, false );
    }

    public void OnPlayerDisconnected(NetworkPlayer player) 
    {
        if (Network.isServer)
            ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, player, "disconnected", true, false);

        Debug.Log("Clean up after player " + player);
		Network.RemoveRPCs(player);
		Network.DestroyPlayerObjects(player);
	}

    public void OnDisconnectedFromServer(NetworkDisconnection info) 
    {
        if (Network.isServer)
            Debug.Log("Local server connection disconnected");
        else
            if (info == NetworkDisconnection.LostConnection)
                Debug.Log("Lost connection to the server");
            else
                Debug.Log("Successfully diconnected from the server");

        foreach (var p in PlayerScript.AllEnabledPlayerScripts)
            Destroy(p.gameObject);
    }
	
	public void SetChosenUsername(string newChosenUsername) 
	{
		chosenUsername = newChosenUsername;
	}
}
