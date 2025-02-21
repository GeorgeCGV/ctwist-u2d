using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Canvas))]
public class UILevelController : MonoBehaviour
{
    public static event Action OnGameStarAllAnimationsDone;

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

    private void OnEnable()
    {
        LevelManager.OnTimeLeftUpdate += HandleTimeLeftUpdate;
        LevelManager.OnAnnounce += HandleAnnounce;
        LevelManager.OnGameOver += HandleGameOver;
        LevelManager.OnBeforeGameStarts += HandleBeforeGameStarts;
    }

    private void OnDisable()
    {
        LevelManager.OnTimeLeftUpdate -= HandleTimeLeftUpdate;
        LevelManager.OnAnnounce -= HandleAnnounce;
        LevelManager.OnGameOver -= HandleGameOver;
        LevelManager.OnBeforeGameStarts -= HandleBeforeGameStarts;
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

    private void HandleGameOver(Data.LevelData level, Data.GameOverResults result)
    {
        LevelManager.Instance.SetPaused(true);

        resultsMenu.SetActive(true);
        resultsMenu.GetComponent<Animator>().SetBool("Won", result.Won);

        int starsEarned = 0;
        for (int i = 0; i < 3; i++)
        {
            starsEarned += result.Score >= level.starRewards[i] ? 1 : 0;
        }
        resultsMenu.GetComponent<Animator>().SetInteger("Stars", starsEarned);

        // update current level index
        int persistedLevelIndex = PlayerPrefs.GetInt("currentLevel", 0);
        if (level.id == persistedLevelIndex)
        {
            // if we played currentLevel (the last unlocked)
            // then increment idx by 1 to unlock the next level
            PlayerPrefs.SetInt("currentLevel", persistedLevelIndex + 1);
        }

        // update level stars, but only if earned more than before
        int lastAmountOfStars = PlayerPrefs.GetInt("stars" + level.id, 0);
        if (starsEarned > lastAmountOfStars)
        {
            PlayerPrefs.SetInt("stars" + level.id, starsEarned);
        }

        UICounterAnimate counter = resultsMenu.GetComponentInChildren<UICounterAnimate>();
        resultsMenu.GetComponent<Animator>().SetTrigger("Open");
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.DialogAppear);
        counter.endValue = result.Score;
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
        // set limits

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
        for (int i = 1; i <= 3; i++)
        {
            countdownLabel.text = i.ToString();
            countdownLabel.enabled = true;
            AudioManager.Instance.PlaySfx(SfxOnCountdown);
            yield return new WaitForSeconds(.5f);
            countdownLabel.enabled = false;
            yield return new WaitForSeconds(.5f);
        }

        OnGameStarAllAnimationsDone?.Invoke();
    }
    #endregion

    public void OnPause()
    {
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.BtnClick);
        LevelManager.Instance.SetPaused(true);
        pauseMenu.SetActive(true);
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.DialogAppear);
    }

    public void OnUnpause()
    {
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.BtnClick);
        pauseMenu.SetActive(false);
        LevelManager.Instance.SetPaused(false);
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.DialogDissapear);
    }

    public void OnQuit()
    {
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.BtnClick);
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.DialogDissapear);
        SceneManager.LoadSceneAsync(0);
        LevelManager.Instance.SetPaused(false);
    }
}
