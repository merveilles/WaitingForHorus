using UnityEngine;
using System.Collections;

public class MusicScript : MonoBehaviour {
	void Update () {
	    if(Input.GetKeyDown("m"))
        {
            audio.mute = !audio.mute;
        }
	}
}
