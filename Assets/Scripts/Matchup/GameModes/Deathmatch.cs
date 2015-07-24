using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Deathmatch : GameMode
{
    public string CurrentMapName { get; private set; }

    //public float? TimeLimit { get; set; }
    public int ScoreLimit
    {
        get { return _ScoreLimit; }
        set
        {
            if (_ScoreLimit != value && _ScoreLimit >= 0)
            {
                _ScoreLimit = value;
                PlayerPrefs.SetInt("scorelimit", value);
            }
        }
    }

    private bool IsRoundInProgress = false;
    private bool IsMapLoaded = false;
    private List<PresenceListener> PresenceListeners;
    private int _ScoreLimit;

    private PlayerPresence LastLeader;
    private bool AnnouncedFirstKill;

    private bool StartRoundAfterMapChange;

    private int[] ScoreAnnouncementValues = new[]
    {
        10, 5, 3, 2, 1
    };

    private int PeakScoreThisRound;

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

        Relay.Instance.OptionsMenu.OnMapSelection += TryChangeLevel;
        Relay.Instance.OptionsMenu.DisplayRoundOptionsDelegate = DisplayRoundOptions;

        ScoreLimit = PlayerPrefs.GetInt("scorelimit", 35);

        StartRoundAfterMapChange = true;
        StartAfterReceivingServer();
    }

    public override void Update()
    {
        base.Update();
        if (IsRoundInProgress)
        {
            var leader = Leader;
            if (leader != null)
            {
                for (int i = 0; i < PlayerScript.UnsafeAllEnabledPlayerScripts.Count; i++)
                {
                    var character = PlayerScript.UnsafeAllEnabledPlayerScripts[i];
                    if (character.Possessor == null) continue;
                    bool flagVisible = character.Possessor == leader;
                    character.HasFlagVisible = flagVisible;
                }

                // End round if someone over score limit
                if (ScoreLimit > 0 && leader.Score >= ScoreLimit && AnnouncedFirstKill)
                    EndRoundNow();

                // Else announce score remaining if needed
                else if (leader.Score > PeakScoreThisRound && AnnouncedFirstKill)
                {
                    int prevScore = PeakScoreThisRound;
                    PeakScoreThisRound = leader.Score;
                    int pointsThatWereLeft = ScoreLimit - prevScore;
                    int pointsLeft = ScoreLimit - PeakScoreThisRound;
                    for (int i = ScoreAnnouncementValues.Length - 1; i >= 0; i--)
                    {
                        int value = ScoreAnnouncementValues[i];
                        if (pointsThatWereLeft > value && pointsLeft <= value)
                        {
                            Server.BroadcastMessageFromServer(pointsLeft + " points remaining", Server.BannerMessageWithSoundType);
                            break;
                        }
                    }
                }

            }
            else
            {
                for (int i = 0; i < PlayerScript.UnsafeAllEnabledPlayerScripts.Count; i++)
                {
                    var character = PlayerScript.UnsafeAllEnabledPlayerScripts[i];
                    character.HasFlagVisible = false;
                }
            }

            if (leader != LastLeader && leader != null)
            {
                // Only start caring once we've got a kill
                if (AnnouncedFirstKill)
                {
                    string message = LastLeader != null
                        ? leader.Name + " has taken the lead from " + LastLeader.Name
                        : leader.Name + " has taken the lead";
                    Server.BroadcastMessageFromServer(message, Server.BannerMessageWithSoundType);
                    LastLeader = leader;
                }
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
            if (PlayerPresence.UnsafeAllPlayerPresences.Count == 1)
            {
                return leader.IsSpectating ? null : leader;
            }
            bool anyChanged = false;
            foreach (var presence in Server.Presences)
            {
                if (presence.IsSpectating) continue;
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

        ReceiveMapChanged();
    }

    private void DisplayRoundOptions()
    {
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical();
        {
            GUILayout.BeginHorizontal(Relay.Instance.BaseSkin.box);
            GUILayout.Label("DEATHMATCH", Relay.Instance.OptionsMenu.LabelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (IsRoundInProgress)
            {
                if (GUIExts.Button("END ROUND", new GUIStyle(Relay.Instance.BaseSkin.button) {fixedWidth = 95*3 + 1}))
                {
                    EndRoundNow();
                    StartRoundAfterMapChange = false;
                }
            }
            else
            {
                if (GUIExts.Button("START ROUND", new GUIStyle(Relay.Instance.BaseSkin.button) {fixedWidth = 95*3 + 1}))
                {
                    StartRound();
                    StartRoundAfterMapChange = true;
                    Relay.Instance.ShowOptions = false;
                }
            }
            GUILayout.Space(-3);
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        GUILayout.Space(1);

        GUILayout.BeginVertical(new GUIStyle() {fixedWidth = 95});
        {
            GUILayout.BeginHorizontal(Relay.Instance.BaseSkin.box);
            GUILayout.Label("SCORE LIMIT", Relay.Instance.OptionsMenu.LabelStyle);
            GUILayout.EndHorizontal();

            GUI.enabled = !IsRoundInProgress;
            var numText = GUILayout.TextField(ScoreLimit.ToString(), new GUIStyle(Relay.Instance.BaseSkin.textField) {fixedWidth = 95});
            if (numText.Length < 1) ScoreLimit = 0;
            else
            {
                int num;
                if (int.TryParse(numText, out num) && num > 0)
                {
                    ScoreLimit = num;
                }
            }
            GUI.enabled = true;
        }
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    //public override void OnNewConnection(uLink.NetworkPlayer newPlayer)
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
                if (!AnnouncedFirstKill)
                {
                    Server.BroadcastMessageFromServer(instigator.Name + " draws first blood", Server.BannerMessageWithSoundType);
                    AnnouncedFirstKill = true;
                }
            }
        }
        else
            Server.BroadcastMessageFromServer(deadPlayerScript.Possessor.Name + " was destroyed");
        deadPlayerScript.PerformDestroy();
    }

    public override void ReceiveMapChanged()
    {
        IsMapLoaded = true;
        CurrentMapName = Application.loadedLevelName;
        Server.BroadcastMessageFromServer("Welcome to " + CurrentMapName);
        if (!IsRoundInProgress && StartRoundAfterMapChange)
            StartRound();
    }
    private void ReceivePresenceAdded(PlayerPresence newPlayerPresence)
    {
        SetupPresenceListener(newPlayerPresence);
        //if (IsMapLoaded)
        //{
        //    newPlayerPresence.SpawnCharacter(RespawnZone.GetRespawnPoint());
        //}
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
        Relay.Instance.OptionsMenu.OnMapSelection -= TryChangeLevel;
        Relay.Instance.OptionsMenu.DisplayRoundOptionsDelegate = null;
    }

    private void SetupPresenceListener(PlayerPresence presence)
    {
        PresenceListeners.Add(new PresenceListener(this, presence));
    }

    private void PresenceListenerWantsRespawnFor(PlayerPresence presence)
    {
        if (IsMapLoaded && IsRoundInProgress)
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
            case "start":
                if (IsRoundInProgress)
                    Relay.Instance.MessageLog.AddMessage("Unable to start round because a round is already in progress.");
                else
                    StartRound();
            break;
            case "end":
                if (!IsRoundInProgress)
                    Relay.Instance.MessageLog.AddMessage("Unable to end round because a round is not in progress.");
                else
                    EndRoundNow();
            break;
        }
    }

    private void TryChangeLevel(string levelName)
    {
        if (Application.CanStreamedLevelBeLoaded(levelName))
        {
            if (IsRoundInProgress)
                EndRoundNow();
            // Is this also necessary?
            foreach (var playerScript in PlayerScript.AllEnabledPlayerScripts)
                playerScript.PerformDestroy();
            Server.ChangeLevel(levelName);
        }
        else
        {
            Relay.Instance.MessageLog.AddMessage("Unable to change level to " + levelName + " because it is not available.");
        }
    }

    public void StartRound()
    {
        IsRoundInProgress = true;
        Server.IsGameActive = true;
        PeakScoreThisRound = 0;
        LastLeader = null;
        AnnouncedFirstKill = false;
        foreach (var presence in Server.Combatants)
        {
            presence.SpawnCharacter(RespawnZone.GetRespawnPoint());
            presence.SetScorePoints(0);
        }
        //Server.BroadcastMessageFromServer("Round start.");

        var sb = new StringBuilder();
        sb.AppendLine("Deathmatch");
        if (ScoreLimit > 0)
            sb.Append("First to " + ScoreLimit);
        else
            sb.Append("No limit");
        Server.StatusMessage = sb.ToString().ToUpper();
        Server.BroadcastMessageFromServer(sb.ToString(), Server.BannerMessageType);
    }

    public void EndRoundNow()
    {
        IsRoundInProgress = false;
        Server.IsGameActive = false;
        foreach (var playerScript in PlayerScript.AllEnabledPlayerScripts)
        {
            playerScript.PerformDestroy();
        }
        var leader = Leader;
        string winMessage = leader != null ? 
            leader.Name + " wins" :
            "Tie game";
        Server.BroadcastMessageFromServer("Round over\n" + winMessage, Server.BannerMessageWithSoundType);
        Server.StatusMessage = "ROUND OVER";
    }
}