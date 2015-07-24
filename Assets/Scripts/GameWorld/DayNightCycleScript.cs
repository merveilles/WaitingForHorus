using UnityEngine;

public class DayNightCycleScript : MonoBehaviour {

    public float duration = 2.0F;
    public float MinDensity = 0.00175f;
    public float MaxDensity = 0.00275F;
	
	public Color daylightFogColor  			= new Color(0.75F,0.89F,0.09F);
	public Color daylightCameraColor  		= new Color(0.92F,1.00F,0.98F);
	public Color daylightMaterialColor 		= new Color(0.92F,1.00F,0.98F);
	
	public Color nightlightFogColor			= new Color(0.00F,0.00F,0.00F);
	public Color nightlightCameraColor		= new Color(0.00F,0.00F,0.00F);
	public Color nightlightMaterialColor 	= new Color(0.92F,1.00F,0.98F);
	
	public Material worldTexture;
    //private GameObject[] playerMaterials;

    void RecapturePlayerMaterials()
    {
        //playerMaterials = GameObject.FindGameObjectsWithTag( "PlayerMaterial" );
    }

    public void Start()
    {	
        RecapturePlayerMaterials();
	}

    public void uLink_OnPlayerConnected( )
    {
        RecapturePlayerMaterials();
    }

    public void Update()
    {
		float lerp = Easing.EaseInOut(Mathf.PingPong((float) uLink.Network.time, duration) / duration, EasingType.Sine);
		
		// Fix Fog
		RenderSettings.fogColor = Color.Lerp( daylightFogColor, nightlightFogColor, lerp );
        RenderSettings.fogDensity = Mathf.Lerp( MinDensity, MaxDensity, lerp );
		
		// Fix Camera
		Camera.main.backgroundColor = Color.Lerp(daylightCameraColor, nightlightCameraColor, lerp);
		
		// Fix Texture
	    var newColor = Color.Lerp(daylightMaterialColor, nightlightMaterialColor, lerp);
	    worldTexture.color = newColor;

        // TODO basically unusable, rewrite
        //foreach( var player in playerMaterials )
        //{
        //    var c = player.renderer.material.color;
        //    player.renderer.material.color = new Color( newColor.r, newColor.g, newColor.b, c.a );
        //}
    }
}
