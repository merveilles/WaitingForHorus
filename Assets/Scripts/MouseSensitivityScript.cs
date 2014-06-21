using UnityEngine;

public class MouseSensitivityScript : MonoBehaviour
{
    public GUISkin skin;
    public float baseSensitivity = 3;

    public static float Sensitivity { get; private set; }

    int sensitivityPercentage = 50;
    GUIStyle windowStyle;

    public void Awake()
    {
        windowStyle = new GUIStyle(skin.window) { normal = { background = null } };
        sensitivityPercentage = PlayerPrefs.GetInt("sensitivity", 50);
    }

    public void Update()
    {
        if(Input.GetButtonDown("Increase Sensitivity"))
        {
            sensitivityPercentage += 2;
        }
        if(Input.GetButtonDown("Decrease Sensitivity"))
        {
            sensitivityPercentage -= 2;
        }
        sensitivityPercentage = Mathf.Clamp(sensitivityPercentage, 0, 100);
        PlayerPrefs.SetInt("sensitivity", sensitivityPercentage);

        Sensitivity = baseSensitivity *
            Mathf.Pow(2, sensitivityPercentage / 25.0f - 2);
    }

    public void OnGUI()
    {
        GUILayout.Window(3, new Rect(Screen.width - 200, 0, 200, 40),
                         OnWindow, "", windowStyle);
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
		//GUILayout.TextField( Network.player.guid );
        GUILayout.EndHorizontal();
    }
}
