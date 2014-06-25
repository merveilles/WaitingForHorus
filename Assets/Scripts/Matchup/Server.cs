using System;
using System.Collections.Generic;
using UnityEngine;

public class Server : MonoBehaviour
{
    public GameMode CurrentGameMode { get; private set; }

    public List<NetworkPlayer> NetworkPlayers { get; private set; }
    public Relay Relay { get; set; }

    private GameMode DefaultGameMode
    {
        get
        {
            return new Deathmatch(this);
        }
    }

    public void Awake()
    {
        DontDestroyOnLoad(this);
        NetworkPlayers = new List<NetworkPlayer>();
    }

    public void Start()
    {
        if (networkView.isMine)
        {
            OnPlayerConnected(Network.player);
            CurrentGameMode = DefaultGameMode;
            CurrentGameMode.Start();
        }
    }

    public void Update()
    {
    }

    public void OnPlayerConnected(NetworkPlayer player)
    {
        NetworkPlayers.Add(player);
    }

    public void OnPlayerDisconnected(NetworkPlayer player)
    {
        Network.RemoveRPCs(player);
        NetworkPlayers.Remove(player);
    }

    public void OnLevelWasLoaded(int level)
    {
        CurrentGameMode.ReceiveMapChanged();
    }
}

public abstract class GameMode : IDisposable
{
    public delegate void GameModeCompleteHandler();
    public event GameModeCompleteHandler OnGameModeComplete = delegate { };

    public abstract void Start();
    protected Server Server { get; private set; }
    protected GameMode(Server server)
    {
        Server = server;
    }

    public abstract void ReceiveMapChanged();
    public abstract void Dispose();
}

public class Deathmatch : GameMode
{
    public override void Start()
    {
        PlayerScript.OnPlayerScriptSpawned += ReceivePlayerSpawned;
        Application.LoadLevel("pi_mar");
    }

    private void ReceivePlayerSpawned(PlayerScript newPlayerScript)
    {
        Debug.Log("Spawned: " + newPlayerScript);
    }

    public override void ReceiveMapChanged()
    {
        foreach (var networkPlayer in Server.NetworkPlayers)
        {
            if (networkPlayer != Network.player)
                Server.Relay.networkView.RPC("ClientSpawnCharacter", networkPlayer);
            else
                Server.Relay.ClientSpawnCharacter();
        }
    }

    public Deathmatch(Server server) : base(server) { }
    public override void Dispose()
    {
        PlayerScript.OnPlayerScriptSpawned -= ReceivePlayerSpawned;
    }
}