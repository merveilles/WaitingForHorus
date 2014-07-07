using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponIndicatorScript : MonoBehaviour
{
    public static WeaponIndicatorScript Instance { get; private set; }
    public bool ShouldRender { get; set; }
    Material mat;
	float lastOpacity;
    bool isReady;
    private GUIStyle ShieldTextStyle;
    private GUIStyle DarkShieldTextStyle;

    public int BulletCapacity { get; set; }
    public int BulletsAvailable { get; set; }
    public int HealthCapacity { get; set; }
    public int HealthAvailable { get; set; }
    public int ShieldCapacity { get; set; }
    public int ShieldAvailable { get; set; }

    public class PlayerData
    {
        public PlayerScript Script;
        public Vector2 ScreenPosition;
        public float SinceInCrosshair { get { return Mathf.Max(0f, LockStrength); } }
        public bool Found;
        public bool WasLocked;
        public float TimeSinceLastLockSend;

        public float LockStrength = 0f;
        public const float LockLossTimeMultiplier = 2.8f;
        public const float LockStrengthLimitMultiplier = 2f * PlayerShootingScript.AimingTime;
    
        public bool Locked
        {
            get { return LockStrength >= PlayerShootingScript.AimingTime; }
        }

        public bool NeedsLockSent
        {
            get { return Locked && TimeSinceLastLockSend > TimeBetweenLockSends; }
        }

        public void ClampStrength()
        {
            LockStrength = Mathf.Clamp(LockStrength, 0, LockStrengthLimitMultiplier);
        }

        public const float TimeBetweenLockSends = 1f;
    }

    public List<PlayerData> Targets { get; private set; }
    public Vector2 CrosshairPosition { get; set; }

    public void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    public void Start()
    {
        mat = new Material("Shader \"Lines/Colored Blended\" {" +
                           "SubShader { Pass { " +
                           "    Blend SrcAlpha OneMinusSrcAlpha " +
                           "    ZWrite Off Cull Off Fog { Mode Off } " +
                           "    BindChannels {" +
                           "      Bind \"vertex\", vertex Bind \"color\", color }" +
                           "} } }");
        mat.hideFlags = HideFlags.HideAndDontSave;
        mat.shader.hideFlags = HideFlags.HideAndDontSave;

        Targets = new List<PlayerData>();

        ShieldTextStyle = new GUIStyle(Relay.Instance.BaseSkin.label)
        {
            padding = new RectOffset(0,0,0,0),
            margin = new RectOffset(0,0,0,0),
            fixedWidth = 0
        };
        DarkShieldTextStyle = new GUIStyle(ShieldTextStyle);
        DarkShieldTextStyle.normal.textColor = new Color(0, 0, 0, 0.2f);
    }

    public float CooldownStep { get; set; }
	
	void Render( Color color, float opacity, Vector2 size )
	{
        const float segments = 32;

        GL.PushMatrix();
        GL.LoadPixelMatrix();

        mat.SetPass(0);

        GL.Begin(GL.LINES);

        Vector2 ssPos;
		
        GL.Color(new Color(color.r, color.g, color.b, opacity));
        var radius = size * Screen.height / 1500f;
        ssPos = CrosshairPosition;
        //ssPos = new Vector2(Screen.width, Screen.height) / 2f;
        for (int i = 0; i < segments; i++)
        {
            var eased = Easing.EaseIn(CooldownStep, EasingType.Quadratic);

            var thisA = (i / segments * Mathf.PI * 2) * eased + Mathf.PI / 2;
            var nextA = ((i + 1) / segments * Mathf.PI * 2) * eased + Mathf.PI / 2;

            GL.Vertex3(ssPos.x + (float) Math.Cos(thisA) * radius.x, ssPos.y + (float) Math.Sin(thisA) * radius.y, 0);
            GL.Vertex3(ssPos.x + (float) Math.Cos(nextA) * radius.x, ssPos.y + (float) Math.Sin(nextA) * radius.y, 0);
        }

        // Targets
        GL.Color(new Color(1, 1, 1, 1));
        var edge = 20 * Screen.height / 1500f;
        var offsetSize = 5;
        var spacing = 0.8f;
        var diag = edge;

        isReady = false;
        foreach (var t in Targets)
        {
            var step = 1 -
                       Easing.EaseIn(Mathf.Clamp01(t.SinceInCrosshair / PlayerShootingScript.AimingTime),
                                     EasingType.Cubic);
            isReady |= Mathf.Approximately(step, 0);

            step *= offsetSize;

            ssPos = t.ScreenPosition;

            var p1o = step * new Vector2(-edge / 2f, -diag / 2f);
            var p2o = step * new Vector2(edge / 2f, -diag / 2f);
            var p3o = step * new Vector2(0, diag / 2f);

            var p1 = ssPos + new Vector2(-edge / 2f, -diag / 2f);
            var p2 = ssPos + new Vector2(edge / 2f, -diag / 2f);
            var p3 = ssPos + new Vector2(0, diag / 2f);

            GL.Vertex3(p1.x + p1o.x, p1.y + p1o.y, 0);
            GL.Vertex3(p1.x + edge / 2f * spacing + p1o.x, p1.y + p1o.y, 0);
            GL.Vertex3(p1.x + p1o.x, p1.y + p1o.y, 0);
            GL.Vertex3(p1.x + edge / 4f * spacing + p1o.x, p1.y + edge / 2f * spacing + p1o.y, 0);

            GL.Vertex3(p2.x + p2o.x, p2.y + p2o.y, 0);
            GL.Vertex3(p2.x - edge / 2f * spacing + p2o.x, p2.y + p2o.y, 0);
            GL.Vertex3(p2.x + p2o.x, p2.y + p2o.y, 0);
            GL.Vertex3(p2.x - edge / 4f * spacing + p2o.x, p2.y + edge / 2f * spacing + p2o.y, 0);

            GL.Vertex3(p3.x + p3o.x, p3.y + p3o.y, 0);
            GL.Vertex3(p3.x - edge / 4f * spacing + p3o.x, p3.y - edge / 2f * spacing + p3o.y, 0);
            GL.Vertex3(p3.x + p3o.x, p3.y + p3o.y, 0);
            GL.Vertex3(p3.x + edge / 4f * spacing + p3o.x, p3.y - edge / 2f * spacing + p3o.y, 0);
        }

        GL.End();

        GL.PopMatrix();
	}

    void RenderAmmo(Color color, float opacity, Vector2 origin)
    {
        GL.Color(new Color(color.r, color.g, color.b, opacity));

        DrawMeter(BulletsAvailable, BulletCapacity - BulletsAvailable, origin, 1.25f, 2.5f, 5.5f, 1.5f);
    }

    void RenderHealth(Color color, float opacity, Vector2 origin)
    {
        GL.Color(new Color(color.r, color.g, color.b, opacity));
        DrawMeter(HealthAvailable, HealthCapacity - HealthAvailable, origin, 2.25f, 2.5f, 5.5f, 1.5f);
    }

    void RenderMeters()
    {
        var radius = 130 * Screen.height / 1500f;
        Vector2 ssPos = CrosshairPosition;

        GL.PushMatrix();
        GL.LoadPixelMatrix();

        mat.SetPass(0);

        GL.Begin(GL.QUADS);
        RenderAmmo(Color.white, 0.8f, ssPos - new Vector2(radius, -radius));
        RenderAmmo(Color.black, 0.1f, ssPos - new Vector2(radius - 2, -radius));
        RenderHealth(Color.white, 0.8f, ssPos - new Vector2(radius, radius));
        RenderHealth(Color.black, 0.1f, ssPos - new Vector2(radius - 3, radius));
        GL.End();

        GL.PopMatrix();
    }

    private void Rect(Vector2 origin, Vector2 end)
    {
        GL.Vertex3(origin.x, origin.y, 0);
        GL.Vertex3(end.x, origin.y, 0);
        GL.Vertex3(end.x, end.y, 0);
        GL.Vertex3(origin.x, end.y, 0);
    }

    private void DrawMeter(int filledSegments, int emptySegments, Vector2 origin, float segmentWidth, float gapWidth,
        float filledSegmentHeight, float emptySegmentHeight)
    {
        Vector2 originAccum = origin;
        for (int i = 0; i < filledSegments; i++)
        {
            Rect(originAccum, originAccum + new Vector2(segmentWidth, filledSegmentHeight));
            originAccum += new Vector2(segmentWidth + gapWidth, 0);
        }
        for (int i = 0; i < emptySegments; i++)
        {
            Rect(originAccum, originAccum + new Vector2(segmentWidth, emptySegmentHeight));
            originAccum += new Vector2(segmentWidth + gapWidth, 0);
        }
    }

    public IEnumerator OnPostRender()
    {
        yield return new WaitForEndOfFrame();

        if (ShouldRender)
        {
            // Circle around
			var opacity = Mathf.Lerp(lastOpacity, CooldownStep < 1 ? 1 : 0.3f, 0.1f);
			lastOpacity = opacity;
            var color = Color.white;
            if (isReady)
            {
                opacity = 1;
                //color = Color.Lerp(Color.white, Color.red, Mathf.Sin(Time.realtimeSinceStartup * 60) / 2f + 0.5f);
                color = Color.red;
            }
			
			Render( color, opacity * 1.75f, new Vector2(130, 130) );
			Render( Color.black, 0.075f, new Vector2(128, 128) );
            RenderMeters();
			//Render( Color.black, 0.1f, new Vector2(132, 132) );

        }
    }

    public void OnGUI()
    {
        GUI.skin = Relay.Instance.BaseSkin;

        var radius = 130 * Screen.height / 1500f;
        Vector2 ssPos = CrosshairPosition;
        ssPos.y = Screen.height - ssPos.y;
        //Vector2 topRight = new Vector2(ssPos.x + radius, ssPos.y - radius);
        Vector2 botRight = new Vector2(ssPos.x + radius, ssPos.y + radius);
        string shieldText = ShieldAvailable > 0 ? "SHIELD" : "*****";
        Vector2 labelSize = ShieldTextStyle.CalcSize(new GUIContent(shieldText));
        float xOffset = 8f;
        float yOffset = 4f;
        GUI.Label(new Rect(xOffset + botRight.x - labelSize.x, yOffset + botRight.y - labelSize.y, xOffset + labelSize.x, yOffset + labelSize.y), shieldText, DarkShieldTextStyle);
        xOffset += 1f;
        yOffset += 1f;
        GUI.Label(new Rect(xOffset + botRight.x - labelSize.x, yOffset + botRight.y - labelSize.y, xOffset + labelSize.x, yOffset + labelSize.y), shieldText, ShieldTextStyle);
    }
}
