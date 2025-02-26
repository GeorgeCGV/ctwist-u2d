using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Camera adaptation script that adapts
/// the orthographic size based on the
/// screen aspect ration.
///
/// Reacts to screen resize.
/// </summary>
[ExecuteInEditMode]
public class ProjectionSetup : MonoBehaviour
{
    /// <summary>
    /// Mapping of supported portait aspect rations to camera orthographic size.
    /// Key - Mathf.Round((float)Screen.width / Screen.height * 1000f) / 1000f.
    ///       Not precise 3 decimal places rounding.
    /// Value - Camera orthographic size to use.
    ///
    /// The size value is set so all spawner nodes fit on the screen.
    /// </summary>
    private static readonly Dictionary<float, float> RoundedAspectToOrthographicSize = new()
    {
        { 0.361f, 13.68f }, { 0.409f, 13.6f }, { 0.455f, 10.85f },
        { 0.462f, 10.7f }, { 0.474f, 10.39f }, { 0.486f, 10.15f },
        { 0.562f, 8.8f },  { 0.6f, 8.26f },    { 0.625f, 7.9f },
        { 0.698f, 7.1f },  { 0.75f, 6.6f }
    };

    /// <summary>
    /// Current (float)Screen.width / Screen.height.
    /// </summary>
    private float currentAspect;

    /// <summary>
    /// Last Screen.width.
    /// </summary>
    private int lastScreenWidth;

    /// <summary>
    /// Last Screen.height.
    /// </summary>
    private int lastScreenHeight;

    /// <summary>
    /// Coroutine that monitors screen width or height changes.
    /// When current values don't equal the last ones
    /// it invokes OnScreenSizeChanged.
    /// </summary>
    private Coroutine screenChangeCoroutine;

    void Start()
    {
        // initial camera config
        OnScreenSizeChanged();
        // begin to monitor res. changes
        screenChangeCoroutine = StartCoroutine(CheckScreenSize());
    }

    void OnDestroy()
    {
        if (screenChangeCoroutine != null) {
            StopCoroutine(screenChangeCoroutine);
        }
    }

    /// <summary>
    /// Monitors screen size for changes every ~500ms.
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckScreenSize()
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

    private void OnScreenSizeChanged()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        // convert width or height to float to get a float result
        currentAspect = (float)Screen.width / Screen.height;

        if (Screen.width > Screen.height)
        {
            // landscape
            Camera.main.orthographicSize = 5f;
        }
        else
        {
            // portrait mode
            // round aspect to 3 decimal places, not very precise but good enough
            float roundedAspect = Mathf.Round(currentAspect * 1000f) / 1000f;
            if (RoundedAspectToOrthographicSize.TryGetValue(roundedAspect, out float size))
            {
                Camera.main.orthographicSize = size;
            }
            else
            {
                Logger.Debug($"Not supported aspect {currentAspect} rounded to {roundedAspect}");
                // try to approximate based on y = a * x^b + c
                Camera.main.orthographicSize = 4.639f * Mathf.Pow(roundedAspect, -1.081f) + 0.202f;
            }
        }
    }
}
