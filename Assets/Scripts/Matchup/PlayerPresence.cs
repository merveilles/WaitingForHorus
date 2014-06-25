using UnityEngine;

public class PlayerPresence : MonoBehaviour
{
    protected Relay Relay { get; private set; }
    public bool HasAuthority { get { return networkView.isMine; } }

    protected PlayerPresence(Relay relay)
    {
        Relay = relay;
    }
}