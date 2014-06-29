using System.Collections.Generic;
using UnityEngine;

public class Leaderboard
{
    private GUISkin _Skin;
    public GUISkin Skin
    {
        get { return _Skin; }
        set
        {
            _Skin = value;
            
            var ThinPadding = new RectOffset();
            ThinPadding.left = Skin.box.padding.left;
            ThinPadding.top = 5;
            ThinPadding.bottom = 5;
            ThinPadding.right = Skin.box.padding.right;

            NameBoxStyle = new GUIStyle(Skin.box)
            {
                fixedWidth = 150,
                alignment = TextAnchor.MiddleRight,
            };
            ScoreBoxStyle = new GUIStyle(Skin.box)
            {
                fixedWidth = 80,
                alignment = TextAnchor.MiddleLeft,
            };
            NameTitleStyle = new GUIStyle(NameBoxStyle)
            {padding = ThinPadding};
            ScoreTitleStyle = new GUIStyle(ScoreBoxStyle)
            {padding = ThinPadding};
        }
    }

    private GUIStyle NameTitleStyle;
    private GUIStyle ScoreTitleStyle;
    private GUIStyle NameBoxStyle;
    private GUIStyle ScoreBoxStyle;

    public bool Show { get; set; }

    private float ShownAmount = 0f;

    private readonly List<PlayerPresence> PresenceCache;

    public Leaderboard()
    {
        PresenceCache = new List<PlayerPresence>();
    }

    public void Start()
    {

    }

    public void Update()
    {
        float target = Show ? 1f : 0f;
        float speed = 0.0000000001f;
        ShownAmount = Mathf.Lerp(ShownAmount, target, 1.0f - Mathf.Pow(speed, Time.deltaTime));
    }

    public void DrawGUI()
    {
        PresenceCache.Clear();
        PresenceCache.AddRange(PlayerPresence.UnsafeAllPlayerPresences);
        PresenceCache.Sort((p1, p2) => p1.Score.CompareTo(p2.Score));

        GUI.skin = Skin;
        float offScreen = -300;
        float onScreen = 35;
        float xPosition = Mathf.Lerp(offScreen, onScreen, ShownAmount);
        if (!Mathf.Approximately(xPosition, offScreen))
    	    GUILayout.Window(10, new Rect(xPosition, 35, 300, Screen.height), DisplayLeaderboard, string.Empty);
    }

    private void DisplayLeaderboard(int id)
    {
        GUI.skin = Skin;
        GUILayout.BeginHorizontal();
        GUILayout.Box("NAME", NameTitleStyle);
        GUILayout.Space(1);
        GUILayout.Box("SCORE", ScoreTitleStyle);
        GUILayout.EndHorizontal();
        for (int i = 0; i < PresenceCache.Count; i++)
        {
            var presence = PresenceCache[i];
            GUILayout.BeginHorizontal();
            GUILayout.Box(presence.Name, NameBoxStyle);
            GUILayout.Space(1);
            GUILayout.Box(presence.Score.ToString(), ScoreBoxStyle);
            GUILayout.EndHorizontal();
        }
    }
}
