using UnityEngine;

public class GlobalSoundsScript : MonoBehaviour 
{
	public static bool soundEnabled = true;
    public AudioSource buttonPressSound;
	
    public static GlobalSoundsScript Instance { get; private set; }
    static bool playing = false; //work around the fact that TaskManager does a DontDestroyOnLoad

    public bool PlayMusicInEditor = false;

    public void Awake()
    {
        if( !playing )
        {
            playing = true;
            audio.Play();
        }
        else Destroy( this );
        Instance = this;
    }

    public void RestartAudio()
    {
        bool shouldPlayMusic = (!Application.isEditor || PlayMusicInEditor) && !Relay.Instance.DevelopmentMode;
        var indexInArray = Application.loadedLevel - 1;

        if( shouldPlayMusic && indexInArray < songs.Length )
        {
            audio.clip = songs[indexInArray];
            audio.Play();
        }
    }

    public void Start () 
    {
        Relay.Instance.OptionsMenu.OnShouldPlaySoundEffectsOptionChanged += ReceiveSoundEffectsOptionChanged;
        Relay.Instance.OptionsMenu.OnShouldPlayMusicOptionChanged += ReceiveMusicOptionChanged;

        audio.mute = !Relay.Instance.OptionsMenu.ShouldPlayMusic;
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
        audio.mute = !isEnabled;
        RestartAudio();
    }
}
