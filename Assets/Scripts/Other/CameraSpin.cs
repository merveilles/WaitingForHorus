using UnityEngine;

public class CameraSpin : MonoBehaviour 
{
    public static CameraSpin Instance { get; private set; }

    public float rotateSpeed = 0.2f;
    public int sign = 1;

    Vector3 transPosOrigin;
    Quaternion transRotOrigin;

    public GameObject Thingy;

    public bool IsSpectating { get; set; }

    public void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Instance = this;

        IsSpectating = false;
    }

    public void Start()
    {
        transPosOrigin = transform.localPosition;
        transRotOrigin = transform.localRotation;

        Camera.main.depthTextureMode = DepthTextureMode.DepthNormals;

        transform.localPosition = transPosOrigin;
        transform.localRotation = transRotOrigin;
    }

    public void Update()
    {
        if (transform.localEulerAngles.y > 150) sign *= -1;
        if (transform.localEulerAngles.y < 100) sign *= -1;

        transform.Rotate(0, rotateSpeed * Time.deltaTime * sign, 0);

        if (Relay.Instance.CurrentServer == null)
            IsSpectating = true;

        if (IsSpectating)
        {
            Camera.main.transform.position = Thingy.transform.position;
            Camera.main.transform.rotation = Thingy.transform.rotation;
        }
    }
}
