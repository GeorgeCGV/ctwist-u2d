
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Animator), typeof(Light2D), typeof(SpriteRenderer))]
public class Spawn : MonoBehaviour
{
    public AudioClip SfxOnSpawn;
    public GameObject ColorBlockPrefab;

    private static readonly int animatorTriggerSpawn = Animator.StringToHash("Spawn");
    private static readonly int animatorTriggerStop = Animator.StringToHash("Stop");

    private float currentTimeInSeconds;
    private float targetTimeInSeconds;

    private bool busy = false;
    private bool done = false;

    private ColorBlock.EBlockColor spawnColor;
    private float spawnSpeed;

    private Animator animatorCtrl;
    private SpriteRenderer spriteRenderer;
    private Light2D light2D;

    void Awake()
    {
        animatorCtrl = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        light2D = GetComponent<Light2D>();
    }

    public bool SpawnColorBlock(ColorBlock.EBlockColor color, float spawnInSeconds, float speed)
    {
        if (busy)
        {
            return false;
        }

        done = false;
        busy = true;
        targetTimeInSeconds = spawnInSeconds;
        currentTimeInSeconds = 0;
        spawnColor = color;
        spawnSpeed = speed;

        // colorize the spwan node sprite
        spriteRenderer.color = ColorBlock.UnityColorFromBlockColor(spawnColor);
        light2D.color = ColorBlock.UnityColorFromBlockColor(spawnColor);

        // start spriteRenderer and light2D, begin to animate
        spriteRenderer.enabled = true;
        light2D.enabled = true;
        animatorCtrl.SetTrigger(animatorTriggerSpawn);

        return true;
    }

    void Update()
    {
        AnimatorStateInfo stateInfo = animatorCtrl.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Spawn"))
        {
            currentTimeInSeconds += Time.deltaTime;

            float remainingTime = targetTimeInSeconds - currentTimeInSeconds;

            if (remainingTime > 0)
            {
                // Speed multiplier increases as time gets closer to targetTime
                float speedMultiplier = 10 / remainingTime;
                // Prevent extreme values
                animatorCtrl.speed = Mathf.Clamp(speedMultiplier, 1f, 2f);
            }
            else
            {
                if (!done)
                {
                    spriteRenderer.enabled = false;
                    light2D.enabled = false;
                    animatorCtrl.SetTrigger(animatorTriggerStop);
                    done = true;
                }
            }
        }
        else if (stateInfo.IsName("Idle"))
        {
            if (done)
            {
                busy = false;
                done = false;

                AudioManager.Instance.PlaySfx(SfxOnSpawn);

                GameObject obj = BlocksFactory.Instance.NewColorBlock(spawnColor);
                obj.transform.position = transform.position;
                BasicBlock block = obj.GetComponent<BasicBlock>();
                block.GravityStrength = spawnSpeed;
                // block.stepForce = spawnSpeed * .25f;
            }
        }
    }
}
