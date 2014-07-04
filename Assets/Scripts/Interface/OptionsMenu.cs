using System;
using UnityEngine;

public class OptionsMenu
{
    private float _FOVOptionValue;

    public GUISkin Skin
    {
        get { return _Skin; }
        set
        {
            _Skin = value;
            LabelStyle = new GUIStyle(Skin.label);
            LabelStyle.normal.textColor = new Color(0.05f, 0.05f, 0.05f, 0.97f);
        }
    }

    public OptionsMenu(GUISkin skin)
    {
        Skin = skin;
        FOVOptionValue = PlayerPrefs.GetFloat("fov", 85.0f);
        SensitivityOptionValue = PlayerPrefs.GetFloat("mousesensitivity", 0.5f);
        IsExteriorView = PlayerPrefs.GetInt("thirdperson", 1) > 0;
        ShouldPlaySoundEffects = PlayerPrefs.GetInt("soundeffects", 1) > 0;
        ShouldPlayMusic = PlayerPrefs.GetInt("music", 1) > 0;
        IsAimInverted = PlayerPrefs.GetInt("invertaim", 0) > 0;
    }

    public float FOVOptionValue
    {
        get { return _FOVOptionValue; }
        set
        {
            // Don't do anything if almost the same
            if (Mathf.Approximately(_FOVOptionValue, value)) return;

            _FOVOptionValue = value;
            PlayerPrefs.SetFloat("fov", value);
            OnFOVOptionChanged(value);
        }
    }

    private float _Sensitivity;
    public float SensitivityOptionValue
    {
        get { return _Sensitivity; }
        set
        {
            float clamped = Mathf.Clamp01(value);
            if (!Mathf.Approximately(_Sensitivity, clamped))
            {
                _Sensitivity = clamped;
                PlayerPrefs.SetFloat("mouse_sensitivity", clamped);
                OnSensitivityOptionChanged(clamped);
            }
        }
    }

    private bool _IsExteriorView;

    public bool IsExteriorView
    {
        get
        {
            return _IsExteriorView;
        }
        set
        {
            if (_IsExteriorView != value)
            {
                _IsExteriorView = value;
                int asNumber = _IsExteriorView ? 1 : 0;
                PlayerPrefs.SetInt("thirdperson", asNumber);
                OnExteriorViewOptionChanged(value);
            }
        }
    }

    private bool _ShouldPlaySoundEffects;

    public bool ShouldPlaySoundEffects
    {
        get
        {
            return _ShouldPlaySoundEffects;
        }
        set
        {
            if (_ShouldPlaySoundEffects != value)
            {
                _ShouldPlaySoundEffects = value;
                int asNumber = value ? 1 : 0;
                PlayerPrefs.SetInt("soundeffects", asNumber);
                OnShouldPlaySoundEffectsOptionChanged(value);
            }
        }
    }
    private bool _ShouldPlayMusic;

    public bool ShouldPlayMusic
    {
        get
        {
            return _ShouldPlayMusic;
        }
        set
        {
            if (_ShouldPlayMusic != value)
            {
                _ShouldPlayMusic = value;
                int asNumber = value ? 1 : 0;
                PlayerPrefs.SetInt("music", asNumber);
                OnShouldPlayMusicOptionChanged(value);
            }
        }
    }

    private bool _IsAimInverted;

    public bool IsAimInverted
    {
        get
        {
            return _IsAimInverted;
        }
        set
        {
            if (_IsAimInverted != value)
            {
                _IsAimInverted = value;
                int asNumber = value ? 1 : 0;
                PlayerPrefs.SetInt("invertaim", asNumber);
                OnIsAimInvertedOptionChanged(value);
            }
        }
    }

    public delegate void OptionsMenuStateChangedHandler();
    public event OptionsMenuStateChangedHandler OnOptionsMenuWantsClosed = delegate {};
    public event OptionsMenuStateChangedHandler OnOptionsMenuWantsGoToTitle = delegate {};
    public event OptionsMenuStateChangedHandler OnOptionsMenuWantsQuitGame = delegate {};
    public event OptionsMenuStateChangedHandler OnOptionsMenuWantsSpectate = delegate {};

