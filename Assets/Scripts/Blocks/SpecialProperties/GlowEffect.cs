using System.Collections;
using UnityEngine;

namespace Blocks.SpecialProperties
{
    /// <summary>
    /// Glow "effect" is simulated by uniformly scaling hex overlay sprite.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class GlowEffect : MonoBehaviour
    {
        [SerializeField]
        public float inPhaseDuration = 0.8f;

        [SerializeField]
        public float outPhaseDuration = 0.8f;
        
        /// <summary>
        /// Target animation value (i.e. scale) to ping pong to and from.
        /// </summary>
        /// <remarks>
        /// Goes from 1 to that value and back, then repeats.
        /// </remarks>
        [SerializeField]
        public float targetValue = 0.6f;

        private Coroutine _coroutine;
        private SpriteRenderer _spriteRenderer;
        private Vector3 _scale;

        /// <summary>
        /// Grab required references and set initial state.
        /// </summary>
        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _scale = _spriteRenderer.transform.localScale;
            _scale.Set(1,1,1);
            _spriteRenderer.transform.localScale = _scale;
        }

        /// <summary>
        /// Scale To value coroutine animation.
        /// </summary>
        /// <remarks>
        /// Lerps scale value to the specified target within specified duration. 
        /// </remarks>
        /// <param name="value">Target value.</param>
        /// <param name="durationInSeconds">Lerp duration.</param>
        /// <returns></returns>
        private IEnumerator ScaleTo(float value, float durationInSeconds)
        {
            float elapsedTime = 0.0f;
            
            // lerp until reached the duration
            while (elapsedTime < durationInSeconds)
            {
                elapsedTime += Time.deltaTime;
                float newValue = Mathf.Lerp(_scale.x, value, elapsedTime / durationInSeconds);
                _scale.Set(newValue, newValue, newValue);
                _spriteRenderer.transform.localScale = _scale;
                yield return null;
            }

            // force to target upon reached duration
            _scale.Set(value, value, value);
            _spriteRenderer.transform.localScale = _scale;
        }

        /// <summary>
        /// Animates the glow sprite by fading the sprite in and out. 
        /// </summary>
        /// <remarks>
        /// Takes a second for each fade part.
        /// </remarks>
        /// <returns></returns>
        private IEnumerator AnimateGlow()
        {
            // continue to fade in & out forever
            while (true)
            {
                // fade out
                yield return ScaleTo(targetValue, outPhaseDuration);
                // fade in
                yield return ScaleTo(1.0f, inPhaseDuration);
            }
        }

        private void Start()
        {
            _coroutine = StartCoroutine(AnimateGlow());
        }

        private void OnDestroy()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }
        }
    }
}