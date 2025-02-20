using TMPro;
using UnityEngine;

public class CountScore : MonoBehaviour
{
    public int startValue = 0;
    public int endValue = -1;
    public float duration = 2f; // Time in seconds
    private float elapsedTime = 0f;
    private int currentValue;

    [SerializeField]
    private AudioClip sfxScoring;

    private TextMeshProUGUI label;

    private bool startedCounting;

    void Start()
    {
        label = GetComponent<TextMeshProUGUI>();
        startedCounting = false;
    }

    void Update()
    {
        if (endValue == -1) {
            return;
        }

        if ((endValue > 0) && (elapsedTime < duration))
        {
            if (!startedCounting) {
                startedCounting = true;
                AudioManager.Instance.PlaySfx(sfxScoring);
            }

            elapsedTime += Time.unscaledDeltaTime;
            currentValue = (int)Mathf.Lerp(startValue, endValue, elapsedTime / duration);
        }
        else
        {
            currentValue = endValue; // Ensure it reaches the final value
        }

        label.text = currentValue.ToString();
    }
}
