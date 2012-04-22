using UnityEngine;
using System.Collections;

public class MusicScript : MonoBehaviour
{
    // hack to work around the fact that TaskManager does a DontDestroyOnLoad
    // on us
    static bool playing = false;
    void Awake()
    {
        if(!playing)
        {
            playing = true;
            audio.Play();
        }
        else
        {
            Destroy(this);
        }
    }
    
	void Update ()
    {
	    if(Input.GetKeyDown("m"))
        {
            audio.mute = !audio.mute;
        }
	}
}
