using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Audio manager provides ways to play background music
/// and sound effects.
///
/// The instance is not destructable, as it has to be present
/// across the scenes.
///
/// Contains shared SFXes internally.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    /// <summary>
    /// General sound effects used across the scenes.
    /// </summary>
    public enum SFX
    {
        BtnClick,
        DialogAppear,
        DialogDissapear
    }

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

    #region Common SFX Clips
    [SerializeField]
    private AudioClip sfxBtnClick;
    [SerializeField]
    private AudioClip sfxDialogAppear;
    [SerializeField]
    private AudioClip sfxDialogDissappear;
    #endregion

    private void Awake()
    {
        // prevent mutliple instances
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
    /// <param name="source"></param>
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
    /// Starts to play audio clip with  background audio source.
    ///
    /// Only sets the audio clip to the source when
    /// music is disabled.
    /// </summary>
    /// <param name="audio"></param>
    public void PlayMusic(AudioClip audio)
    {
        musicSource.Stop();

        // set the clip for potential music enable
        musicSource.clip = audio;
        musicSource.time = 0;

        if (GameManager.Instance.IsMusicOn())
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

    public void PlaySfxPausable(AudioClip audio)
    {
        if (sfxPausableSource.isPlaying)
        {
            sfxPausableSource.Stop();
        }

        sfxPausableSource.clip = audio;
        sfxPausableSource.time = 0;
        sfxPausableSource.Play();
    }
    #endregion

    #region SFX Source
    public void PlaySfx(AudioClip audio)
    {
        Assert.IsNotNull(audio);

        sfxSource.PlayOneShot(audio);
    }

    public void PlaySfx(SFX sfxKey)
    {
        AudioClip clip;
        switch (sfxKey)
        {
            case SFX.BtnClick:
                clip = sfxBtnClick;
                break;
            case SFX.DialogAppear:
                clip = sfxDialogAppear;
                break;
            case SFX.DialogDissapear:
                clip = sfxDialogDissappear;
                break;
            default:
                Debug.Log("No SFX found for key " + sfxKey);
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
