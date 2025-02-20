using InputSamples.Drawing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputManager : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float speedDecayFactor = 1.0f;

    [SerializeField, Min(0.1f)]
    private float speedMultiplier = 1.0f;

    [SerializeField, Min(0.1f)]
    private float swipeDirectionThreshold = 0.6f;

    [SerializeField]
    private float minSwipeDistance = 0.2f;

    [SerializeField]
    private float maxSwipeDuration = 1.0f;

    private PlayerInput playerInput;

    private GameObject activeBlocks;

    private float speed;

    /// <summary>
    /// Tap/Click start position in world coordinates.
    /// </summary>
    private Vector2 pointerPressStartPos;

    /// <summary>
    /// Tap/Click end position in world coordinates.
    /// </summary>
    private Vector2 pointerPressEndPos;

    private void Awake()
    {
        activeBlocks = GameObject.FindGameObjectWithTag("active_blocks");
        playerInput = GetComponent<PlayerInput>();
        // playerInput.currentActionMap.
    }

    void Update()
    {
        float absSpeed = Mathf.Abs(speed);

        // apply rotation
        activeBlocks.transform.Rotate(speed < 0 ? Vector3.forward : Vector3.back, absSpeed * Time.deltaTime);

        // decrease the speed
        if (speed > 0)
        {
            speed -= absSpeed * speedDecayFactor * Time.deltaTime;
        }
        else if (speed < 0)
        {
            speed += absSpeed * speedDecayFactor * Time.deltaTime;
        }

        // prevent oscilations, stop when withing some deadband
        if (speed < .2f && speed > -.2f)
        {
            speed = .0f;
        }
    }

    protected void AdaptSpeed(float duration)
    {
        // swipe deadband by distance and time
        float swipeDistance = Vector3.Distance(pointerPressStartPos, pointerPressEndPos);
        if (float.IsInfinity(swipeDistance) || float.IsNaN(swipeDistance))
        {
            return;
        }
        Vector2 dir = (pointerPressEndPos - pointerPressStartPos).normalized;
        Debug.Log($"Swipe distance {swipeDistance} duration {duration} dir {dir}");

        if ((swipeDistance >= minSwipeDistance) &&
            (duration > 0) && (duration <= maxSwipeDuration))
        {
            var screenWorldEndPos = Camera.main.ScreenToWorldPoint(pointerPressEndPos);
            Debug.DrawLine(Camera.main.ScreenToWorldPoint(pointerPressStartPos), Camera.main.ScreenToWorldPoint(pointerPressEndPos), Color.green, 5);

            // depending on where the pointer ends we have to invert
            // the direction of the wheel; as:
            // moving blocks to the left when above mid screen - rotates them to the left
            // moving blocks to the left when below mid screen - rotates them to the right
            // the same inversion applies for up and down; imagine wheel rotation
            // PS: our mid screen is (0, 0)
            bool invertY = screenWorldEndPos.y > 0;
            bool invertX = screenWorldEndPos.x > 0;

            // make it a bit more interesting, using total blocks amount as the mass
            // that resists rapid speed change when more blocks are present
            float massFactor = activeBlocks.transform.childCount;

            // compute speed change
            float speedChange = swipeDistance / duration * speedMultiplier / massFactor;
            speedChange = Mathf.Clamp(speedChange, 0, 1500);

            Debug.Log($"speedChange {speedChange}, mass: {massFactor}, duration {duration}");

            // determine left & right dir
            if (Vector2.Dot(dir, Vector2.left) > swipeDirectionThreshold)
            {
                if (invertY)
                {
                    speed -= speedChange;
                }
                else
                {
                    speed += speedChange;
                }
            }
            else if (Vector2.Dot(dir, Vector2.right) > swipeDirectionThreshold)
            {
                if (invertY)
                {
                    speed += speedChange;
                }
                else
                {
                    speed -= speedChange;
                }
            }

            // determine up & down dir
            if (Vector2.Dot(dir, Vector2.up) > swipeDirectionThreshold)
            {
                if (invertX)
                {
                    speed -= speedChange;
                }
                else
                {
                    speed += speedChange;
                }
            }
            else if (Vector2.Dot(dir, Vector2.down) > swipeDirectionThreshold)
            {
                if (invertX)
                {
                    speed += speedChange;
                }
                else
                {
                    speed -= speedChange;
                }
            }
        }
    }

    public void OnPress(InputAction.CallbackContext ctx)
    {
        if (!LevelManager.Instance.IsRunning())
        {
            return;
        }

        if (playerInput.currentControlScheme != "Keyboard&Mouse")
        {
            return;
        }

        if (ctx.started)
        {
            pointerPressStartPos = Mouse.current.position.ReadValue();
        }
        else if (ctx.canceled)
        {
            pointerPressEndPos = Mouse.current.position.ReadValue();
            AdaptSpeed((float)ctx.duration);
        }
    }

    private bool dragging;
    private float dragStartTime;
    private int activeInputId;
    public void OnTouchscreenInput(InputAction.CallbackContext ctx)
    {
        PointerInput input = ctx.ReadValue<PointerInput>();

        if (input.Contact && !dragging)
        {
            dragStartTime = Time.time;
            activeInputId = input.InputId;
            pointerPressStartPos = input.Position;
            dragging = true;
            Debug.Log($"Started dragging at {pointerPressStartPos}");
        }
        else if (input.Contact && dragging)
        {
            if (activeInputId != input.InputId) {
                return;
            }
            pointerPressEndPos = input.Position;
        }
        else
        {
            if (activeInputId != input.InputId) {
                return;
            }
            Debug.Log($"Done dragging at {pointerPressEndPos}");

            AdaptSpeed(Time.time - dragStartTime);
            dragging = false;
        }
    }
}
