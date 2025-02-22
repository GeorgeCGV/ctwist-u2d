using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class UIScoreMultiplierCtrl : MonoBehaviour
{

    private Image fillBar;
    private TextMeshProUGUI label;

    void Awake()
    {
        fillBar = transform.Find("MultiplierFiller").GetComponent<Image>();
        Assert.IsNotNull(fillBar);

        label = transform.Find("MultiplierLbl").GetComponent<TextMeshProUGUI>();
        Assert.IsNotNull(label);
    }

    private void OnEnable()
    {
        MultiplierHandler.OnMultiplierTimerUpdate += HandleMultiplierTimerUpdate;
        MultiplierHandler.OnMultiplierUpdate += HandleMultiplierUpdate;

    }

    private void OnDisable()
    {
        MultiplierHandler.OnMultiplierTimerUpdate -= HandleMultiplierTimerUpdate;
        MultiplierHandler.OnMultiplierUpdate += HandleMultiplierUpdate;
    }

    private void HandleMultiplierUpdate(int val)
    {
        label.text = "x" + Mathf.FloorToInt(val).ToString();
    }

    private void HandleMultiplierTimerUpdate(float current, float max)
    {
        fillBar.fillAmount = current / max;
    }
}
