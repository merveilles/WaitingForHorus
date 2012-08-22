using System.Linq;
using UnityEngine;
using System.Collections;

public class RoundScript : MonoBehaviour
{
    const float RoundDuration = 30;
    const float PauseDuration = 5;

    float sinceRoundTransition;
    public bool RoundStopped { get; private set; }
    float sinceInteround;

    public static RoundScript Instance { get; private set; }

    void Start()
    {
        Instance = this;
    }

    void Update() 
    {
        Debug.Log("Peer type : " + Network.peerType);
        if (Network.peerType == NetworkPeerType.Server)
	    {
            sinceRoundTransition += Time.deltaTime;

            if (sinceRoundTransition >= (RoundStopped ? PauseDuration : RoundDuration))
            {
                RoundStopped = !RoundStopped;
                if (RoundStopped)
                    networkView.RPC("StopRound", RPCMode.All);
                else
                    networkView.RPC("RestartRound", RPCMode.All);
                sinceRoundTransition = 0;
            }
	    }
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

        ChatScript.Instance.ChatLog.Clear();
        RoundStopped = false;
    }
}
