using UnityEngine;

public class MenuController : MonoBehaviour
{
    private static int animatorTriggerOpen = Animator.StringToHash("Open");
    private static int animatorTriggerClose = Animator.StringToHash("Close");

    [SerializeField]
    private AudioClip BackgroundMusic;
    [SerializeField]
    private GameObject optionsPanel;

    void Start() {
        AudioManager.Instance.PlayMusic(BackgroundMusic);
    }

    public void OnOptionsOpen() {
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.BtnClick);
        optionsPanel.GetComponent<Animator>().SetTrigger(animatorTriggerOpen);
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.DialogAppear);
    }

    public void OnOptionsClose() {
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.BtnClick);
        optionsPanel.GetComponent<Animator>().SetTrigger(animatorTriggerClose);
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
