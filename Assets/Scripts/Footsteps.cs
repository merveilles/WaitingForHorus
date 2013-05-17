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
	public AudioClip[] SoundsWater;
	public AudioSource[] AudioSources;
	public float WaterHeight = -1.45f;
	public bool IsMoving = false;
	public bool PlayerStep = true;
	
	private int currentAudioSource = 0;
	
	void Start() 
	{
		StartCoroutine("Step");
	}
	
	IEnumerator Step()
	{
		while( true )
		{
			if( PlayerStep && ( Input.GetAxis( "Strafe" ) != 0.0f || Input.GetAxis( "Thrust" ) != 0.0f ) || IsMoving )
			{
				if( Physics.Raycast( transform.position, Vector3.down, StepHeight ) ) 
				{
					AudioClip[] sounds = SoundsNormal; 
					if( transform.position.y < WaterHeight) sounds = SoundsWater;
					
			        currentAudioSource++;
			        if( currentAudioSource > AudioSources.Length - 1 )
			            currentAudioSource = 0;
					
					AudioSources[currentAudioSource].volume = Volume - Random.value * StepVolume;
					AudioSources[currentAudioSource].pitch = 1.0f - Random.value * StepPitch;
					AudioSources[currentAudioSource].PlayOneShot( sounds[(int)(Random.value * sounds.Length)] );
				}
			}
			
			yield return new WaitForSeconds( StepOffset + Random.value * StepDelay );
		}
	}
}
