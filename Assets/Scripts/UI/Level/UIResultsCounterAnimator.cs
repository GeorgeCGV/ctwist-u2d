using System;
using TMPro;
using UnityEngine;

namespace UI.Level
{
    /// <summary>
    /// Allows to animate present TextMeshProUGUI by
    /// counting int value <c>from</c> to <c>to</c> within <c>seconds</c>.
    /// </summary>
    /// <remarks>
    /// Prevents update by disabling itself until end value is provided.
    /// When end value is reached the component is disabled until next
    /// Animate call.
    /// </remarks>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UIResultsCounterAnimator : MonoBehaviour
    {
        private int _startValue;
        private int _endValue;
        private float _duration = 2.0f;
        private float _elapsedTime;
        private float _currentValue;

        [SerializeField]
        private AudioClip sfx;

        private TextMeshProUGUI _label;

        /// <summary>
        /// Executed when animation is done.
        /// </summary>
        private Action _onDoneCallback;

        /// <summary>
        /// Grabs required components.
        /// </summary>
        /// <remarks>
        /// Component is disabled by default, enabled via <c>Animate</c> call.
        /// Initial value is set to <c>0</c>.
        /// </remarks>
        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
            _label.text = "0";
            enabled = false;
        }

        private void Update()
        {
            // the game pauses by modifying the timeScale
            // therefore unscaled deltaTime is used
            _elapsedTime += Time.unscaledDeltaTime;

            if (_elapsedTime < _duration)
            {
                _currentValue = Mathf.Lerp(_startValue, _endValue,
                                          Mathf.Clamp01(_elapsedTime / _duration));
            }
            else
            {
                // ensure value reaches the end value
                _currentValue = _endValue;
                // done, disable the component
                enabled = false;
                // notify
                _onDoneCallback?.Invoke();
            }

            _label.text = ((int)_currentValue).ToString();
        }

        /// <summary>
        /// Animates label text with int value <c>from</c> to <c>to</c> within <c>seconds</c>.
        /// </summary>
        /// <param name="from">Initial value.</param>
        /// <param name="to">End value.</param>
        /// <param name="onDone">On done callback.</param>
        /// <param name="seconds">Duration if less or equal to 0 then SFX duration is used.</param>
        public void Animate(int from, int to, Action onDone = null, float seconds = 0)
        {
            _onDoneCallback = onDone;
            _elapsedTime = 0;
            _startValue = from;
            _endValue = to;
            if (sfx is not null)
            {
                if (seconds <= 0)
                {
                    _duration = sfx.length;
                }
                AudioManager.Instance.PlaySfx(sfx);
            }
            else
            {
                // prevents duration of <= 0.1
                _duration = Mathf.Max(seconds, 0.1f);
            }
            enabled = true;
        }

        /// <summary>
        /// Incremental animation from the last <c>endValue</c>.
        /// </summary>
        /// <param name="by">Animate from the last end value by provided value.</param>
        /// <param name="onDone">On done callback.</param>
        /// <param name="seconds">Duration, if less or equal to 0 then SFX duration is used.</param>
        public void Animate(int by, Action onDone = null, float seconds = 0)
        {
            Animate(_endValue, _endValue + by, onDone, seconds);
        }

#if UNITY_EDITOR && DEBUG // simple way to extend editor without adding a ton of extra code
        public int testFrom;
        public int testTo;
        public bool testAnimate;

        private void OnValidate()
        {
            if (!testAnimate)
            {
                return;
            }
            Animate(testFrom, testTo);
            // Animate(testFrom, testTo, delegate { Animate(100); });
            testAnimate = false;
        }
#endif // UNITY_EDITOR && DEBUG
    }
}