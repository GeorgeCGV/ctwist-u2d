using System.Collections;
using UnityEngine;

public class CameraInit : MonoBehaviour
{
    public float currentAspect = 0;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private Coroutine screenChangeCoroutine;

    void Start()
    {
        OnScreenSizeChanged();

        screenChangeCoroutine = StartCoroutine(CheckScreenSize());
    }

    void OnDestroy()
    {
        StopCoroutine(screenChangeCoroutine);
    }

    IEnumerator CheckScreenSize()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                OnScreenSizeChanged();
            }
        }
    }

    void OnScreenSizeChanged()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        currentAspect = (float)Screen.width / Screen.height;

        // round to 3 decimal places, not very precise
        float roundedAspect = Mathf.Round(currentAspect * 1000f) / 1000f;

        if (Screen.width > Screen.height) {
            // landscape
            Camera.main.orthographicSize = 5f;
        } else {
            // portrait mode
            if (Mathf.Approximately(roundedAspect, 0.361f)) {
                Camera.main.orthographicSize = 13.68f;
            } else if (Mathf.Approximately(roundedAspect, 0.409f)) {
                Camera.main.orthographicSize = 13.6f;
            } else if (Mathf.Approximately(roundedAspect, 0.455f)) {
                Camera.main.orthographicSize = 10.85f;
            } else if (Mathf.Approximately(roundedAspect, 0.462f)) {
                Camera.main.orthographicSize = 10.7f;
            } else if (Mathf.Approximately(roundedAspect, 0.474f)) {
                Camera.main.orthographicSize = 10.39f;
            } else if (Mathf.Approximately(roundedAspect, 0.486f)) {
                Camera.main.orthographicSize = 10.15f;
            } else if (Mathf.Approximately(roundedAspect, 0.562f)) {
                Camera.main.orthographicSize = 8.8f;
            } else if (Mathf.Approximately(roundedAspect, 0.6f)) {
                Camera.main.orthographicSize = 8.26f;
            } else if (Mathf.Approximately(roundedAspect, 0.625f)) {
                Camera.main.orthographicSize = 7.9f;
            } else if (Mathf.Approximately(roundedAspect, 0.698f)) {
                Camera.main.orthographicSize = 7.1f;
            } else if (Mathf.Approximately(roundedAspect, 0.75f)) {
                Camera.main.orthographicSize = 6.6f;
            }


        }

    }
}
