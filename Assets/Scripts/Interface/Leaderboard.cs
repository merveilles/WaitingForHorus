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

            StatusMessageStyle = new GUIStyle(Skin.box)
            {
                fixedWidth = 231,
                alignment = TextAnchor.MiddleCenter,
                padding = ThinPadding
            };

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

    private GUIStyle StatusMessageStyle;

    private GUIStyle NameTitleStyle;
    private GUIStyle ScoreTitleStyle;
    private GUIStyle NameBoxStyle;
    private GUIStyle ScoreBoxStyle;

    public bool Show { get; set; }

    private float ShownAmount = 0f;

    private readonly List<PlayerPresence> CombatantsCache;
    private readonly List<PlayerPresence> SpectatorsCache;

    public Leaderboard()
    {
        CombatantsCache = new List<PlayerPresence>();
        SpectatorsCache = new List<PlayerPresence>();
    }

    public void Start()
    {

    }

    public void Update()
    {
        float target = Show ? 1f : 0f;
        float speed = 0.0000000001f;
        ShownAmount = Mathf.Lerp(ShownAmount, target, 1.0f - Mathf.Pow(speed, Time.deltaTime));

        CombatantsCache.Clear();
        SpectatorsCache.Clear();
        for (int i = 0; i < PlayerPresence.UnsafeAllPlayerPresences.Count; i++)
        {
            var presence = PlayerPresence.UnsafeAllPlayerPresences[i];
            if (presence.IsSpectating)
                SpectatorsCache.Add(presence);
            else
                CombatantsCache.Add(presence);
        }
        CombatantsCache.Sort((p1, p2) => p2.Score.CompareTo(p1.Score));
        SpectatorsCache.Sort((p1, p2) => System.String.Compare(p1.Name, p2.Name, System.StringComparison.Ordinal));
    }

    public void DrawGUI()
    {
        GUI.skin = Skin;
        float offScreen = -300;
        float onScreen = 35;
        float xPosition = Mathf.Lerp(offScreen, onScreen, ShownAmount);
        if (!Mathf.Approximately(xPosition, offScreen))
    	    GUILayout.Window(Definitions.LeaderboardWindowID, new Rect(xPosition, 35, 300, Screen.height), DisplayLeaderboard, string.Empty);
    }

    private void DisplayLeaderboard(int id)
    {
        GUI.skin = Skin;
        GUILayout.BeginVertical();

        // Silly check
        if (Relay.Instance.CurrentServer != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Box(Relay.Instance.CurrentServer.StatusMessage, StatusMessageStyle);
            GUILayout.EndHorizontal();
        }

        if (CombatantsCache.Count > 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Box("COMBATANT NAME", NameTitleStyle);
            GUILayout.Space(1);
            GUILayout.Box("SCORE", ScoreTitleStyle);
            GUILayout.EndHorizontal();
            for (int i = 0; i < CombatantsCache.Count; i++)
            {
                var presence = CombatantsCache[i];
                GUILayout.BeginHorizontal();
                GUILayout.Box(presence.Name, NameBoxStyle);
                GUILayout.Space(1);
                GUILayout.Box(presence.Score.ToString(), ScoreBoxStyle);
                GUILayout.EndHorizontal();
            }
        }

        //if (CombatantsCache.Count > 0 && SpectatorsCache.Count > 0)
        //    GUILayout.Space(10);

        if (SpectatorsCache.Count > 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Box("SPECTATOR NAME", NameTitleStyle);
            GUILayout.EndHorizontal();
            for (int i = 0; i < SpectatorsCache.Count; i++)
            {
                var presence = SpectatorsCache[i];
                GUILayout.BeginHorizontal();
                GUILayout.Box(presence.Name, NameBoxStyle);
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndVertical();
    }
}
