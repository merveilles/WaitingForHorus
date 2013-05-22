using UnityEngine;
using System.Collections;

public class Footsteps : MonoBehaviour 
{
	public float Volume = 0.25f;
	public float StepHeight = 0.1f;
	public float StepDelay = 1.0f;
	public float StepOffset = 0.25f;
	public float StepVolume = 0.25f;
	public float StepPitch = 0.125f;
	public AudioClip[] SoundsNormal;
	public AudioSource[] AudioSources;
	
	private int currentAudioSource = 0;
	private bool Walking = false;
	
	bool CheckStep()
	{
		return ( Input.GetAxis( "Strafe" ) != 0.0f || Input.GetAxis( "Thrust" ) != 0.0f ) && Physics.Raycast( transform.position, Vector3.down, StepHeight );
	}
	
	void Update()
	{
		if( CheckStep() && !Walking )
			StartCoroutine( "Step" );
	}
	
	IEnumerator Step()
	{
		Walking = true;
		while( CheckStep() )
		{
			AudioClip[] sounds = SoundsNormal; 
			//if( transform.position.y < WaterHeight) sounds = SoundsWater;
			
	        currentAudioSource++;
	        if( currentAudioSource > AudioSources.Length - 1 )
	            currentAudioSource = 0;
			
			AudioSources[currentAudioSource].volume = 1.0f - Random.value * StepVolume;
			AudioSources[currentAudioSource].pitch = 1.0f - Random.value * StepPitch;
			AudioSources[currentAudioSource].PlayOneShot( SoundsNormal[(int)(Random.value * sounds.Length)] );

			yield return new WaitForSeconds( StepOffset + Random.value * StepDelay );
		}
		Walking = false;
	}
}
