using System;
using System.Collections;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Canvas))]
public class UILevelController : MonoBehaviour
{
    public static event Action OnGameStartAllAnimationsDone;

    [SerializeField]
    private GameObject annotationPrefab;

    [SerializeField]
    private GameObject pausePanel;

    [SerializeField]
    private GameObject topLeftPanel;

    [SerializeField]
    private GameObject topRightPanel;

    [SerializeField]
    private GameObject pauseMenu;

    [SerializeField]
    private GameObject resultsMenu;

    [SerializeField]
    private AudioClip SfxOnCountdown;

    [SerializeField]
    private TMP_Text countdownLabel;

    [SerializeField]
    private GameObject goalPanel;

    [SerializeField]
    private TMP_Text timeLabel;

    private UIResultsMenuView resultsView;

    void Awake()
    {
        // init gameover/results menu
        resultsView = GetComponentInChildren<UIResultsMenuView>(true);
        Assert.IsNotNull(resultsView);
        resultsView.Init();
    }

    void OnEnable()
    {
        LevelManager.OnTimeLeftUpdate += HandleTimeLeftUpdate;
        LevelManager.OnAnnounce += HandleAnnounce;
        LevelManager.OnBeforeGameStarts += HandleBeforeGameStarts;
        LevelManager.OnGameOver += HandleGameOver;

    }

    private void HandleGameOver(LevelData data, LevelResults results)
    {
        // pause the time
        LevelManager.Instance.SetPaused(true);
        // show results view
        resultsView.Show(results, OnQuit, OnNext);
    }

    void OnDisable()
    {
        LevelManager.OnTimeLeftUpdate -= HandleTimeLeftUpdate;
        LevelManager.OnAnnounce -= HandleAnnounce;
        LevelManager.OnBeforeGameStarts -= HandleBeforeGameStarts;
        LevelManager.OnGameOver -= HandleGameOver;
    }

    private void HandleAnnounce(string text, Vector2 pos)
    {
        // Convert world position to screen position
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(pos);

        // Convert screen position to UI local position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GetComponent<Canvas>().transform as RectTransform, screenPosition, GetComponent<Canvas>().worldCamera, out Vector2 uiPosition);

        GameObject annoation = Instantiate(annotationPrefab);
        annoation.GetComponent<UISelfDestroyAnnotationFadeOut>().SetText(text);
        // Set the parent to the Canvas
        annoation.transform.SetParent(GetComponent<Canvas>().transform, false);
        annoation.transform.SetSiblingIndex(4);
        // Get RectTransform & Set Position
        annoation.GetComponent<RectTransform>().anchoredPosition = uiPosition;
    }

    private void HandleTimeLeftUpdate(int min, int sec)
    {
        timeLabel.text = $"{min:D2}:{sec:D2}";
    }

    #region LevelStartup
    private void HandleBeforeGameStarts(Data.LevelData data)
    {
        // set goals
        if (data.goalScore != 0)
        {
            goalPanel.GetComponent<UIGoalPanel>().InitScoreGoal(data.goalScore);
        }

        TextMeshProUGUI lvlLabel = transform.Find("LevelLabel")?.GetComponent<TextMeshProUGUI>();
        Assert.IsNotNull(lvlLabel);

        lvlLabel.text = "Level " + (data.id + 1).ToString();

        pausePanel.GetComponent<Animator>().SetTrigger("Appear");
        topLeftPanel.GetComponent<Animator>().SetTrigger("Appear");
        topRightPanel.GetComponent<Animator>().SetTrigger("Appear");
        // to avoid creation of another set of delegates...
        // simply wait until animation is done (depends on "Appear" clip duration)
        StartCoroutine(StartLevel(GetComponent<Animator>()));
    }

    private IEnumerator StartLevel(Animator animator)
    {
        animator.SetTrigger("ShadeOff");
        for (int i = 3; i >= 1; i--)
        {
            countdownLabel.text = i.ToString();
            countdownLabel.enabled = true;
            AudioManager.Instance.PlaySfxPausable(SfxOnCountdown);
            yield return new WaitForSeconds(.5f);
            countdownLabel.enabled = false;
            yield return new WaitForSeconds(.5f);
        }

        OnGameStartAllAnimationsDone?.Invoke();
    }
    #endregion

    public void OnPause()
    {
        AudioManager.Instance.PausableSfxPause(true);
        AudioManager.Instance.PlaySfx(AudioManager.SFX.BtnClick);
        LevelManager.Instance.SetPaused(true);
        pauseMenu.SetActive(true);
        AudioManager.Instance.PlaySfx(AudioManager.SFX.DialogAppear);
    }

    public void OnUnpause()
    {
        AudioManager.Instance.PlaySfx(AudioManager.SFX.BtnClick);
        pauseMenu.SetActive(false);
        LevelManager.Instance.SetPaused(false);
        AudioManager.Instance.PlaySfx(AudioManager.SFX.DialogDissapear);
        AudioManager.Instance.PausableSfxPause(false);
    }

    public void OnQuit()
    {
        AudioManager.Instance.PlaySfx(AudioManager.SFX.BtnClick);
        AudioManager.Instance.PlaySfx(AudioManager.SFX.DialogDissapear);
        AudioManager.Instance.StopSfxPausable();
        SceneManager.LoadSceneAsync(0);
        LevelManager.Instance.SetPaused(false);
    }

    public void OnNext(int nextLevelId, UIResultsMenuView.OnNextAction onNextAction)
    {
        if (onNextAction == UIResultsMenuView.OnNextAction.Quit) {
            OnQuit();
        } else {
            AudioManager.Instance.PlaySfx(AudioManager.SFX.BtnClick);
            AudioManager.Instance.PlaySfx(AudioManager.SFX.DialogDissapear);
            AudioManager.Instance.StopSfxPausable();
            LoadScreen.Instance.LoadLevel(nextLevelId);
            LevelManager.Instance.SetPaused(false);
        }
    }

}
