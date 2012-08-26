using UnityEngine;
using System.Collections;

public class DayNightCycleScript : MonoBehaviour {

    public float duration = 2.0F;
	
	public Color daylightFogColor  			= new Color(0.75F,0.89F,0.09F);
	public Color daylightCameraColor  		= new Color(0.92F,1.00F,0.98F);
	public Color daylightMaterialColor 		= new Color(0.92F,1.00F,0.98F);
	
	public Color nightlightFogColor			= new Color(0.00F,0.00F,0.00F);
	public Color nightlightCameraColor		= new Color(0.00F,0.00F,0.00F);
	public Color nightlightMaterialColor 	= new Color(0.92F,1.00F,0.98F);
	
	public Material worldTexture;
	
	void Start()
    {	
		//Debug.Log(worldTexture.color);
	}

	void Update()
    {
		float lerp = Easing.EaseInOut(Mathf.PingPong((float) Network.time, duration) / duration, EasingType.Sine);
		
		// Fix Fog
		RenderSettings.fogColor = Color.Lerp(daylightFogColor, nightlightFogColor, lerp);
		RenderSettings.fogDensity = Mathf.Lerp(0.002F, 0.006F, lerp);
		
		// Fix Camera
		Camera.main.backgroundColor = Color.Lerp(daylightCameraColor, nightlightCameraColor, lerp);
		
		// Fix Texture
	    var newColor = Color.Lerp(daylightMaterialColor, nightlightMaterialColor, lerp);
	    worldTexture.color = newColor;
        foreach (var mat in GameObject.FindGameObjectsWithTag("PlayerMaterial"))
        {
            var c = mat.renderer.material.color;
            mat.renderer.material.color = new Color(newColor.r, newColor.g, newColor.b, c.a);
        }
    }
}
