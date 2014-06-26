using UnityEngine;

public class Deathmatch : GameMode
{
    public string CurrentMapName { get; private set; }

    private bool IsMapLoaded = false;

    public override void Start()
    {
        PlayerScript.OnPlayerScriptSpawned += ReceivePlayerSpawned;
        PlayerScript.OnPlayerScriptDied += ReceivePlayerDied;
        PlayerPresence.OnPlayerPresenceAdded += ReceivePresenceAdded;

        CurrentMapName = "Loading...";
        Application.LoadLevel("pi_mar");
    }

    //public override void OnNewConnection(NetworkPlayer newPlayer)
    //{
        
    //}

    private void ReceivePlayerSpawned(PlayerScript newPlayerScript)
    {
        Debug.Log("Spawned: " + newPlayerScript);
    }

    private void ReceivePlayerDied(PlayerScript deadPlayerScript)
    {
        if (networkView.isMine)
        {
            //Network.Destroy(deadPlayerScript.networkView.viewID);
            deadPlayerScript.PerformDeath();
            foreach (var player in Server.NetworkPlayers)
            {
                //if (player != deadPlayerScript.networkView.owner && !Network.isServer)
                //    deadPlayerScript.networkView.SetScope(player, false);
            }
        }
    }

    public override void ReceiveMapChanged()
    {
        IsMapLoaded = true;
        if (networkView.isMine)
        {
            foreach (var presence in Server.Presences)
            {
                presence.SpawnCharacter(RespawnZone.GetRespawnPoint());
            }
        }
    }
    private void ReceivePresenceAdded(PlayerPresence newPlayerPresence)
    {
        if (networkView.isMine && IsMapLoaded)
        {
            newPlayerPresence.SpawnCharacter(RespawnZone.GetRespawnPoint());
        }
    }

    public void Destroy()
    {
        PlayerScript.OnPlayerScriptSpawned -= ReceivePlayerSpawned;
        PlayerScript.OnPlayerScriptDied -= ReceivePlayerDied;
        PlayerPresence.OnPlayerPresenceAdded -= ReceivePresenceAdded;
    }
}