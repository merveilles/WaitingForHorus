using UnityEngine;
using System.Collections;

public class LevelChoosingScript : MonoBehaviour {

	void Start() 
    {
        Application.LoadLevel(RandomHelper.Random.Next(1, Application.levelCount));
	}
}
