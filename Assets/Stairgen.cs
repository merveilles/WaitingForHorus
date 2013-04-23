using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class Stairgen : MonoBehaviour
{
	public int Length = 20;
	public float Probablity = 1.0f;
	public int Seed = 1;
	public Vector3 StepScale;
	public Vector3 RotateDirection;
	public Vector3 RotateOffset;
	public GameObject[] StepFab;
	public GameObject DummyFab;
	
	private GameObject lastStep;
	
	public void Build()
	{
		print( "Deleting Old..." );
		
		var children = new List<GameObject>();
		foreach( Transform child in transform ) children.Add( child.gameObject );
		children.ForEach( child => DestroyImmediate( child ) );
		
		print( "Building..." );
		
		lastStep = gameObject;
		
		int y = 0, z = 0, cf = 0;
		while( z < Length )
		{
			GameObject Fab = StepFab[cf];
			SpawnHex( Fab, z );
			cf++;
			if( cf > StepFab.GetLength(0) - 1 ) cf = 0;
			y++;
			z++;
		}
	}
	
	void SpawnHex( GameObject fab, int index )
	{
		GameObject dummy = (GameObject)Instantiate( DummyFab, lastStep.transform.position, lastStep.transform.rotation );
		dummy.transform.parent = lastStep.transform;
		
		//if( lastStep != gameObject ) 
		//{
			dummy.transform.Rotate( RotateDirection );
			dummy.transform.Translate( StepScale );
		//}
		
		Random.seed = index * Seed;
		if( Probablity > Random.value )
		{
			GameObject step = (GameObject)Instantiate( fab, lastStep.transform.position, lastStep.transform.rotation );
			step.transform.parent = dummy.transform;
			step.transform.Rotate( RotateOffset );
		}
		
		lastStep = dummy;
	}
	
	void Update()
	{
		if( Application.isEditor ) Build();
	}
}