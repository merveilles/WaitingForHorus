using UnityEngine;

public class GlobalSoundsScript : MonoBehaviour 
{
	public static bool soundEnabled = true;
    public AudioSource buttonPressSound;
	
    static GlobalSoundsScript Instance;
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
    }

    public void Start () 
    {
        Instance = this;

        Relay.Instance.OptionsMenu.OnShouldPlaySoundEffectsOptionChanged += ReceiveSoundEffectsOptionChanged;
        Relay.Instance.OptionsMenu.OnShouldPlayMusicOptionChanged += ReceiveMusicOptionChanged;
    }

    public void OnDestroy()
    {
        Relay.Instance.OptionsMenu.OnShouldPlaySoundEffectsOptionChanged -= ReceiveSoundEffectsOptionChanged;
        Relay.Instance.OptionsMenu.OnShouldPlayMusicOptionChanged -= ReceiveMusicOptionChanged;
    }
	
    public AudioClip[] songs;

    public static void PlayButtonPress()
    {
        Instance.buttonPressSound.Play();
    }

    public void Update()
    {
	}

    public void OnLevelWasLoaded( int levelIndex )
    {
        bool shouldPlayMusic = (!Application.isEditor || PlayMusicInEditor) && !Relay.Instance.DevelopmentMode;
        var indexInArray = levelIndex - 1;

        if( shouldPlayMusic && indexInArray < songs.Length )
        {
            audio.clip = songs[indexInArray];
            audio.Play();
        }
    }

    private void ReceiveSoundEffectsOptionChanged(bool isEnabled)
    {
        soundEnabled = isEnabled;
    }
    private void ReceiveMusicOptionChanged(bool isEnabled)
    {
        audio.mute = !isEnabled;
    }
}
