using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UICounterAnimate : MonoBehaviour
{
    private int startValue = 0;
    private int endValue = -1;
    private float duration = 2f;
    private float elapsedTime = 0f;
    private int currentValue;

    [SerializeField]
    private AudioClip sfxScoring;

    private TextMeshProUGUI label;

    void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        label.text = "0";
    }

    public void Animate(int from, int to, float seconds)
    {
        // prevents duration of <= 0.1
        duration = Mathf.Max(seconds, 0.1f);
        startValue = from;
        endValue = to;
        AudioManager.Instance.PlaySfx(sfxScoring);
    }

    void Update()
    {
        if (endValue == -1)
        {
            return;
        }

        if ((endValue > 0) && (elapsedTime < duration))
        {
            elapsedTime += Time.unscaledDeltaTime;
            currentValue = (int)Mathf.Lerp(startValue, endValue, elapsedTime / duration);
        }
        else
        {
            // Ensure it reaches the final value
            currentValue = endValue;
        }

        label.text = currentValue.ToString();
    }
}
