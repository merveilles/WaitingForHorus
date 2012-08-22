using UnityEngine;
using System.Collections;

public class CameraSpin : MonoBehaviour 
{
    public float rotateSpeed = 0.2f;
    public int sign = 1;

    void Update()
    {
        if (transform.localEulerAngles.y > 150) sign *= -1;
        if (transform.localEulerAngles.y < 100) sign *= -1;

        transform.Rotate(0, rotateSpeed * Time.deltaTime * sign, 0);
    }
}
