using UnityEngine;

public class MouseSensitivityScript : MonoBehaviour
{
    public GUISkin skin;
    public float baseSensitivity = 3;

    public static float Sensitivity { get; private set; }

    float sensitivityPercentage = 50;
    //GUIStyle windowStyle;

    public void Awake()
    {
        //windowStyle = new GUIStyle(skin.window) { normal = { background = null } };
        //sensitivityPercentage = PlayerPrefs.GetInt("sensitivity", 50);
        Relay.Instance.OptionsMenu.OnSensitivityOptionChanged += ReceiveSensitivityChanged;
    }

    public void Start()
    {
        sensitivityPercentage = Relay.Instance.OptionsMenu.SensitivityOptionValue;
    }

    public void Update()
    {
        if(Input.GetButtonDown("Increase Sensitivity"))
        {
            Relay.Instance.OptionsMenu.SensitivityOptionValue += 0.05f;
        }
        if(Input.GetButtonDown("Decrease Sensitivity"))
        {
            Relay.Instance.OptionsMenu.SensitivityOptionValue -= 0.05f;
        }
        sensitivityPercentage = Mathf.Clamp01(sensitivityPercentage);

        float adjusted = Mathf.Lerp(0.05f, 1.0f, sensitivityPercentage);
        float fromCurve = Relay.Instance.MouseSensitivityCurve.Evaluate(adjusted);
        const float multiplier = 7.0f;
        Sensitivity = fromCurve * multiplier;
    }

    public void OnGUI()
    {
        //GUILayout.Window(3, new Rect(Screen.width - 200, 0, 200, 40),
        //                 OnWindow, "", windowStyle);
    }

    void OnWindow(int windowId)
    {
        GUI.skin = skin;
        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
        /*GUILayout.Label("-", GUILayout.ExpandWidth(false));
        GUILayout.HorizontalSlider(sensitivityPercentage, 0, 100,
            skin.FindStyle("Sensitivity Slider"),
            skin.FindStyle("Sensitivity Slider Thumb"),
            GUILayout.ExpandWidth(true));
        GUILayout.Label(string.Format("+ {0:d2}", sensitivityPercentage),
            GUILayout.ExpandWidth(false));*/
		//GUILayout.TextField( uLink.Network.player.guid );
        GUILayout.EndHorizontal();
    }

    private void ReceiveSensitivityChanged(float newSensitivity)
    {
        sensitivityPercentage = newSensitivity;
    }

    public void OnDestroy()
    {
        Relay.Instance.OptionsMenu.OnSensitivityOptionChanged -= ReceiveSensitivityChanged;
    }
}
