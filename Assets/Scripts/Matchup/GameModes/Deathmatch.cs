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

    public override void Update()
    {
        base.Update();
        var leader = Leader;
        ScreenSpaceDebug.AddLineOnce("Deathmatch update");
        if (leader != null)
        {
            ScreenSpaceDebug.AddLineOnce("leader is " + leader.Name);
            for (int i = 0; i < PlayerScript.UnsafeAllEnabledPlayerScripts.Count; i++)
            {
                var character = PlayerScript.UnsafeAllEnabledPlayerScripts[i];
                if (character.Possessor == null) continue;
                bool flagVisible = character.Possessor == leader;
                ScreenSpaceDebug.AddLineOnce("setting " + character.Possessor.Name + "'s flag to " + flagVisible);
                character.HasFlagVisible = flagVisible;
            }
        }
        else
        {
            ScreenSpaceDebug.AddLineOnce("null leader");
            for (int i = 0; i < PlayerScript.UnsafeAllEnabledPlayerScripts.Count; i++)
            {
                var character = PlayerScript.UnsafeAllEnabledPlayerScripts[i];
                character.HasFlagVisible = false;
            }
        }
    }

    // May return null
    public PlayerPresence Leader
    {
        get
        {
            if (PlayerPresence.UnsafeAllPlayerPresences.Count < 1) return null;
            PlayerPresence leader = PlayerPresence.UnsafeAllPlayerPresences[0];
            if (PlayerPresence.UnsafeAllPlayerPresences.Count == 1) return leader;
            bool anyChanged = false;
            foreach (var presence in Server.Presences)
            {
                if (presence.Score != leader.Score)
                    anyChanged = true;
                if (presence.Score > leader.Score)
                    leader = presence;
            }
            return anyChanged ? leader : null;
        }
    }

    private void StartAfterReceivingServer()
    {
        foreach (var presence in Server.Presences)
        {
            SetupPresenceListener(presence);
        }

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
            presence.SetScorePoints(0);
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
        newPlayerPresence.SetScorePoints(0);
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