using TMPro;
using UnityEngine;

namespace UI.Level
{
    /// <summary>
    /// Object self destructs after fading out.
    /// </summary>
    /// <remarks>
    /// Used for in-game annotations and results view bonus scores.
    /// The label starts with alpha set to 1.0 (255) that fades
    /// to 0.0 (0) over set duration. The <c>GameObject</c> is destroyed
    /// when animation is done.
    /// </remarks>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UISelfDestroyAnnotationFadeOut : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float duration = 2.0f;

        private TextMeshProUGUI _label;

        private float _elapsedTime;
        private const float StartValue = 255;
        private const float EndValue = 0;

        /// <summary>
        /// /// The game pauses by modifying the timeScale,
        /// so we need this flag to use unscaled time
        /// when required.
        /// </summary>
        private bool _useUnscaledTime;

        /// <summary>
        /// Initializes the label and time variant.
        /// </summary>
        /// <param name="text">Label text.</param>
        /// <param name="seconds">Fade out duration.</param>
        /// <param name="useUnscaledTime">Use unscaled time or not.</param>
        public void Init(string text, float seconds = 0, bool useUnscaledTime = false)
        {
            _label.text = text;
            // must be > 0, otherwise default duration is used
            duration = seconds <= 0 ? duration : seconds;
            _elapsedTime = 0.0f;
            _useUnscaledTime = useUnscaledTime;
        }

        /// <summary>
        /// Grabs required components and sets alpha to start value.
        /// </summary>
        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
            _label.alpha = StartValue;
        }

        private void Update()
        {
            if (_elapsedTime < duration)
            {
                _elapsedTime += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                _label.alpha = Mathf.Lerp(StartValue, EndValue, _elapsedTime / duration);
            }
            else
            {
                _label.alpha = EndValue;
                Destroy(gameObject);
            }
        }
    }
}