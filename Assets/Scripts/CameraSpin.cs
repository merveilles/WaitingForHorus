using UnityEngine;
using System.Collections;

public class CameraSpin : MonoBehaviour 
{
    public float rotateSpeed = 0.2f;
    public int sign = 1;

    Vector3 camPosOrigin, transPosOrigin;
    Quaternion camRotOrigin, transRotOrigin;
    bool wasSpectating;

    void Start()
    {
        DontDestroyOnLoad(gameObject);

        camPosOrigin = Camera.main.transform.localPosition;
        transPosOrigin = transform.localPosition;

        camRotOrigin = Camera.main.transform.localRotation;
        transRotOrigin = transform.localRotation;
    }

    void Update()
    {
        if (transform.localEulerAngles.y > 150) sign *= -1;
        if (transform.localEulerAngles.y < 100) sign *= -1;

        transform.Rotate(0, rotateSpeed * Time.deltaTime * sign, 0);

        if (ServerScript.Spectating && !wasSpectating)
            ResetTransforms();
        wasSpectating = ServerScript.Spectating;
    }

    void OnDisconnectedFromServer(NetworkDisconnection mode)
    {
        if (TaskManager.Instance != null)
            TaskManager.Instance.WaitFor(0.25f).Then(ResetTransforms);
    }

    void ResetTransforms()
    {
        // Added a delay, it doesn't seem to work...?
        Camera.main.transform.localPosition = camPosOrigin;
        transform.localPosition = transPosOrigin;

        Camera.main.transform.localRotation = camRotOrigin;
        transform.localRotation = transRotOrigin;
    }
}
