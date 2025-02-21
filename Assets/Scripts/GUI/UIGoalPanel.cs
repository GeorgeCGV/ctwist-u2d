using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIGoalPanel : MonoBehaviour
{
    private enum EGoalType
    {
        None, ScoreGoal, BlocksGoal
    }

    private EGoalType type = EGoalType.None;

    [SerializeField]
    private TMP_Text scoreGoalLabel;

    private void OnDisable()
    {
        switch (type)
        {
            case EGoalType.ScoreGoal:
                LevelManager.OnScoreUpdate -= HandleScoreUpdate;
                break;
            default:
                // nothing
                break;
        }
    }

    private void HandleScoreUpdate(int score)
    {
        float goal = float.Parse(scoreGoalLabel.text);
        if (goal <= 0) {
            return;
        }
        Image fillBar = transform.Find("GoalFiller").GetComponent<Image>();
        fillBar.fillAmount = score / goal;
    }

    public void InitScoreGoal(int score)
    {
        if (type != EGoalType.None)
        {
            throw new ArgumentException("Goal panel is already setup as " + type);
        }

        type = EGoalType.ScoreGoal;
        LevelManager.OnScoreUpdate += HandleScoreUpdate;
        scoreGoalLabel.text = score.ToString();
        HandleScoreUpdate(0);
    }
}
