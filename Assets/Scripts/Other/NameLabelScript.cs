using UnityEngine;

public class NameLabelScript : MonoBehaviour 
{
    public void LateUpdate()
	{
		transform.rotation = Camera.main.transform.rotation;
		var distance = Mathf.Sqrt(Vector3.Distance(Camera.main.transform.position, transform.position)) / 10;
		if (distance > 5) distance = 5;
		transform.localScale = new Vector3(distance, distance, distance);
	}
}
