using UnityEngine;
using System.Collections;

public class GlobalSoundsScript : MonoBehaviour 
{
	public static bool soundEnabled = true;
    public AudioSource buttonPressSound;
	
    static GlobalSoundsScript Instance;
    static bool playing = false; //work around the fact that TaskManager does a DontDestroyOnLoad

	void Start () 
    {
        Instance = this;
	}
	
    public AudioClip[] songs;

    void Awake()
    {
        if( !playing )
        {
            playing = true;
            audio.Play();
        }
        else Destroy( this );
    }

    public static void PlayButtonPress()
    {
        Instance.buttonPressSound.Play();
    }
	
	void Update()
    {
	    if( Input.GetKeyDown("m") )
            audio.mute = !audio.mute; // Disable music
		
		if( Input.GetKeyDown("n") )
            GlobalSoundsScript.soundEnabled = !GlobalSoundsScript.soundEnabled;
	}
	
    void OnLevelWasLoaded( int levelIndex )
    {
        var indexInArray = levelIndex - 1;

        if( indexInArray < songs.Length )
        {
            audio.clip = songs[indexInArray];
            audio.Play();
        }
    }
}
