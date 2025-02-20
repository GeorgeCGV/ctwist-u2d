using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class SelfDestroyAnnotationFadeOut : MonoBehaviour
{
    [SerializeField]
    private float duration;

    private float elapsedTime = 0f;

    private TextMeshProUGUI label;

    private float startValue = 255;
    private float endValue = 0;

    void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        label.alpha = startValue;
        Assert.IsNotNull(label);
    }

    public void SetText(string text)
    {
        label.text = text;
    }

    void Update()
    {
        if (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            label.alpha = Mathf.Lerp(startValue, endValue, elapsedTime / duration);
        } else {
            label.alpha = endValue;
            Destroy(gameObject);
        }
    }
}
