using UnityEngine;

public class MenuController : MonoBehaviour
{
    private static readonly int animatorTriggerOpen = Animator.StringToHash("Open");
    private static readonly int animatorTriggerClose = Animator.StringToHash("Close");

    [SerializeField]
    private AudioClip backgroundMusic;

    [SerializeField]
    private GameObject optionsPanel;

    [SerializeField]
    private GameObject levelSelectionPanel;

    void Start() {
        AudioManager.Instance.PlayMusic(backgroundMusic);

        levelSelectionPanel.GetComponent<LevelSelectionCtrl>().CreateSelectableLevels();
    }

    private static void PanelAction(GameObject panel, bool open)
    {
        panel.SetActive(open);
        Animator panelAnimator = panel.GetComponent<Animator>();
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.BtnClick);
        panelAnimator.SetTrigger(open ? animatorTriggerOpen : animatorTriggerClose);
        AudioManager.Instance.PlaySfx(open ? (int)AudioManager.SFX.DialogAppear : (int)AudioManager.SFX.DialogDissapear);
    }

    public void OnLevelSelectionOpen() {
        PanelAction(levelSelectionPanel, true);
    }

    public void OnLevelSelectionClose() {
        PanelAction(levelSelectionPanel, false);
    }

    public void OnOptionsOpen() {
        PanelAction(optionsPanel, true);
    }

    public void OnOptionsClose() {
        PanelAction(optionsPanel, false);
    }
}
