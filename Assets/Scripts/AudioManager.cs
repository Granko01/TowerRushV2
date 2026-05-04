using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("SFX")]
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip cementUse;
    [SerializeField] private AudioClip dropSound;
    [SerializeField] private AudioClip youWin;
    [SerializeField] private AudioClip youLost;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource      = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayButtonClick() { if (buttonClick) sfxSource.PlayOneShot(buttonClick); }
    public void PlayCementUse()   { if (cementUse)   sfxSource.PlayOneShot(cementUse); }
    public void PlayDrop()        { if (dropSound)    sfxSource.PlayOneShot(dropSound); }
    public void PlayWin()         { if (youWin)       sfxSource.PlayOneShot(youWin); }
    public void PlayLose()        { if (youLost)      sfxSource.PlayOneShot(youLost); }
}
