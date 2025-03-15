using System;
using System.Linq;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace UI.Level
{
    /// <summary>
    /// Level results panel controller.
    /// </summary>
    /// <remarks>
    /// Requires <c>UICounterAnimator</c> in one of the child objects.
    /// It is used to control achieved score and bonus score animation.
    /// Expects <c>TextMeshProUGUI</c> at path "Panel/RightBtn/Label",
    /// it is a label for the right button that will be modified depending
    /// on the level progression.
    /// </remarks>
    [RequireComponent(typeof(Animator))]
    public class UIResultsMenuCtrl : MonoBehaviour
    {
        private static readonly int AnimatorStarsProp = Animator.StringToHash("Stars");
        private static readonly int AnimatorWonProp = Animator.StringToHash("Won");
        private static readonly int AnimatorHighscoreProp = Animator.StringToHash("Highscore");
        private static readonly int AnimatorShowTriggerProp = Animator.StringToHash("Open");

        /// <summary>
        /// Tells callback waht action must be taken:
        /// Next level id can be continued to - for retry/next.
        /// Or Quit when there is nothing more to play - for the last level.
        /// </summary>
        public enum OnNextAction
        {
            Continue,
            Quit
        }

        /// <summary>
        /// Self-destructing label prefab to show bonus score points.
        /// </summary>
        /// <see cref="UISelfDestroyAnnotationFadeOut"/>
        [SerializeField]
        private GameObject bonusScorePrefab;

        [SerializeField]
        private TextMeshProUGUI rightBtnLbl;
        [SerializeField]
        private UIResultsCounterAnimator counterAnimator;
        [SerializeField]
        private AudioClip sfxStarAppear;

        /// <summary>
        /// Animator of this object.
        /// </summary>
        private Animator _animator;

        /// <summary>
        /// Callback for Quit button.
        /// </summary>
        private Action _onQuitCallback;

        /// <summary>
        /// Callback for Next/Retry/Done button.
        /// </summary>
        private Action<int, OnNextAction> _onNextCallback;

        /// <summary>
        /// What to transmit to the OnNextCallback.
        /// </summary>
        /// <remarks>
        /// By default, we expect to Continue, as there is only one last level.
        /// </remarks>
        private OnNextAction _onNextAction = OnNextAction.Continue;

        private LevelResults _results;

        /// <summary>
        /// Stores results score bonus keys to allow
        /// subsequent score animations for each bonus.
        /// </summary>
        private string[] _bonusKeys;

        /// <summary>
        /// Tracks <c>bonusKeys</c> index for playback.
        /// </summary>
        private int _lastBonusKeyIdx;

        /// <summary>
        /// Shows the results popup/menu.
        /// </summary>
        /// <param name="results">Game results.</param>
        /// <param name="onQuit">Quit button callback.</param>
        /// <param name="onNext">Next/Retry/Done button callback.</param>
        public void Show(LevelResults results, Action onQuit, Action<int, OnNextAction> onNext)
        {
            Assert.IsNotNull(onQuit, "onQuit callback can't be null");
            _onQuitCallback = onQuit;

            Assert.IsNotNull(onNext, "onNext callback can't be null");
            _onNextCallback = onNext;

            _results = results;
            _bonusKeys = results.BonusScore.Keys.ToArray();
            _lastBonusKeyIdx = 0;

            // setup right button
            // can be one of:
            // * "Retry" - lost
            // * "Next" - won, and next lvl exists (regardless if it is a replay or not)
            // * "Done" - we won, but there are no more levels in the game
            // unlock next level
            string rightBtnLblText = "Retry";
            if (results.Won)
            {
                if (results.WasLastLevel)
                {
                    rightBtnLblText = "Done";
                    _onNextAction = OnNextAction.Quit;
                }
                else
                {
                    rightBtnLblText = "Next";
                }
            }

            rightBtnLbl.text = rightBtnLblText;

            // Show the menu
            gameObject.SetActive(true);
            _animator.SetBool(AnimatorWonProp, results.Won);
            _animator.SetBool(AnimatorHighscoreProp, results.IsHighscore);
            _animator.SetInteger(AnimatorStarsProp, results.EarnedStars);
            _animator.SetTrigger(AnimatorShowTriggerProp);

            AudioManager.Instance.PlaySfx(AudioManager.Sfx.DialogAppear);
        }

        /// <summary>
        /// Checks that critical references are set.
        /// </summary>
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            counterAnimator = GetComponentInChildren<UIResultsCounterAnimator>();
            Assert.IsNotNull(counterAnimator, "missing counter animator");
            Assert.IsNotNull(rightBtnLbl, "missing right button label");

            Assert.IsNotNull(sfxStarAppear, "missing SFX for star appear");
        }

        /// <summary>
        /// Panel's right button OnClick handler.
        /// </summary>
        /// <remarks>
        /// Set in the Editor.
        /// </remarks>
        private void OnRightBtn()
        {
            _onNextCallback?.Invoke(_results.NextLevel, _onNextAction);
        }

        /// <summary>
        /// Panel's left button OnClick handler.
        /// </summary>
        /// <remarks>
        /// Set in the Editor.
        /// </remarks>
        private void OnLeftBtn()
        {
            _onQuitCallback?.Invoke();
        }

        /// <summary>
        /// Handles Animator's almost appear event.
        /// </summary>
        /// <remarks>
        /// Set in the panel Open animation.
        /// </remarks>
        private void OnAlmostAppear()
        {
            counterAnimator.Animate(0, _results.Score, _bonusKeys != null ? OnCounterAnimationDone : null);
        }

        /// <summary>
        /// Callback from counterAnimator when it is done.
        /// Used to restart counter animations for bonus points.
        /// </summary>
        /// <remarks>
        /// <c>UIResultsCounterAnimator</c>
        /// </remarks>
        private void OnCounterAnimationDone()
        {
            // exit in case the object is no longer enabled but the callback executed
            if (!enabled)
            {
                return;
            }

            // trigger bonus score points animation (if any left)
            if (_lastBonusKeyIdx <= (_bonusKeys.Length - 1))
            {
                string reason = _bonusKeys[_lastBonusKeyIdx++];
                int value = _results.BonusScore[reason];

                if (bonusScorePrefab != null)
                {
                    GameObject annotation = Instantiate(bonusScorePrefab, transform);
                    annotation.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 600);
                    annotation.GetComponent<UISelfDestroyAnnotationFadeOut>().Init($"{reason} {value}", 1.0f, true);
                }

                counterAnimator.Animate(value, _bonusKeys != null ? OnCounterAnimationDone : null);
            }
        }

        /// <summary>
        /// Callback for Animator's start appear event.
        /// </summary>
        /// <remarks>
        /// Set in the star animation.
        /// </remarks>
        private void OnStartAppear()
        {
            AudioManager.Instance.PlaySfx(sfxStarAppear);
        }
    }
}