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

        Relay.Instance.MessageLog.OnCommandEntered += ReceiveCommandEntered;
        
        StartAfterReceivingServer();
    }

    private void StartAfterReceivingServer()
    {
        foreach (var presence in Server.Presences)
        {
            SetupPresenceListener(presence);
        }

        CurrentMapName = "pi_mar";

        // TODO can we get some real map changing here?
        if (Application.loadedLevelName == "pi_mar")
            ReceiveMapChanged();
        else
            Server.ChangeLevel("pi_mar");
    }


    //public override void OnNewConnection(NetworkPlayer newPlayer)
    //{
        
    //}

    private void ReceivePlayerSpawned(PlayerScript newPlayerScript)
    {
        //Debug.Log("Spawned: " + newPlayerScript);
    }

    private void ReceivePlayerDied(PlayerScript deadPlayerScript, PlayerPresence instigator)
    {
        if (instigator != null)
        {
            if (deadPlayerScript.Possessor == instigator)
            {
                Server.BroadcastMessageFromServer(deadPlayerScript.Possessor.Name + " committed suicide");
                instigator.ReceiveScorePoints(-1);
            }
            else
            {
                Server.BroadcastMessageFromServer(deadPlayerScript.Possessor.Name + " was destroyed by " + instigator.Name);
                instigator.ReceiveScorePoints(1);
            }
        }
        else
            Server.BroadcastMessageFromServer(deadPlayerScript.Possessor.Name + " was destroyed");
        deadPlayerScript.PerformDestroy();
    }

    public override void ReceiveMapChanged()
    {
        IsMapLoaded = true;
        foreach (var presence in Server.Presences)
        {
            presence.SpawnCharacter(RespawnZone.GetRespawnPoint());
        }
        CurrentMapName = Application.loadedLevelName;
        Server.BroadcastMessageFromServer("Welcome to " + CurrentMapName);
    }
    private void ReceivePresenceAdded(PlayerPresence newPlayerPresence)
    {
        SetupPresenceListener(newPlayerPresence);
        if (IsMapLoaded)
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
        Relay.Instance.MessageLog.OnCommandEntered -= ReceiveCommandEntered;
    }

    private void SetupPresenceListener(PlayerPresence presence)
    {
        PresenceListeners.Add(new PresenceListener(this, presence));
    }

    private void PresenceListenerWantsRespawnFor(PlayerPresence presence)
    {
        if (IsMapLoaded)
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

    private void ReceiveCommandEntered(string command, string[] args)
    {
        switch (command)
        {
            case "map":
                if (args.Length > 0)
                    TryChangeLevel(args[0]);
            break;
        }
    }

    private void TryChangeLevel(string levelName)
    {
        if (Application.CanStreamedLevelBeLoaded(levelName))
        {
            foreach (var playerScript in PlayerScript.AllEnabledPlayerScripts)
            {
                playerScript.PerformDestroy();
            }
            Server.ChangeLevel(levelName);
        }
        else
        {
            Relay.Instance.MessageLog.AddMessage("Unable to change level to " + levelName + " because it is not available.");
        }
    }
}