using System.Collections.Generic;
using UnityEngine;

public class Deathmatch : GameMode
{
    public string CurrentMapName { get; private set; }

    private bool IsMapLoaded = false;
    private List<PresenceListener> PresenceListeners;

    public override void Awake()
    {
        base.Awake();
        PresenceListeners = new List<PresenceListener>();
    }

    public override void Start()
    {
        base.Start();
        PlayerScript.OnPlayerScriptSpawned += ReceivePlayerSpawned;
        PlayerScript.OnPlayerScriptDied += ReceivePlayerDied;
        PlayerPresence.OnPlayerPresenceAdded += ReceivePresenceAdded;
        PlayerPresence.OnPlayerPresenceRemoved += ReceivePresenceRemoved;
        
        if (networkView.isMine || Server != null)
            StartAfterReceivingServer();
        else
        {
            networkView.RPC("WantServerViewID", networkView.owner);
        }
    }

    private void StartAfterReceivingServer()
    {
        foreach (var presence in Server.Presences)
        {
            SetupPresenceListener(presence);
        }

        CurrentMapName = "Loading...";
        if (Application.loadedLevelName != "pi_mar")
            Application.LoadLevel("pi_mar");
        else
            ReceiveMapChanged();
    }

    [RPC]
// ReSharper disable once UnusedMember.Local
    private void WantServerViewID(NetworkMessageInfo info)
    {
        if (networkView.isMine)
        {
            networkView.RPC("RemoteReceiveServerViewID", info.sender, Server.networkView.viewID);
        }
    }

    [RPC]
// ReSharper disable once UnusedMember.Local
    private void RemoteReceiveServerViewID(NetworkViewID serverViewID)
    {
        NetworkView view = NetworkView.Find(serverViewID);
        if (view != null)
        {
            Server = view.observed as Server;
            if (Server != null && !IsMapLoaded) StartAfterReceivingServer();
        }
    }

    //public override void OnNewConnection(NetworkPlayer newPlayer)
    //{
        
    //}

    private void ReceivePlayerSpawned(PlayerScript newPlayerScript)
    {
        //Debug.Log("Spawned: " + newPlayerScript);
    }

    private void ReceivePlayerDied(PlayerScript deadPlayerScript)
    {
        if (networkView.isMine)
        {
            deadPlayerScript.PerformDestroy();
            Server.BroadcastMessageFromServer(deadPlayerScript.Possessor.Name + " was destroyed");
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
        CurrentMapName = Application.loadedLevelName;
        Server.BroadcastMessageFromServer("Welcome to " + CurrentMapName);
    }
    private void ReceivePresenceAdded(PlayerPresence newPlayerPresence)
    {
        SetupPresenceListener(newPlayerPresence);
        if (networkView.isMine && IsMapLoaded)
        {
            newPlayerPresence.SpawnCharacter(RespawnZone.GetRespawnPoint());
        }
    }

    private void ReceivePresenceRemoved(PlayerPresence removedPlayerPresence)
    {
        for (int i = PresenceListeners.Count - 1; i >= 0; i--)
        {
            if (PresenceListeners[i].Presence == removedPlayerPresence)
            {
                PresenceListeners[i].OnDestroy();
                PresenceListeners.RemoveAt(i);
                break;
            }
        }
    }

    public void OnDestroy()
    {
        foreach (var presenceListener in PresenceListeners)
            presenceListener.OnDestroy();
        PresenceListeners.Clear();

        PlayerScript.OnPlayerScriptSpawned -= ReceivePlayerSpawned;
        PlayerScript.OnPlayerScriptDied -= ReceivePlayerDied;
        PlayerPresence.OnPlayerPresenceAdded -= ReceivePresenceAdded;
    }

    private void SetupPresenceListener(PlayerPresence presence)
    {
        PresenceListeners.Add(new PresenceListener(this, presence));
    }

    private void PresenceListenerWantsRespawnFor(PlayerPresence presence)
    {
        if (networkView.isMine && IsMapLoaded)
            presence.SpawnCharacter(RespawnZone.GetRespawnPoint());
    }

    //private void RemovePresenceListener(PresenceListener listener)
    //{
    //    listener.OnDestroy();
    //    PresenceListeners.Remove(listener);
    //}

    private class PresenceListener
    {
        public PlayerPresence Presence;
        public Deathmatch Deathmatch;

        public PresenceListener(Deathmatch parent, PlayerPresence handledPresence)
        {
            Deathmatch = parent;
            Presence = handledPresence;
            Presence.OnPlayerPresenceWantsRespawn += ReceivePresenceWantsRespawn;
        }

        private void ReceivePresenceWantsRespawn()
        {
            Deathmatch.PresenceListenerWantsRespawnFor(Presence);
        }

        public void OnDestroy()
        {
            Presence.OnPlayerPresenceWantsRespawn -= ReceivePresenceWantsRespawn;
        }
    }
}