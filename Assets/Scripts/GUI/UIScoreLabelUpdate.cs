using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UIScoreLabelUpdate : MonoBehaviour
{
    private TextMeshProUGUI label;

    void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        label.text = "0";
    }

    private void OnEnable()
    {
        LevelManager.OnScoreUpdate += HandleScoreUpdate;
    }

    private void OnDisable()
    {
        LevelManager.OnScoreUpdate -= HandleScoreUpdate;
    }

    private void HandleScoreUpdate(int obj)
    {
        label.text = obj.ToString();
    }

}
