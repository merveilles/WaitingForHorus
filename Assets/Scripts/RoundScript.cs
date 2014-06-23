using UnityEngine;
using System.Collections;

public class RoundScript : MonoBehaviour
{
    const float RoundDuration = 8 * 5;
    const float PauseDuration = 5;
    const int SameLevelRounds = 1;

    float sinceRoundTransition;
    public bool RoundStopped { get; private set; }
    public string CurrentLevel { get; set; }
    bool said5secWarning;
    int toLevelChange;

    private float[] RoundWarningTimes = {60f, 30f, 10f};
    private float ScaryWarningTime = 10f;

    public static RoundScript Instance { get; private set; }

    public void Awake()
    {
        Instance = this;
        toLevelChange = SameLevelRounds;
    }

    public void Update() 
    {
        if (Network.isServer)
        {
            float lastTimeRemaining = RoundDuration - sinceRoundTransition;
            sinceRoundTransition += Time.deltaTime;
            float timeRemaining = RoundDuration - sinceRoundTransition;

            if (!RoundStopped)
            {
                foreach (var roundWarningTime in RoundWarningTimes)
                {
                    if (lastTimeRemaining >= roundWarningTime &&
                        timeRemaining < roundWarningTime)
                    {
                        string decorator = roundWarningTime <= ScaryWarningTime ? "!" : "...";
                        ChatScript.Instance.networkView.RPC(
                            "LogChat", RPCMode.All, Network.player,
                            roundWarningTime + " seconds remaining" + decorator, true, true);
                    }
                }
            }
            else
            {
                if (!said5secWarning && PauseDuration - sinceRoundTransition < 5)
                {
                    ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, Network.player,
                                                        "Game starts in 5 seconds...", true, true);
                    said5secWarning = true;
                }
            }


            if (sinceRoundTransition >= (RoundStopped ? PauseDuration : RoundDuration))
            {
                RoundStopped = !RoundStopped;
                if (RoundStopped)
                {
                    networkView.RPC("StopRound", RPCMode.All);
                    ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, Network.player,
                                    "Round over!", true, true);
                    toLevelChange--;

                    if (toLevelChange == 0)
                        ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, Network.player,
                                                            "Level will change on the next round.", true, true);
                }
                else
                {
                    string oldLevel = CurrentLevel;
                    if( toLevelChange == 0 )
                        CurrentLevel = RandomHelper.InEnumerable( ServerScript.Instance.AllowedLevels );

                    ServerScript.Instance.ChangeLevel( CurrentLevel, true, true );

                    if( toLevelChange == 0 )
                        Debug.Log( "Loaded level is now " + CurrentLevel );

                    PlayerRegistry.Instance.networkView.RPC( "RegisteredHandshake", RPCMode.All, null, true );

                    networkView.RPC( "RestartRound", RPCMode.All, true );
                    ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, Network.player,
                                    "Game start!", true, true);
                }
                sinceRoundTransition = 0f;
                said5secWarning = false;
            }
	    }
	}


    [RPC]
    public void StopRound()
    {
        foreach (var player in PlayerScript.AllEnabledPlayerScripts)
            player.Paused = true;
        RoundStopped = true;
    }

    [RPC]
    public void RestartRound( bool changedLevel = false )
    {
        StartCoroutine(WaitAndResume());
    }

    IEnumerator WaitAndResume()
    {
        while (ServerScript.IsAsyncLoading)
            yield return new WaitForSeconds(1 / 30f);

        foreach (var player in PlayerScript.AllEnabledPlayerScripts)
           player.Paused = false;

        foreach (var entry in NetworkLeaderboard.Instance.Entries)
        {
            entry.Deaths = 0;
            entry.Kills = 0;
            entry.ConsecutiveKills = 0;
        }

        ChatScript.Instance.ChatLog.ForEach(x => x.Hidden = true);

        RoundStopped = false;
    }
}
