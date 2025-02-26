using UnityEngine;
using UnityEngine.UI;

public class UIOptionsCtrl : MonoBehaviour
{
    private Toggle sfxToggle;
    private Toggle musicToggle;

    void Awake()
    {
        sfxToggle = transform.Find("Sfx").GetComponent<Toggle>();
        musicToggle = transform.Find("Audio").GetComponent<Toggle>();
    }

    void Start()
    {
        sfxToggle.SetIsOnWithoutNotify(GameManager.Instance.IsSFXOn());
        musicToggle.SetIsOnWithoutNotify(GameManager.Instance.IsMusicOn());
    }

    public void OnSfxValueChanged(bool value)
    {
        AudioManager.Instance.PlaySfx(AudioManager.SFX.BtnClick);
        GameManager.Instance.ToggleSFX();
    }

    public void OnMusicValueChanged(bool value)
    {
        AudioManager.Instance.PlaySfx(AudioManager.SFX.BtnClick);
        GameManager.Instance.ToggleMusic();
    }
}
