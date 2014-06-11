using UnityEngine;
using System.Collections;

public class RemovePerGFXMode : MonoBehaviour 
{
	public int MinimumMode = 2;
	public MonoBehaviour Target;
	
	void Awake() 
	{
		//print( QualitySettings.GetQualityLevel() );
		if( QualitySettings.GetQualityLevel() < MinimumMode )
		{
			if( Target != null )
				Destroy( Target );
			else
				Destroy( gameObject );
		}	
	}
}
