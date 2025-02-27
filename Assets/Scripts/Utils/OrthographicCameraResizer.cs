using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Camera adaptation script that adapts
    /// the orthographic size based on the
    /// screen aspect ratio.
    ///
    /// Reacts to screen resize.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class OrthographicCameraResizer : MonoBehaviour
    {
        /// <summary>
        /// Mapping of supported portrait aspect ratios to camera orthographic size.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Rounded aspect ratio is <code>Mathf.Round((float)Screen.width / Screen.height * 1000f) / 1000f</code>.
        /// It is not precise; to 3 decimal places, but it is enough.
        /// </para>
        /// <para>
        /// The orthographic size value is set so that all spawner nodes fit on the screen.
        /// </para>
        /// </remarks>
        private static readonly Dictionary<float, float> RoundedAspectToOrthographicSize = new()
        {
            { 0.361f, 13.68f }, { 0.409f, 13.6f }, { 0.455f, 10.85f },
            { 0.462f, 10.7f }, { 0.474f, 10.39f }, { 0.486f, 10.15f },
            { 0.562f, 8.8f }, { 0.6f, 8.26f }, { 0.625f, 7.9f },
            { 0.698f, 7.1f }, { 0.75f, 6.6f }
        };

        /// <summary>
        /// Current (float)Screen.width / Screen.height.
        /// </summary>
        private float _currentAspect;

        /// <summary>
        /// Last Screen.width.
        /// </summary>
        private int _lastScreenWidth;

        /// <summary>
        /// Last Screen.height.
        /// </summary>
        private int _lastScreenHeight;

        /// <summary>
        /// Coroutine that monitors screen width or height changes.
        /// When current values don't equal the last ones
        /// it invokes OnScreenSizeChanged.
        /// </summary>
        private Coroutine _screenChangeCoroutine;

        private Camera _camera;

        /// <summary>
        /// Grabs required references.
        /// </summary>
        public void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            // initial camera config
            OnScreenSizeChanged();
            // begin to monitor res. changes
            _screenChangeCoroutine = StartCoroutine(CheckScreenSize());
        }

        public void OnDestroy()
        {
            if (_screenChangeCoroutine != null)
            {
                StopCoroutine(_screenChangeCoroutine);
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

                if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
                {
                    OnScreenSizeChanged();
                }
            }
        }

        /// <summary>
        /// Handles scree size changes.
        /// </summary>
        private void OnScreenSizeChanged()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            // convert width or height to float to get a float result
            _currentAspect = (float)Screen.width / Screen.height;

            if (Screen.width > Screen.height)
            {
                // landscape
                _camera.orthographicSize = 5f;
            }
            else
            {
                // portrait mode
                // round aspect to 3 decimal places, not very precise but good enough
                float roundedAspect = Mathf.Round(_currentAspect * 1000f) / 1000f;
                if (RoundedAspectToOrthographicSize.TryGetValue(roundedAspect, out float size))
                {
                    _camera.orthographicSize = size;
                }
                else
                {
                    Logger.Debug($"Not supported aspect {_currentAspect} rounded to {roundedAspect}");
                    // try to approximate based on y = a * x^b + c
                    _camera.orthographicSize = 4.639f * Mathf.Pow(roundedAspect, -1.081f) + 0.202f;
                }
            }
        }
    }
}