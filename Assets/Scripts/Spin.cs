using UnityEngine;
using System.Collections;

public class Spin : MonoBehaviour 
{
	public float Speed = 1.0f;
	public bool PlayerProximityActive = false;
	public float ProximityThreshold = 2.0f;
	public float ProximityMultiplier = 4.0f;
	public Vector3 Axis = Vector3.forward;
	
	void Update() 
	{
		float s = Speed;
		
		if( PlayerProximityActive )
		{
			GameObject[] plrs = GameObject.FindGameObjectsWithTag( "Player" );
			foreach( GameObject plr in plrs ) 
			if( Vector3.Distance( plr.transform.position, gameObject.transform.position ) < ProximityThreshold ) s = Speed * ProximityMultiplier;
		}
		
		transform.Rotate( Axis, s * Time.deltaTime );
	}
}
