using UnityEngine;

public class GlobalSoundsScript : MonoBehaviour 
{
	public static bool soundEnabled = true;
    public AudioSource buttonPressSound;

    public AudioSource ChatMessageSound;
    public AudioSource ServerMessageSound;
	
    public static GlobalSoundsScript Instance { get; private set; }
    static bool playing = false; //work around the fact that TaskManager does a DontDestroyOnLoad

    public bool PlayMusicInEditor = false;

    public void Awake()
    {
        if( !playing )
        {
            playing = true;
            GetComponent<AudioSource>().Play();
        }
        else Destroy( this );
        Instance = this;
    }

    public void RestartAudio()
    {
		bool shouldPlayMusic = (!Application.isEditor || PlayMusicInEditor) && !Relay.Instance.DevelopmentMode;
		var indexInArray = Application.loadedLevel - 1;
		if (indexInArray >= songs.Length) 
		{
			// we don't have a music track for level with that index, just play the first one
			indexInArray = 0;
		}

        if( shouldPlayMusic && indexInArray < songs.Length )
        {
            GetComponent<AudioSource>().clip = songs[indexInArray];
            GetComponent<AudioSource>().Play();
        }
    }

    public void Start () 
    {
        Relay.Instance.OptionsMenu.OnShouldPlaySoundEffectsOptionChanged += ReceiveSoundEffectsOptionChanged;
        Relay.Instance.OptionsMenu.OnShouldPlayMusicOptionChanged += ReceiveMusicOptionChanged;

        GetComponent<AudioSource>().mute = !Relay.Instance.OptionsMenu.ShouldPlayMusic;
    }

    public void OnDestroy()
    {
        Relay.Instance.OptionsMenu.OnShouldPlaySoundEffectsOptionChanged -= ReceiveSoundEffectsOptionChanged;
        Relay.Instance.OptionsMenu.OnShouldPlayMusicOptionChanged -= ReceiveMusicOptionChanged;
    }
	
    public AudioClip[] songs;

    public static void PlayButtonPress()
    {
        if (Instance != null)
            Instance.buttonPressSound.Play();
    }

    public static void PlayChatMessageSound()
    {
        if (Instance != null)
            Instance.ChatMessageSound.Play();
    }

    public static void PlayServerMessageSound()
    {
        if (Instance != null)
            Instance.ServerMessageSound.Play();
    }

    public void Update()
    {
	}

    public void OnLevelWasLoaded( int levelIndex )
    {
        RestartAudio();
    }

    private void ReceiveSoundEffectsOptionChanged(bool isEnabled)
    {
        soundEnabled = isEnabled;
    }
    private void ReceiveMusicOptionChanged(bool isEnabled)
    {
        GetComponent<AudioSource>().mute = !isEnabled;
        RestartAudio();
    }
}
