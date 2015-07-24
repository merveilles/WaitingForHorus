using UnityEngine;

public class MechaAnimationEvents : MonoBehaviour
{
    public delegate void StepHandler(Vector3 direction);

    public event StepHandler OnStep = delegate { };
    public void StepForward(float forward)
    {
        OnStep(new Vector3(0, 0, forward));
    }

    public void StepRight(float right)
    {
        OnStep(new Vector3(0, 0, right));
    }
}
