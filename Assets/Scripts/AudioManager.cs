using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Serializable]
    public enum SFX
    {
        BtnClick,
        DialogAppear,
        DialogDissapear
    }

    [SerializeField]
    private AudioSource musicSource;
    [SerializeField]
    private AudioSource sfxSource;
    [SerializeField]
    private AudioSource sfxPausableSource;

    [SerializeField]
    private AudioClip sfxBtnClick;
    [SerializeField]
    private AudioClip sfxDialogAppear;
    [SerializeField]
    private AudioClip sfxDialogDissappear;

    private void Awake()
    {
        // prevent mutliple instances
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            // prevent destruction
            DontDestroyOnLoad(this);
        }
    }

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

    public void PauseSfx(bool paused)
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

    private void PlayPausableClip(AudioClip audio, AudioSource source)
    {
        if (source.isPlaying)
        {
            source.Stop();
        }

        source.clip = audio;
        source.Play();
    }

    private void StopAudioSource(AudioSource source)
    {
        if (source.isPlaying)
        {
            source.Stop();
        }
    }

    public void StopMusic()
    {
        StopAudioSource(musicSource);
    }

    public void PlayMusic(AudioClip audio)
    {
        PlayPausableClip(audio, musicSource);
    }

    public void StopSfxPausable()
    {
        StopAudioSource(sfxPausableSource);
    }

    public void PlaySfxPausable(AudioClip audio)
    {
        PlayPausableClip(audio, sfxPausableSource);
    }

    public void PlaySfx(AudioClip audio)
    {
        sfxSource.PlayOneShot(audio);
    }

    public void PlaySfx(int sfxKey)
    {
        AudioClip clip;
        switch ((SFX)sfxKey)
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
}
