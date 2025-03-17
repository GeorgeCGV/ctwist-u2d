using System;
using Blocks;
using Model;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace Spawn
{
    /// <summary>
    /// Spawner's spawn node, spawns ISpawnEntity.
    ///
    /// Controls visualization of a spawn and instantiates
    /// the object when visualization is over.
    /// </summary>
    [RequireComponent(typeof(Light2D), typeof(SpriteRenderer))]
    public class SpawnNode : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private Light2D _light2D;

        /// <summary>
        /// Sound effect to play when Entity spawns.
        /// TODO: move to BasicBlock to allow overrides?
        /// </summary>
        [SerializeField]
        private AudioClip sfxOnSpawn;

        /// <summary>
        /// Entity to spawn.
        /// </summary>
        private ISpawnEntity _spawnEntity;

        /// <summary>
        /// Accumulates Time.deltaTime since SpawnEntity call.
        /// Used for the animation.
        /// </summary>
        private float _elapsedTime;
        /// <summary>
        /// Animation duration.
        /// </summary>
        private float _animationDuration;
        /// <summary>
        /// Executed when spawn animation is over and entity has spawned.
        /// </summary>
        private Action<SpawnNode, BasicBlock> _onSpawnedCallback;

        /// <summary>
        /// Tied to the object's enable state.
        /// The object is enabled and updated when
        /// spawn entity is scheduled to be spawned.
        /// Upon entity spawn the object is deactivated
        /// to avoid further update calls.
        /// </summary>
        /// <value>True when object is enabled and will spawn, otherwise False.</value>
        public bool Busy
        {
            get => enabled;
            private set => enabled = value;
        }

        /// <summary>
        /// Schedules ISpawnEntity to be spawned.
        /// </summary>
        /// <param name="entity">Entity to spawn.</param>
        /// <param name="onSpawned">Callback to invoke when spawn node is done.</param>
        /// <returns>True if spawn is scheduled, otherwise False.</returns>
        public bool SpawnEntity(ISpawnEntity entity, Action<SpawnNode, BasicBlock> onSpawned = null)
        {
            if (Busy)
            {
                return false;
            }

            Assert.IsNotNull(entity);

            _elapsedTime = 0;
            // avoids division by 0 in the update
            _animationDuration = Mathf.Max(entity.SpawnInSeconds(), 0.1f);
            _spawnEntity = entity;
            _onSpawnedCallback = onSpawned;

            _spriteRenderer.color = _spawnEntity.SpawnColor();
            _light2D.color = _spawnEntity.BacklightColor();

            // start spriteRenderer and light2D
            _spriteRenderer.enabled = true;
            _light2D.enabled = true;

            // mark as busy (enables the component).
            Busy = true;

            return true;
        }

        /// <summary>
        /// Stop ongoing spawn (if any).
        /// </summary>
        public void StopSpawn()
        {
            _spawnEntity = null;
            _spriteRenderer.enabled = false;
            _light2D.enabled = false;
            Busy = false;
        }

        #region Unity
        
        /// <summary>
        /// Prepares the component.
        /// </summary>
        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _light2D = GetComponent<Light2D>();
        }
        private void Update()
        {
            _elapsedTime += Time.deltaTime;

            // animation progress
            float progress = _elapsedTime / _animationDuration;
            if (progress < 1)
            {
                // simple alpha animation that speeds up the closer we are to its end
                float animationSpeed = Mathf.Lerp(2f, 10f, progress);
                float newAlpha = Mathf.PingPong(_elapsedTime * animationSpeed, 1f);
                _spriteRenderer.color = new Color(_spriteRenderer.color.r, _spriteRenderer.color.g, _spriteRenderer.color.b, newAlpha);
            }
            else
            {
                BasicBlock block = null;
                // animation is over
                // spawn if there is anything to spawn
                // the entity is set to null in StopSpawn,
                // otherwise it is always set
                if (_spawnEntity != null)
                {
                    block = _spawnEntity.Create();

                    block.transform.position = transform.position;
                    block.gravityStrength = _spawnEntity.BlockStartSpeed() * 0.5f;
                    block.AddStartSpeed(_spawnEntity.BlockStartSpeed());
                    float torque = _spawnEntity.BlockStartSpeed() * 0.2f;
                    block.AddTorque(Random.Range(-torque, torque));
                    AudioManager.Instance.PlaySfx(sfxOnSpawn);
                    _spawnEntity = null;
                }

                _spriteRenderer.enabled = false;
                _light2D.enabled = false;

                // mark as free (disable the component)
                Busy = false;

                // invoke on "free"/done callback after
                // node state update
                _onSpawnedCallback?.Invoke(this, block);
            }
        }

#if UNITY_EDITOR // simple way to extend editor without adding a ton of extra code
        public float testSpawnIn = 1.0f;
        public float testSpawnBlockSpeed = 1.0f;
        public bool testSpawn;
        public BlockType.EBlockType testSpawnType = BlockType.EBlockType.Blue;
        
        private void OnValidate()
        {
            if (!testSpawn)
            {
                return;
            }

            if (BlockType.EBlockTypeIsColorBlock(testSpawnType))
            {
                SpawnEntity(new SpawnBlockEntity(testSpawnType, testSpawnIn, testSpawnBlockSpeed));
            }
            
            testSpawn = false;
        }
#endif // UNITY_EDITOR
        
        #endregion Unity
    }
}