using System.Linq;
using UnityEngine;
using System.Collections;

public class RoundScript : MonoBehaviour
{
    const float RoundDuration = 60 * 5;
    const float PauseDuration = 20;
    const int SameLevelRounds = 2;

    float sinceRoundTransition;
    public bool RoundStopped { get; private set; }
    public string CurrentLevel { get; set; }
    float sinceInteround;
    bool said60secWarning, said30secWarning, said10secWarning, said5secWarning;
    int toLevelChange;

    public static RoundScript Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        toLevelChange = SameLevelRounds;
    }

    void Update() 
    {
        if (Network.isServer)
	    {
            sinceRoundTransition += Time.deltaTime;

            if (!RoundStopped)
            {
                if (!said60secWarning && RoundDuration - sinceRoundTransition < 60)
                {
                    ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, Network.player,
                                                        "60 seconds remaining...", true, true);
                    said60secWarning = true;
                }
                if (!said30secWarning && RoundDuration - sinceRoundTransition < 30)
                {
                    ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, Network.player,
                                                        "30 seconds remaining...", true, true);
                    said30secWarning = true;
                }
                if (!said10secWarning && RoundDuration - sinceRoundTransition < 10)
                {
                    ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, Network.player,
                                                        "10 seconds remaining!", true, true);
                    said10secWarning = true;
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
                    if (toLevelChange == 0)
                    {
                        ChangeLevel();
                        Debug.Log("Loaded level is now " + CurrentLevel);
                        networkView.RPC("ChangeLevelTo", RPCMode.Others, CurrentLevel);
                    }

                    networkView.RPC("RestartRound", RPCMode.All);
                    ChatScript.Instance.networkView.RPC("LogChat", RPCMode.All, Network.player,
                                    "Game start!", true, true);
                }
                sinceRoundTransition = 0;
                said60secWarning = said30secWarning = said10secWarning = said5secWarning = false;
            }
	    }
	}

    [RPC]
    public void ChangeLevel()
    {
        transform.parent.SendMessage("ChangeLevel");
        toLevelChange = SameLevelRounds;
    }

    [RPC]
    public void ChangeLevelTo(string levelName)
    {
        transform.parent.SendMessage("ChangeLevelIfNeeded", levelName);
        toLevelChange = SameLevelRounds;
    }

    [RPC]
    public void SyncLevel(string toLevel)
    {
        transform.parent.SendMessage("SyncAndSpawn", toLevel);
    }

    [RPC]
    public void StopRound()
    {
        foreach (var player in FindObjectsOfType(typeof(PlayerScript)).Cast<PlayerScript>())
            player.Paused = true;
        RoundStopped = true;
    }

    [RPC]
    public void RestartRound()
    {
        if (!ServerScript.Spectating)
            foreach (var player in FindObjectsOfType(typeof(PlayerScript)).Cast<PlayerScript>())
                if (player.networkView.isMine)
                    player.networkView.RPC("ImmediateRespawn", RPCMode.All);

        StartCoroutine(WaitAndResume());
    }

    IEnumerator WaitAndResume()
    {
        while (ServerScript.IsAsyncLoading)
            yield return new WaitForSeconds(1 / 30f);

        foreach (var player in FindObjectsOfType(typeof(PlayerScript)).Cast<PlayerScript>())
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