    public delegate void FloatOptionChangedHandler(float optionValue);
    public delegate void BoolOptionChangedHandler(bool optionValue);

    public event FloatOptionChangedHandler OnFOVOptionChanged = delegate {};
    public event FloatOptionChangedHandler OnSensitivityOptionChanged = delegate {};
    public event BoolOptionChangedHandler OnExteriorViewOptionChanged = delegate {};
    public event BoolOptionChangedHandler OnShouldPlaySoundEffectsOptionChanged = delegate {}; 
    public event BoolOptionChangedHandler OnShouldPlayMusicOptionChanged = delegate {};
    public event BoolOptionChangedHandler OnIsAimInvertedOptionChanged = delegate {}; 

    private float VisibilityAmount = 0f;
    private GUISkin _Skin;

    private GUIStyle LabelStyle;

    public bool ShouldDisplaySpectateButton { get; set; }

    public void Update()
    {
        float speed = 0.000001f;
        float target = Relay.Instance.ShowOptions ? 1.0f : 0.0f;
        VisibilityAmount = Mathf.Lerp(VisibilityAmount, target, 1.0f - Mathf.Pow(speed, Time.deltaTime));
    }

    public void DrawGUI()
    {
        GUI.skin = Skin;

        float height = 200f;
        float offscreenY = (-35f * 2) - (height + 50);
        float onscreenY = 35f;
        float actualY = Mathf.Lerp(offscreenY, onscreenY, VisibilityAmount);
        if (!Mathf.Approximately(actualY, offscreenY))
            GUILayout.Window(Definitions.OptionsWindowID, new Rect(35, actualY, Screen.width - 35*2, 200), DrawWindow,
                string.Empty);
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal(Skin.box);
        GUILayout.Label("FOV: " + String.Format("{0:0}", FOVOptionValue), LabelStyle);
        FOVOptionValue = GUILayout.HorizontalSlider(FOVOptionValue, CameraScript.MinimumFieldOfView,
            CameraScript.MaximumFieldOfView, Skin.customStyles[0], Skin.customStyles[1]);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal(Skin.box);
        GUILayout.Label("SENSITIVITY", LabelStyle);
        SensitivityOptionValue = GUILayout.HorizontalSlider(SensitivityOptionValue, 0.01f,
            1f, Skin.customStyles[0], Skin.customStyles[1]);
        GUILayout.EndHorizontal();



        GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

                GUILayout.BeginHorizontal(Skin.box);
                GUILayout.Label("THIRD PERSON", LabelStyle);
                IsExteriorView = GUILayout.Toggle(IsExteriorView, "");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(Skin.box);
                GUILayout.Label("INVERT AIM", LabelStyle);
                IsAimInverted = GUILayout.Toggle(IsAimInverted, "");
                GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.Space(1);

            GUILayout.BeginVertical();

                GUILayout.BeginHorizontal(Skin.box);
                GUILayout.Label("MUSIC", LabelStyle);
                ShouldPlayMusic = GUILayout.Toggle(ShouldPlayMusic, "");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(Skin.box);
                GUILayout.Label("SOUND EFFECTS", LabelStyle);
                ShouldPlaySoundEffects = GUILayout.Toggle(ShouldPlaySoundEffects, "");
                GUILayout.EndHorizontal();

            GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("ACCEPT", Skin.button))
            OnOptionsMenuWantsClosed();
        GUILayout.Space(1);
        if (Relay.Instance.CurrentServer != null)
        {
            if (GUILayout.Button("LEAVE SERVER", new GUIStyle(Skin.button) {fixedWidth = 95}))
                OnOptionsMenuWantsGoToTitle();
        }
        else
        {
            if (GUILayout.Button("QUIT GAME", new GUIStyle(Skin.button) {fixedWidth = 95}))
                OnOptionsMenuWantsQuitGame();
        }
        if (ShouldDisplaySpectateButton)
        {
            GUILayout.Space(1);
            if (GUILayout.Button("SPECTATE", new GUIStyle(Skin.button) {fixedWidth = 95}))
            {
                OnOptionsMenuWantsSpectate();
                OnOptionsMenuWantsClosed();
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

    }
}
