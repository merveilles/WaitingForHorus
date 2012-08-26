using System.Linq;
using UnityEngine;
using System.Collections;

public class RoundScript : MonoBehaviour
{
    const float RoundDuration = 60*5; //60 * 5;
    const float PauseDuration = 5; // 25;
    const int SameLevelRounds = 2;

    float sinceRoundTransition;
    public bool GameStarted { get; set; }
    public bool RoundStopped { get; private set; }
    float sinceInteround;
    bool said60secWarning, said30secWarning, said10secWarning, said5secWarning;
    int toLevelChange;

    public static RoundScript Instance { get; private set; }

    void Start()
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
                        networkView.RPC("ChangeLevel", RPCMode.All);
                        toLevelChange = SameLevelRounds;
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
        foreach (var player in FindObjectsOfType(typeof(PlayerScript)).Cast<PlayerScript>())
        {
            player.Paused = false;
            if (player.networkView.isMine)
                player.networkView.RPC("ImmediateRespawn", RPCMode.All);
        }

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
