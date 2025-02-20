using System;
using UnityEngine;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }

    [Serializable]
    public enum SFX {
        BtnClick,
        DialogAppear,
        DialogDissapear
    }

    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;

    [SerializeField] AudioClip sfxBtnClick;
    [SerializeField] AudioClip sfxDialogAppear;
    [SerializeField] AudioClip sfxDialogDissappear;

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

    public void StopMusic() {
        if (musicSource.isPlaying) {
            musicSource.Stop();
        }
    }

    public void PlayMusic(AudioClip audio) {
        if (musicSource.isPlaying) {
            musicSource.Stop();
        }

        musicSource.clip = audio;
        musicSource.Play();
    }

    public void PlaySfx(AudioClip audio) {
        musicSource.PlayOneShot(audio);
    }

    public void PlaySfx(int sfxKey) {
        AudioClip clip;
        switch ((SFX)sfxKey) {
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

        musicSource.PlayOneShot(clip);
    }

    public void PlaySfx(AudioClip audio, AudioSource sfxSource = null) {
        if (sfxSource == null) {
            musicSource.PlayOneShot(audio);
        }

        sfxSource.PlayOneShot(audio);
    }
}
