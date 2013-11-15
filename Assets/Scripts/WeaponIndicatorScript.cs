using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class WeaponIndicatorScript : MonoBehaviour
{
    Material mat;
	float lastOpacity;
    bool isReady;

    public class PlayerData
    {
        public PlayerScript Script;
        public Vector2 ScreenPosition;
        public float SinceInCrosshair;
        public bool Found;
        public bool WasLocked;
    
        public bool Locked
        {
            get { return SinceInCrosshair >= PlayerShootingScript.AimingTime; }
        }
    }

    public List<PlayerData> Targets { get; private set; }
    public Vector2 CrosshairPosition { get; set; }

    void Start()
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
    }

    public float CooldownStep { get; set; }
	
	void Render( Color color, float opacity, Vector2 size )
	{
        const float Segments = 32;

        GL.PushMatrix();
        GL.LoadPixelMatrix();

        mat.SetPass(0);

        GL.Begin(GL.LINES);

        Vector2 ssPos;
		
        GL.Color(new Color(color.r, color.g, color.b, opacity));
        var radius = size * Screen.height / 1500f;
        ssPos = CrosshairPosition;
        //ssPos = new Vector2(Screen.width, Screen.height) / 2f;
        for (int i = 0; i < Segments; i++)
        {
            var eased = Easing.EaseIn(CooldownStep, EasingType.Quadratic);

            var thisA = (i / Segments * Mathf.PI * 2) * eased + Mathf.PI / 2;
            var nextA = ((i + 1) / Segments * Mathf.PI * 2) * eased + Mathf.PI / 2;

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
            isReady |= step == 0;

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

    IEnumerator OnPostRender()
    {
        yield return new WaitForEndOfFrame();

        if (!RoundScript.Instance.RoundStopped && !ServerScript.IsAsyncLoading && !ServerScript.Spectating)
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
			//Render( Color.black, 0.1f, new Vector2(132, 132) );
        }
    }
}
