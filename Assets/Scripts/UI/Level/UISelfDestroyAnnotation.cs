using TMPro;
using UnityEngine;

namespace UI.Level
{
    /// <summary>
    /// Object self-destructs after moving past the screen height.
    /// </summary>
    /// <remarks>
    /// Used for in-game annotations and results view bonus scores.
    /// </remarks>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UISelfDestroyAnnotation : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float duration = 2.0f;

        [SerializeField]
        private bool animate;
        
        private TextMeshProUGUI _label;

        private float _elapsedTime;
        private float _startY;
        private float _endY;
        private Vector2 _position;

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
            if (animate)
            {
                _position = transform.position;
                _startY = _position.y;
                _endY = _startY + Screen.height + _label.preferredHeight;
            }
        }

        /// <summary>
        /// Grabs required components and sets alpha to start value.
        /// </summary>
        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (_elapsedTime < duration)
            {
                _elapsedTime += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                
                if (animate)
                {
                    _position.y =  Mathf.Lerp(_startY, _endY, Mathf.Clamp01(_elapsedTime / duration));
                    transform.position = _position;
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}