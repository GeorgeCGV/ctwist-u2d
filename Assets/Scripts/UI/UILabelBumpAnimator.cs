using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Simple label ping-pong scale animation.
    /// </summary>
    /// <remarks>
    /// Component is disabled by default. Begins scale animation when enabled
    /// and disables itself when done.
    /// </remarks>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UILabelBumpAnimator : MonoBehaviour
    {
        [SerializeField]
        private float startScale;
        [SerializeField]
        private float endScale;

        /// <summary>
        /// Total animation duration.
        /// </summary>
        [SerializeField]
        private float duration;

        /// <summary>
        /// <see cref="Mathf.PingPong"/> <c>t</c> factor.
        /// </summary>
        [SerializeField]
        private float animationSpeed;

        /// <summary>
        /// Ref. to required label component to be animated.
        /// </summary>
        private TextMeshProUGUI _label;

        /// <summary>
        /// Current elapsed time.
        /// </summary>
        private float _elapsedTime;

        /// <summary>
        /// Current scale value.
        /// </summary>
        private Vector3 _currentValue;

        /// <summary>
        /// Grab required references and disable self.
        /// </summary>
        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
            enabled = false;
        }

        /// <summary>
        /// Reset elapsed time.
        /// </summary>
        private void OnEnable()
        {
            _elapsedTime = 0;
        }

        /// <summary>
        /// Animate.
        /// </summary>
        private void Update()
        {
            // the game pauses by modifying the timeScale;
            // therefore, unscaled deltaTime is used
            _elapsedTime += Time.deltaTime;

            if (_elapsedTime < duration)
            {
                // ping-pong and lerp
                float pingPongValue = Mathf.PingPong(_elapsedTime * animationSpeed, 1.0f);
                float scale = Mathf.Lerp(startScale, endScale, pingPongValue);
                // to avoid issues with TextMeshProUGUI uniform scaling is required
                _currentValue.Set(scale, scale, scale);
            }
            else
            {
                // ensure scale is set back to start value
                _currentValue.Set(startScale, startScale, startScale);
                // done, disable the component
                enabled = false;
            }

            _label.transform.localScale = _currentValue;
        }
    }
}