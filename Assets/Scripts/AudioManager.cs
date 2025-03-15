using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Audio manager provides ways to play background music
/// and sound effects.
/// </summary>
/// <remarks>
/// <para>
/// Contains shared SFXes internally.
/// </para>
/// <para>
/// The instance is not destructible, as it has to be present
/// across scenes.
/// </para>
/// </remarks>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    /// <summary>
    /// General sound effects used across the scenes.
    /// </summary>
    public enum Sfx
    {
        BtnClick,
        DialogAppear,
        DialogDisappear
    }

    #region Audio Sources

    /// <summary>
    /// Background music source for looped tracks.
    /// </summary>
    [SerializeField]
    private AudioSource musicSource;

    /// <summary>
    /// Sound effects source used for short
    /// PlayOneShot clips.
    ///
    /// As Play interferes with PlayOneShot,
    /// the longer SFXes that require pause
    /// capability shall use sfxPausableSource.
    /// </summary>
    [SerializeField]
    private AudioSource sfxSource;

    /// <summary>
    /// Source for pausable sound effects.
    /// </summary>
    [SerializeField]
    private AudioSource sfxPausableSource;

    #endregion

    #region Common SFX Clips

    [SerializeField]
    private AudioClip sfxBtnClick;
    [SerializeField]
    private AudioClip sfxDialogAppear;
    [SerializeField]
    private AudioClip sfxDialogDisappear;

    #endregion
    
    private void Start()
    {
        // the game is relatively small, simply mute the sources
        MuteSfx(!GameManager.IsSFXOn());
    }

    private void Awake()
    {
        // prevent multiple instances
        if (Instance != null && Instance != this)
        {
            // destroy script and the entire object that has audio sources
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // prevent destruction
            DontDestroyOnLoad(this);
        }
    }

    #region Background Music Source

    /// <summary>
    /// Background source pause/unpause.
    /// </summary>
    /// <param name="paused"></param>
    public void PauseMusic(bool paused)
    {
        if (paused)
        {
            musicSource.Pause();
        }
        else
        {
            musicSource.UnPause();
        }
    }

    /// <summary>
    /// Stops background source.
    /// </summary>
    public void StopMusic()
    {
        musicSource.Stop();
    }

    /// <summary>
    /// Starts background source if it has an audio clip.
    /// </summary>
    public void PlayMusic()
    {
        if (musicSource.clip != null)
        {
            musicSource.Play();
        }
    }

    /// <summary>
    /// Starts to play audio clip with background audio source.
    /// </summary>
    /// <remarks>
    /// Only sets the audio clip to the source when
    /// music is disabled.
    /// </remarks>
    /// <param name="clip">Music audio clip.</param>
    public void PlayMusic(AudioClip clip)
    {
        Assert.IsNotNull(clip, "music audio clip shall not be null");
        musicSource.Stop();
        // set the clip for potential music enable
        musicSource.clip = clip;
        musicSource.time = 0;
        if (GameManager.IsMusicOn())
        {
            musicSource.Play();
        }
    }

    #endregion

    #region Pausable SFX source

    /// <summary>
    /// Pausable SFX source pause/unpause.
    /// </summary>
    /// <param name="paused"></param>
    public void PausableSfxPause(bool paused)
    {
        if (paused)
        {
            sfxPausableSource.Pause();
        }
        else
        {
            sfxPausableSource.UnPause();
        }
    }

    public void StopSfxPausable()
    {
        sfxPausableSource.Stop();
    }

    /// <summary>
    /// Starts to play audio clip with sfx pausable audio source.
    /// </summary>
    /// <param name="clip">Music audio clip.</param>
    public void PlaySfxPausable(AudioClip clip)
    {
        Assert.IsNotNull(clip, "sfx audio clip shall not be null");
        sfxPausableSource.Stop();
        sfxPausableSource.clip = clip;
        sfxPausableSource.time = 0;
        sfxPausableSource.Play();
    }

    #endregion

    #region SFX Source

    /// <summary>
    /// Plays one shot audio clip with sfx audio source.
    /// </summary>
    /// <param name="clip">Music audio clip.</param>
    public void PlaySfx(AudioClip clip)
    {
        Assert.IsNotNull(clip, "sfx audio clip shall not be null");
        Assert.IsNotNull(clip);
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Plays one shot stored SFX with sfx audio source.
    /// </summary>
    /// <remarks>
    /// Ignores invalid values.
    /// </remarks>
    /// <param name="sfxKey">SFX to play.</param>
    public void PlaySfx(Sfx sfxKey)
    {
        AudioClip clip;
        switch (sfxKey)
        {
            case Sfx.BtnClick:
                clip = sfxBtnClick;
                break;
            case Sfx.DialogAppear:
                clip = sfxDialogAppear;
                break;
            case Sfx.DialogDisappear:
                clip = sfxDialogDisappear;
                break;
            default:
                Logger.Debug($"No SFX found for key {sfxKey}");
                return;
        }

        sfxSource.PlayOneShot(clip);
    }

    #endregion

    #region SFX

    /// <summary>
    /// Mutes SFX and Pausable SFX sources.
    /// </summary>
    /// <param name="value"></param>
    public void MuteSfx(bool value)
    {
        sfxSource.mute = value;
        sfxPausableSource.mute = value;
    }

    #endregion
}