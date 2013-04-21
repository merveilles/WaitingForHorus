using UnityEngine;
using System.Collections;

public class GlobalSoundsScript : MonoBehaviour 
{
    static GlobalSoundsScript Instance;
	
	public static bool soundEnabled = true;

    public AudioSource buttonPressSound;

	void Start () 
    {
        Instance = this;
	}

    public static void PlayButtonPress()
    {
        Instance.buttonPressSound.Play();
    }
	
}
