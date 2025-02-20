using UnityEngine;

public class MenuController : MonoBehaviour
{
    public AudioClip BackgroundMusic;
    public GameObject optionsPanel;

    void Start() {
        AudioManager.Instance.PlayMusic(BackgroundMusic);
    }

    public void OnOptionsOpen() {
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.BtnClick);
        optionsPanel.GetComponent<Animator>().SetTrigger("Open");
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.DialogAppear);
    }

    public void OnOptionsClose() {
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.BtnClick);
        optionsPanel.GetComponent<Animator>().SetTrigger("Close");
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.DialogDissapear);
    }

    public void OnPlay() {
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.BtnClick);
        LoadScreen.Instance.LoadLevel(1);
    }

    public void OnQuit() {
        Application.Quit();
    }
}
