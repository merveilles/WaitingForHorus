using UnityEngine;

public class DisableShadows : MonoBehaviour 
{
    public void OnPreRender() 
	{
	    //storedShadowDistance = QualitySettings.shadowDistance;
	    QualitySettings.shadowDistance = 0;
	}

    public void OnPostRender() 
	{
	    QualitySettings.shadowDistance = 250;
	}
}