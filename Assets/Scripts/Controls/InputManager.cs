using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace Controls
{
    public sealed class InputManager : MonoBehaviour
    {
        /// <summary>
        /// Swipe rotation sensitivity.
        /// </summary>
        [SerializeField, Min(0.1f)]
        private float sensitivity = 0.6f;

        /// <summary>
        /// Drag rotation sensitivity.
        /// </summary>
        [SerializeField, Min(1f)]
        private float dragSensitivity = 7f;

        /// <summary>
        /// Object to apply rotation to.
        /// </summary>
        private GameObject _activeBlocks;

        /// <summary>
        /// Tap/Click start position and when last drag position in world coordinates.
        /// </summary>
        private Vector2 _pointerPressStartPos;

        /// <summary>
        /// Tap/Click a end position in world coordinates.
        /// </summary>
        private Vector2 _pointerPressEndPos;

        /// <summary>
        /// Input ID.
        /// </summary>
        private int _activeInputId;

        /// <summary>
        /// Is dragging the pointer flag.
        /// Active when holding a finger or a mouse button.
        /// </summary>
        private bool _dragging;

        /// <summary>
        /// Drag start time.
        /// </summary>
        private float _dragStartTime;

        /// <summary>
        /// Current rotational speed.
        /// </summary>
        private float _angularVelocity;

        /// <summary>
        /// How fast angular velocity slows down / decays.
        /// </summary>
        [SerializeField, Min(0.1f)]
        private float decayRate = 4.0f;

        /// <summary>
        /// Minimum velocity to stop.
        /// </summary>
        [SerializeField, Min(0.01f)]
        private float minAngularVelocity = 0.01f;

        /// <summary>
        /// Speed limit one factor used to compute new angular velocity.
        /// /// Computed from swipe's distance divided by its duration.
        /// </summary>
        [SerializeField, Min(0)]
        private float swipeSpeedLimit = 30.0f;

        /// <summary>
        /// Active camera.
        /// </summary>
        private Camera _camera;

        /// <summary>
        /// Checks that position doesn't contain nan or infinite values.
        /// That might happen on clicks picked up outside the view.
        /// </summary>
        /// <param name="pos">Vector2</param>
        /// <returns>True if position values are set.</returns>
        private static bool IsScreenPositionValid(Vector2 pos)
        {
            return !(float.IsNaN(pos.x) || float.IsInfinity(pos.x) || float.IsNaN(pos.y) || float.IsInfinity(pos.y));
        }

        #region Unity
        
        /// <summary>
        /// Grabs required references.
        /// </summary>
        private void Awake()
        {
            _camera = Camera.main;
            _activeBlocks = GameObject.FindGameObjectWithTag("active_blocks");
            Assert.IsNotNull(_activeBlocks, "missing active blocks");
            _angularVelocity = 0.0f;
        }

        /// <summary>
        /// Controls activeBlocks rotation and velocity decay.
        /// </summary>
        private void Update()
        {
            float absVelocity = Mathf.Abs(_angularVelocity);

            // prevent necessary updates if reached min threshold
            if (absVelocity <= minAngularVelocity)
            {
                return;
            }

            // angular velocity AV-new = AV-current + impulse * e^−decay×Δt
            _angularVelocity *= Mathf.Exp(-decayRate * Time.deltaTime);

            // stop AV reached min threshold
            if (absVelocity <= minAngularVelocity)
            {
                _angularVelocity = 0;
            }

            // angle θ-new = θ-current + AV-new * Δt
            _activeBlocks.transform.rotation *= Quaternion.Euler(0, 0, _angularVelocity * Time.deltaTime);
        }

        #endregion Unity
        
        /// <summary>
        /// Process possible swipe gesture.
        /// </summary>
        /// <param name="duration">Duration between gesture end and start tinme.</param>
        private void HandlePossibleSwipe(float duration)
        {
            float distance = Vector2.Distance(_pointerPressEndPos, _pointerPressStartPos);

            // prevent possible bad input (sometimes InputSystem feeds values outside of the view)
            if (float.IsInfinity(distance) || float.IsNaN(distance))
            {
                return;
            }

#if DEBUG_LOG_INPUT
            Logger.Debug($"Swipe distance {distance} duration {duration} dir {dir}");
#endif // DEBUG_LOG_INPUT

            // swipe is dead-banded by distance and duration
            if (duration > 0)
            {
                // compute angle difference and speed
                Vector2 pivot = _activeBlocks.transform.position;
                float angleDelta = Vector2.SignedAngle(_pointerPressStartPos - pivot, _pointerPressEndPos - pivot);
                float speed = distance / duration;

                // apply speed limit
                if (swipeSpeedLimit > 0)
                {
                    speed = Mathf.Max(speed, swipeSpeedLimit);
                }

                // determine new velocity
                _angularVelocity += angleDelta * speed * sensitivity;
#if DEBUG_LOG_INPUT
                Logger.Debug($"Angle delta {angleDelta}, speed {speed}, angularVelocity {angularVelocity}");
#endif // DEBUG_LOG_INPUT
            }
        }

        /// <summary>
        /// Expects PointerInput composite from the InputSystem on mouse/pointer or touch input.
        /// </summary>
        /// <remarks>
        /// Set in the Editor.
        /// </remarks>
        /// <param name="ctx">Context with PointerInput composite value.</param>
        private void OnPointerInput(InputAction.CallbackContext ctx)
        {
            PointerInput input = ctx.ReadValue<PointerInput>();

            // discard invalid positions
            if (!IsScreenPositionValid(input.Position))
            {
                return;
            }

            // if (!LevelManager.Instance.IsRunning())
            // {
            //     return;
            // }

            if (input.Contact && !_dragging)
            {
                _dragStartTime = Time.time;
                _activeInputId = input.InputId;
                _pointerPressStartPos = _camera.ScreenToWorldPoint(input.Position);
                _dragging = true;

#if DEBUG_LOG_INPUT
                Logger.Debug($"{ctx.control.device.name}: started dragging at {pointerPressStartPos}");
#endif // DEBUG_LOG_INPUT
            }
            else if (input.Contact && _dragging)
            {
                // previous input type changed or it was lost, discard
                if (_activeInputId != input.InputId)
                {
                    return;
                }

                Vector2 pointerCurrentPos = _camera.ScreenToWorldPoint(input.Position);
                Vector2 pivot = _activeBlocks.transform.position;
                float angleDelta = Vector2.SignedAngle(_pointerPressStartPos - pivot, pointerCurrentPos - pivot);
                _angularVelocity += angleDelta * dragSensitivity;

                // update start position, as swipe shall happen from that point
                _pointerPressStartPos = pointerCurrentPos;
            }
            else if (!input.Contact && _dragging)
            {
                // previous input type changed or it was lost, discard
                if (_activeInputId != input.InputId)
                {
                    return;
                }

                _pointerPressEndPos = _camera.ScreenToWorldPoint(input.Position);

#if DEBUG_LOG_INPUT
                Logger.Debug($"{ctx.control.device.name}: done dragging at {pointerPressEndPos}");
                Debug.DrawLine(_pointerPressStartPos, _pointerPressEndPos, Color.red, 3);
#endif // DEBUG_LOG_INPUT

                HandlePossibleSwipe(Time.time - _dragStartTime);
                _dragging = false;
            }
        }
    }
}