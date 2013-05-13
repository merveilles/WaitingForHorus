using UnityEngine;
using System.Collections;

public class DisableShadows : MonoBehaviour 
{
	float storedShadowDistance;
 
	void OnPreRender() 
	{
	    //storedShadowDistance = QualitySettings.shadowDistance;
	    QualitySettings.shadowDistance = 0;
	}
	 
	void OnPostRender() 
	{
	    QualitySettings.shadowDistance = 250;
	}
}
