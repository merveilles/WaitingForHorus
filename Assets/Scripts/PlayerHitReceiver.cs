using UnityEngine;

public class PlayerHitReceiver : MonoBehaviour
{
    public PlayerScript Player;

    public bool WantsRocketJump
    {
        get { return Player.IsLookingDownFarEnoughForRocketJump; }
    }
}
