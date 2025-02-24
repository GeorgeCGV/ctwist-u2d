using InputSamples.Drawing;
using UnityEngine;
using UnityEngine.InputSystem;

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
    /// Minimum distance threshold between end and start positions
    /// to be considered as valid swipe motion.
    /// </summary>
    [SerializeField]
    private float minSwipeDistance = 0f;

    /// <summary>
    /// Object to apply rotation to.
    /// </summary>
    private GameObject activeBlocks;

    /// <summary>
    /// Tap/Click start position and when last drag position in world coordinates.
    /// </summary>
    private Vector2 pointerPressStartPos;

    /// <summary>
    /// Tap/Click a end position in world coordinates.
    /// </summary>
    private Vector2 pointerPressEndPos;

    /// <summary>
    /// Input ID.
    /// </summary>
    private int activeInputId;
    /// <summary>
    /// Is dragging the pointer flag.
    /// Active when holding a finger or a mouse button.
    /// </summary>
    private bool dragging;
    /// <summary>
    /// Drag start time.
    /// </summary>
    private float dragStartTime;

    /// <summary>
    /// Current rotational speed.
    /// </summary>
    private float angularVelocity = 0.0f;
    /// <summary>
    /// How fast angular velicoty slows down / decays.
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

    private void Awake()
    {
        activeBlocks = GameObject.FindGameObjectWithTag("active_blocks");
    }

    /// <summary>
    /// Checks that position doesn't contain nan or infinite values.
    /// That might happen on clicks picked up outside of the view.
    /// </summary>
    /// <param name="pos">Vector2</param>
    /// <returns>True if position values are set.</returns>
    public static bool IsScreenPositionValid(Vector2 pos)
    {
        return !(float.IsNaN(pos.x) || float.IsInfinity(pos.x) || float.IsNaN(pos.y) || float.IsInfinity(pos.y));
    }

    /// <summary>
    /// Controls activeBlocks rotation and velocity decay.
    /// </summary>
    void Update()
    {
        float absVelocity = Mathf.Abs(angularVelocity);

        // prevent uneccessary updates if reached min threshold
        if (absVelocity <= minAngularVelocity)
        {
            return;
        }

        // angular velocity AVnew = AVcurrent + impulse * e^−decay×Δt
        angularVelocity *= Mathf.Exp(-decayRate * Time.deltaTime);

        // stop AV reached min threshold
        if (absVelocity <= minAngularVelocity)
        {
            angularVelocity = 0;
        }

        // angle θnew​ = θcurrent​ + AVnew ​* Δt
        activeBlocks.transform.rotation *= Quaternion.Euler(0, 0, angularVelocity * Time.deltaTime);
    }

    /// <summary>
    /// Process possible swipe gesture.
    /// </summary>
    /// <param name="duration">Duration between gesture end and start tinme.</param>
    private void HandlePossibleSwipe(float duration)
    {
        float distance = Vector2.Distance(pointerPressEndPos, pointerPressStartPos);

        // prevent possible bad input (sometimes InputSystem feeds values outside of the view)
        if (float.IsInfinity(distance) || float.IsNaN(distance))
        {
            return;
        }

#if DEBUG_LOG_INPUT
        Logger.Debug($"Swipe distance {distance} duration {duration} dir {dir}");
#endif // DEBUG_LOG_INPUT

        // swipe is deadbanded by distance and duration
        if ((distance >= minSwipeDistance) && (duration > 0))
        {
            // compute angle difference and speed
            Vector2 pivot = activeBlocks.transform.position;
            float angleDelta = Vector2.SignedAngle(pointerPressStartPos - pivot, pointerPressEndPos - pivot);
            float speed = distance / duration;

            // apply speed limit
            if (swipeSpeedLimit > 0)
            {
                speed = Mathf.Max(speed, swipeSpeedLimit);
            }

            // determine new velocity
            angularVelocity += angleDelta * speed * sensitivity;
#if DEBUG_LOG_INPUT
            Logger.Debug($"Angle delta {angleDelta}, speed {speed}, angularVelocity {angularVelocity}");
#endif // DEBUG_LOG_INPUT
        }
    }

    /// <summary>
    /// Expects PointerInput composite from the InputSystem on mouse/pointer or touch input.
    /// </summary>
    /// <param name="ctx">Context with PointerInput composite value.</param>
    public void OnPointerInput(InputAction.CallbackContext ctx)
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

        if (input.Contact && !dragging)
        {
            dragStartTime = Time.time;
            activeInputId = input.InputId;
            pointerPressStartPos = Camera.main.ScreenToWorldPoint(input.Position);
            dragging = true;

#if DEBUG_LOG_INPUT
            Logger.Debug($"{ctx.control.device.name}: started dragging at {pointerPressStartPos}");
#endif // DEBUG_LOG_INPUT

        } else if (input.Contact && dragging) {
            // previous input type changed or it was lost, discard
            if (activeInputId != input.InputId)
            {
                return;
            }

            Vector2 pointerCurrentPos = Camera.main.ScreenToWorldPoint(input.Position);
            Vector2 pivot = activeBlocks.transform.position;
            float angleDelta = Vector2.SignedAngle(pointerPressStartPos - pivot, pointerCurrentPos - pivot);
            angularVelocity += angleDelta * dragSensitivity;

            // update start position, as swipe shall happen from that point
            pointerPressStartPos = pointerCurrentPos;
        }
        else if (!input.Contact && dragging)
        {
            // previous input type changed or it was lost, discard
            if (activeInputId != input.InputId)
            {
                return;
            }

#if DEBUG_LOG_INPUT
            Logger.Debug($"{ctx.control.device.name}: done dragging at {pointerPressEndPos}");
#endif // DEBUG_LOG_INPUT

            pointerPressEndPos = Camera.main.ScreenToWorldPoint(input.Position);

            DebugUtils.DrawLine(pointerPressStartPos, pointerPressEndPos, Color.red, 3);

            HandlePossibleSwipe(Time.time - dragStartTime);
            dragging = false;
        }
    }
}
