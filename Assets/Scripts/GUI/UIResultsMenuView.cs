using System;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Animator))]
public class UIResultsMenuView : MonoBehaviour
{
    private static readonly int animatorStarsProp = Animator.StringToHash("Stars");
    private static readonly int animatorWonProp = Animator.StringToHash("Won");
    private static readonly int animatorHighscoreProp = Animator.StringToHash("Highscore");
    private static readonly int animatorShowTriggerProp = Animator.StringToHash("Open");

    public enum OnNextAction {
        Continue, Quit
    }

    [SerializeField]
    private AudioClip sfxStarAppear;

    private Animator animator;
    private UICounterAnimate counterAnimator;

    private TextMeshProUGUI rightBtnLbl;

    private Action OnQuitCallback;
    private Action<int, OnNextAction> OnNextCallback;
    private OnNextAction onNextAction = OnNextAction.Continue;
    private LevelResults results;

    public void Init()
    {
        animator = GetComponent<Animator>();

        counterAnimator = GetComponentInChildren<UICounterAnimate>();
        Assert.IsNotNull(counterAnimator);

        rightBtnLbl = transform.Find("Panel")?.Find("RightBtn")?.Find("Label")?.GetComponent<TextMeshProUGUI>();
        Assert.IsNotNull(rightBtnLbl);
    }

    public void Show(LevelResults results, Action onQuit, Action<int, OnNextAction> onNext)
    {
        Assert.IsNotNull(onQuit);
        OnQuitCallback = onQuit;

        Assert.IsNotNull(onNext);
        OnNextCallback = onNext;

        this.results = results;

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
                onNextAction = OnNextAction.Quit;
            }
            else
            {
                rightBtnLblText = "Next";
            }
        }

        rightBtnLbl.text = rightBtnLblText;

        // Show the menu
        gameObject.SetActive(true);
        animator.SetBool(animatorWonProp, results.Won);
        animator.SetBool(animatorHighscoreProp, results.IsHighscore);
        animator.SetInteger(animatorStarsProp, results.EarnedStars);
        animator.SetTrigger(animatorShowTriggerProp);
        AudioManager.Instance.PlaySfx(AudioManager.SFX.DialogAppear);

    }

    /// <summary>
    /// Panel's right button OnClick handler.
    /// </summary>
    public void OnRightBtn()
    {
        OnNextCallback?.Invoke(results.NextLevel, onNextAction);
    }

    /// <summary>
    /// Panel's left button OnClick handler.
    /// </summary>
    public void OnLeftBtn()
    {
        OnQuitCallback?.Invoke();
    }

    /// <summary>
    /// Handles Animator's almost appear event.
    /// </summary>
    void OnAlmostAppear()
    {
        counterAnimator.Animate(0, results.Score, 2);
    }

    /// <summary>
    /// Handles Animator's start appear event.
    /// </summary>
    void OnStartAppear()
    {
        AudioManager.Instance.PlaySfx(sfxStarAppear);
    }
}
