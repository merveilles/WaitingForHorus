using UnityEngine;
using UnityEngine.SocialPlatforms;

public abstract class PlayerPresence
{
    protected Relay Relay { get; private set; }
    public abstract bool IsLocal { get; }

    protected PlayerPresence(Relay relay)
    {
        Relay = relay;
    }

    public abstract bool IsOnline { get; }
}

public class LocalPlayerPresence : PlayerPresence
{
    public override bool IsLocal
    {
        get { return true; }
    }

    public override bool IsOnline
    {
        get { return Relay.IsConnected; }
    }

    public LocalPlayerPresence(Relay relay) : base(relay)
    {
    }
}