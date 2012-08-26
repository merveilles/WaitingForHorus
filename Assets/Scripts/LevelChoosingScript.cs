using UnityEngine;
using System.Collections;

public class LevelChoosingScript : MonoBehaviour {

	void Start() 
    {
        Debug.Log(Application.levelCount);
        Application.LoadLevel(RandomHelper.Random.Next(1, Application.levelCount));
	}
}
