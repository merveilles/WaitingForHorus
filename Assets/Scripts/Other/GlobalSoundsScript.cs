using UnityEngine;

public class GlobalSoundsScript : MonoBehaviour 
{
	public static bool soundEnabled = true;
    public AudioSource buttonPressSound;
	
    static GlobalSoundsScript Instance;
    static bool playing = false; //work around the fact that TaskManager does a DontDestroyOnLoad

    public bool PlayMusicInEditor = false;

    public void Start () 
    {
        Instance = this;
	}
	
    public AudioClip[] songs;

    public void Awake()
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

    public void Update()
    {
	    if( Input.GetKeyDown("m") )
            audio.mute = !audio.mute; // Disable music
		
		if( Input.GetKeyDown("n") )
            soundEnabled = !soundEnabled;
	}

    public void OnLevelWasLoaded( int levelIndex )
    {
        bool shouldPlayMusic = !Application.isEditor || PlayMusicInEditor;
        var indexInArray = levelIndex - 1;

        if( shouldPlayMusic && indexInArray < songs.Length )
        {
            audio.clip = songs[indexInArray];
            audio.Play();
        }
    }
}
