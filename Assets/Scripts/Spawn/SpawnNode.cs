
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Spawner's spawn node, spawns ISpawnEntity.
///
/// Controls visualization of a spawn and instantiates
/// the object when visualization is over.
/// </summary>
[RequireComponent(typeof(Light2D), typeof(SpriteRenderer))]
public class SpawnNode : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Light2D light2D;

    /// <summary>
    /// Sound effect to play when Entity spawns.
    /// TODO: move to BasicBlock to allow overrides?
    /// </summary>
    [SerializeField]
    private AudioClip SfxOnSpawn;

    /// <summary>
    /// Entity to spawn.
    /// </summary>
    private ISpawnEntity spawnEntity;

    /// <summary>
    /// Accumulates Time.deltaTime since SpawnEntity call.
    /// Used for the animation.
    /// </summary>
    private float elapsedTime;
    /// <summary>
    /// Animation duration.
    /// </summary>
    private float animationDuration;
    /// <summary>
    /// Executed when spawn animation is over and entity has spawned.
    /// </summary>
    private Action<SpawnNode, GameObject> onSpawnedCallback;

    /// <summary>
    /// Tied to the object's enable state.
    /// The object is enabled and updated when
    /// spawn entity is scheduled to be spawned.
    /// Upon entity spawn the object is deactivated
    /// to avoid further update calls.
    /// </summary>
    /// <value>True when object is enabled and
    //         will spawn, otherwise False.</value>
    public bool Busy
    {
        get
        {
            return enabled;
        }
        private set
        {
            enabled = value;
        }
    }

    /// <summary>
    /// Prepares the component.
    /// </summary>
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        light2D = GetComponent<Light2D>();
    }

    /// <summary>
    /// Schedules ISpawnEntity to be spawned.
    /// </summary>
    /// <param name="entity">Entity to spawn.</param>
    /// <param name="onSpawned">Callback to invoke when spawn node is done.</param>
    /// <returns>True if spawn is scheduled, otherwise False.</returns>
    public bool SpawnEntity(ISpawnEntity entity, Action<SpawnNode, GameObject> onSpawned = null)
    {
        if (Busy)
        {
            return false;
        }

        Assert.IsNotNull(entity);

        elapsedTime = 0;
        // avoids division by 0 in the update
        animationDuration = Mathf.Max(entity.SpawnInSeconds(), 0.1f);
        spawnEntity = entity;
        onSpawnedCallback = onSpawned;

        spriteRenderer.color = spawnEntity.SpawnColor();
        light2D.color = spawnEntity.BacklightColor();

        // start spriteRenderer and light2D
        spriteRenderer.enabled = true;
        light2D.enabled = true;

        // mark as busy (enables the component).
        Busy = true;

        return true;
    }

    /// <summary>
    /// Stop ongoing spawn (if any).
    /// </summary>
    public void StopSpawn()
    {
        spawnEntity = null;
        spriteRenderer.enabled = false;
        light2D.enabled = false;
        Busy = false;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        // animation progress
        float progress = elapsedTime / animationDuration;
        if (progress < 1)
        {
            // simple alpha animation that speeds up the closer we are to its end
            float animationSpeed = Mathf.Lerp(2f, 10f, progress);
            float newAlpha = Mathf.PingPong(elapsedTime * animationSpeed, 1f);
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, newAlpha);
        }
        else
        {
            GameObject obj = null;
            // animation is over
            // spawn if there is anything to spawn
            // the entity is set to null in StopSpawn,
            // otherwise it is always set
            if (spawnEntity != null)
            {
                obj = spawnEntity.Create();

                obj.transform.position = transform.position;
                BasicBlock block = obj.GetComponent<BasicBlock>();
                block.GravityStrength = spawnEntity.BlockStartSpeed();

                AudioManager.Instance.PlaySfx(SfxOnSpawn);
                spawnEntity = null;
            }

            spriteRenderer.enabled = false;
            light2D.enabled = false;

            // mark as free (disable the component)
            Busy = false;

            // invoke on "free"/done callback after
            // node state update
            onSpawnedCallback?.Invoke(this, obj);
        }
    }

#if UNITY_EDITOR // simple way to extend editor without adding a ton of extra code
    public float spawnIn = 1.0f;
    public bool spawn = false;

    void OnValidate()
    {
        if (spawn)
        {
            SpawnEntity(new ColorBlockEntity(ColorBlock.EBlockColor.Blue, spawnIn, 1), null);
            spawn = false;
        }
    }

#endif // UNITY_EDITOR
}
