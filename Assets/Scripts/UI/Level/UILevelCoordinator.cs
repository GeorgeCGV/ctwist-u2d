using System;
using System.Collections;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace UI.Level
{
    /// <summary>
    /// Level Scene UI coordinator.
    /// </summary>
    /// <remarks>
    /// Controls the UI in the level scene.
    /// Coordinates all other controllers,
    /// and level startup / end.
    /// </remarks>
    [RequireComponent(typeof(Canvas))]
    public class UILevelCoordinator : MonoBehaviour
    {
        private static readonly int AnimatorShadeOffTrigger = Animator.StringToHash("ShadeOff");
        private static readonly int PanelAnimatorShowTrigger = Animator.StringToHash("Appear");

        /// <summary>
        /// Invoked when startup animation is complete and the
        /// level can start.
        /// </summary>
        public static event Action OnGameStartAllAnimationsDone;

        /// <summary>
        /// Prefab to use for the UI annotations spawned
        ///  during the game (i.e. +X score).
        /// </summary>
        [SerializeField]
        private GameObject annotationPrefab;

        /// <summary>
        /// Bottom right pause panel.
        /// </summary>
        [SerializeField]
        private Animator pausePanelAnimator;

        /// <summary>
        /// Top left panel with scores and multiplier.
        /// </summary>
        [SerializeField] 
        private Animator topLeftPanelAnimator;

        /// <summary>
        /// Top right panel with goals and limits.
        /// </summary>
        [SerializeField] 
        private Animator topRightPanelAnimator;

        /// <summary>
        /// Pause menu/popup.
        /// </summary>
        [SerializeField]
        private GameObject pauseMenu;

        /// <summary>
        /// Countdown label used for 3, 2, 1 countdown
        /// when level starts.
        /// </summary>
        [SerializeField]
        private TMP_Text countdownLabel;

        /// <summary>
        /// SFX to be used on each countdown label change.
        /// </summary>
        [SerializeField]
        private AudioClip sfxOnCountdown;

        /// <summary>
        /// Level goal controller.
        /// </summary>
        [SerializeField]
        private UIGoalCtrl goalCtrl;

        /// <summary>
        /// Level limit controller.
        /// </summary>
        [SerializeField]
        private UILimitCtrl limitCtrl;

        /// <summary>
        /// GameOver / Results controller.
        /// </summary>
        [SerializeField]
        private UIResultsMenuCtrl resultsCtrl;

        /// <summary>
        /// Level label.
        /// </summary>
        /// <remarks>
        /// Shows what level is being played.
        /// </remarks>
        [SerializeField]
        private TextMeshProUGUI lvlLabel;

        /// <summary>
        /// Checks that critical references are set.
        /// </summary>
        private void Awake()
        {
            Assert.IsNotNull(lvlLabel, "missing lvlLabel");
            Assert.IsNotNull(pauseMenu, "missing pauseMenu");

            Assert.IsNotNull(annotationPrefab, "missing annotationPrefab controller");
            Assert.IsNotNull(countdownLabel, "missing countdownLabel controller");

            Assert.IsNotNull(resultsCtrl, "missing results controller");
            Assert.IsNotNull(goalCtrl, "missing goal controller");
            Assert.IsNotNull(limitCtrl, "missing limit controller");
        }

        private void OnEnable()
        {
            LevelManager.OnAnnounce += HandleAnnounce;
            LevelManager.OnBeforeGameStarts += HandleBeforeGameStarts;
            LevelManager.OnGameOver += HandleGameOver;
        }

        private void OnDestroy()
        {
            LevelManager.OnAnnounce -= HandleAnnounce;
            LevelManager.OnBeforeGameStarts -= HandleBeforeGameStarts;
            LevelManager.OnGameOver -= HandleGameOver;
        }

        #region Misc

        /// <summary>
        /// Instantiates self-destructing label to display
        /// game announcements (like +X score).
        /// </summary>
        /// <remarks>
        /// <see cref="UISelfDestroyAnnotationFadeOut"/>
        /// </remarks>
        /// <param name="text">Text.</param>
        /// <param name="pos">World position.</param>
        private void HandleAnnounce(string text, Vector2 pos)
        {
            if (Camera.main == null)
            {
                // we won't be able to convert to screen point in such case
                return;
            }

            // convert world position to screen position
            Vector2 screenPosition = Camera.main.WorldToScreenPoint(pos);
            // convert screen position to UI local position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                GetComponent<Canvas>().transform as RectTransform,
                screenPosition, GetComponent<Canvas>().worldCamera, out Vector2 uiPosition);

            // instantiate with Canvas as parent
            GameObject annotation = Instantiate(annotationPrefab, GetComponent<Canvas>().transform);
            annotation.GetComponent<UISelfDestroyAnnotationFadeOut>().Init(text);
            // render behind pause panel
            annotation.transform.SetSiblingIndex(4);
            annotation.GetComponent<RectTransform>().anchoredPosition = uiPosition;
        }

        #endregion

        #region Level GameOver

        /// <summary>
        /// Callback for GameOver.
        /// </summary>
        /// <remarks>
        /// Pauses the level and shows the results.
        /// </remarks>
        /// <param name="results">Level results.</param>
        private void HandleGameOver(LevelResults results)
        {
            // pause the time
            LevelManager.SetPaused(true);
            // show results view
            resultsCtrl.Show(results, OnQuit, OnNext);
        }

        #endregion

        #region Level Startup

        /// <summary>
        /// Callback to prepare the UI and start the startup animation.
        /// </summary>
        /// <param name="data">Level data</param>
        private void HandleBeforeGameStarts(LevelData data)
        {
            // set level id
            lvlLabel.text = $"Level {data.ID + 1}";
            // init goal(s)
            goalCtrl.Init(data.goal);
            // init limit(s)
            limitCtrl.Init(data.limit);

            // start startup animations
            pausePanelAnimator?.SetTrigger(PanelAnimatorShowTrigger);
            topLeftPanelAnimator?.SetTrigger(PanelAnimatorShowTrigger);
            topRightPanelAnimator?.SetTrigger(PanelAnimatorShowTrigger);
            // to avoid creation of another set of controller and callbacks
            // simply wait until animation is done (depends on "Appear" clip duration)
            StartCoroutine(StartLevel());
        }

        /// <summary>
        /// Coroutine for 3, 2, 1 countdown before play happens.
        /// </summary>
        /// <remarks>
        /// Pragmatic approach instead of using another Animator.
        /// </remarks>
        /// <returns>Delays.</returns>
        private IEnumerator StartLevel()
        {
            GetComponent<Animator>().SetTrigger(AnimatorShadeOffTrigger);
            for (int i = 3; i >= 1; i--)
            {
                countdownLabel.text = i.ToString();
                countdownLabel.enabled = true;

                if (sfxOnCountdown)
                {
                    AudioManager.Instance.PlaySfxPausable(sfxOnCountdown);
                }

                yield return new WaitForSeconds(.5f);
                countdownLabel.enabled = false;
                yield return new WaitForSeconds(.5f);
            }

            // notify parties that the game starts
            OnGameStartAllAnimationsDone?.Invoke();
        }

        #endregion

        /// <summary>
        /// Callback for the pause button.
        /// </summary>
        /// <remarks>
        /// Set in the Editor.
        /// </remarks>
        private void OnPause()
        {
            AudioManager.Instance.PausableSfxPause(true);
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.BtnClick);
            LevelManager.SetPaused(true);
            pauseMenu.SetActive(true);
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.DialogAppear);
        }

        /// <summary>
        /// Callback for the pause menu unpause button.
        /// </summary>
        /// <remarks>
        /// Set in the Editor.
        /// </remarks>
        private void OnUnpause()
        {
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.BtnClick);
            pauseMenu.SetActive(false);
            LevelManager.SetPaused(false);
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.DialogDisappear);
            AudioManager.Instance.PausableSfxPause(false);
        }

        /// <summary>
        /// Callback for the results and pause buttons to quit to the main menu.
        /// </summary>
        /// <remarks>
        /// Set in the Editor.
        /// </remarks>
        private void OnQuit()
        {
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.BtnClick);
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.DialogDisappear);
            AudioManager.Instance.StopSfxPausable();
            SceneManager.LoadSceneAsync(0);
            LevelManager.SetPaused(false);
        }

        /// <summary>
        /// Callback for the results right button.
        /// </summary>
        /// <param name="nextLevelId">Next level id, but can be the same as the one played.</param>
        /// <param name="onNextAction">What should be done when non quit button is pressed.</param>
        private void OnNext(int nextLevelId, UIResultsMenuCtrl.OnNextAction onNextAction)
        {
            if (onNextAction == UIResultsMenuCtrl.OnNextAction.Quit)
            {
                // if it was the last level, then we can't continue
                OnQuit();
            }
            else
            {
                // continue to the next level or retry
                AudioManager.Instance.PlaySfx(AudioManager.Sfx.BtnClick);
                AudioManager.Instance.PlaySfx(AudioManager.Sfx.DialogDisappear);
                AudioManager.Instance.StopSfxPausable();
                LevelLoader.Instance.LoadLevel(nextLevelId);
                LevelManager.SetPaused(false);
            }
        }
    }
}