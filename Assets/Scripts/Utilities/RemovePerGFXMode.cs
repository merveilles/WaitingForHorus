using UnityEngine;

public class RemovePerGFXMode : MonoBehaviour 
{
	public int MinimumMode = 2;
	public MonoBehaviour Target;

    public void Awake() 
	{
		if( QualitySettings.GetQualityLevel() < MinimumMode )
		{
			if( Target != null )
				Destroy( Target );
			else
				Destroy( gameObject );
		}	
	}
}
