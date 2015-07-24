using UnityEngine;

public class Spin : MonoBehaviour 
{
	public float Speed = 1.0f;
	public Vector3 Axis = Vector3.forward;

    public void Update() 
	{
		float s = Speed;
		transform.Rotate( Axis, s * Time.deltaTime );
	}
}
