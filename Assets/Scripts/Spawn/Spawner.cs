using System;
using System.Collections.Generic;
using Blocks;
using Blocks.SpecialProperties;
using Model;
using UnityEngine;
using static Model.BlockType;
using Random = UnityEngine.Random;

namespace Spawn
{
    /// <summary>
    /// Manages everything related to the blocks spawn.
    ///
    /// Spawn blocks are queued and then independently
    /// spawned using spawn nodes.
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        /// <summary>
        /// Value for the spawn limit when no limit is set.
        /// </summary>
        private const int SpawnUnlimited = -1;

        /// <summary>
        /// All available spawn nodes in the level.
        /// </summary>
        [SerializeField]
        private List<SpawnNode> spawnNodes;

        /// <summary>
        /// Maintained list of "free" / not busy / available spawn nodes.
        /// </summary>
        private List<SpawnNode> _freeSpawnNodes;

        /// <summary>
        /// Time until the next spawn enqueue.
        /// </summary>
        /// <remarks>
        /// Ideally that can be factored out in a
        /// standalone class following a strategy
        /// pattern. That can allow to create
        /// different spawn sequences.
        /// </remarks>
        private float _nextSpawnTime;

        /// <summary>
        /// Elapsed time before nextSpawnTime.
        /// Accumulates Time.deltaTime.
        /// </summary>
        private float _elapsedTime;

        /// <summary>
        /// Timer that decrements the nextSpawnTime.
        /// </summary>
        private ProgressiveValueTimer _nextSpawnTimeTimer;

        /// <summary>
        /// Current block start speed to use for new
        /// block enqueue entities.
        /// </summary>
        private float _blockSpeed = 1.0f;

        /// <summary>
        /// Timer that increments spawned block start
        /// speed (blockSpeed).
        /// </summary>
        private ProgressiveValueTimer _blockSpeedTimer;

        /// <summary>
        /// Entities spawn queue.
        /// </summary>
        private Queue<ISpawnEntity> _spawnQueue;

        /// <summary>
        /// Chance value [0.0; 1.0] for batch spawn enqueue.
        /// </summary>
        private float _spawnBatchChance;

        /// <summary>
        /// Range for how many entities are generated
        /// When batch spawn enqueue happens.
        /// </summary>
        private (int min, int max) _spawnBatchRange;

        /// <summary>
        /// Executed when any spawn nodes spawned an Entity.
        /// </summary>
        private Action<BasicBlock> _onSpawnedCallback;

        /// <summary>
        /// Hard limit on the amount of blocks the spawner
        /// can spawn during its life.
        /// </summary>
        private int _spawnsLeft;

        /// <summary>
        /// A chance to spawn <see cref="StoneBlock"/>.
        /// </summary>
        /// <value>[0.0; 1.0]</value>
        private float _stoneBlockChance;
        
        /// <summary>
        /// A chance to spawn a block with <see cref="ChainedProperty"/>.
        /// </summary>
        /// <value>[0.0; 1.0]</value>
        private float _chainedPropertyChance;
        
        /// <summary>
        /// Callback for the spawn node.
        /// Used to enqueue the spawn node back to
        /// "free"/not busy spawn points.
        /// </summary>
        /// <param name="node">Spawn node.</param>
        /// <param name="obj">Spawned entity.</param>
        private void OnSpawnedHandler(SpawnNode node, BasicBlock obj)
        {
            _freeSpawnNodes.Add(node);
            if (obj)
            {
                _onSpawnedCallback?.Invoke(obj);
            }
        }

        /// <summary>
        /// Initializes the spawner based on the level data.
        /// </summary>
        /// <param name="data">Level data.</param>
        /// <param name="onSpawned">On spawned callback.</param>
        public void Init(LevelData data, Action<BasicBlock> onSpawned = null)
        {
            _spawnsLeft = data.limit.Variant() == ELimitVariant.SpawnLimit ? data.limit.spawns : SpawnUnlimited;
            _stoneBlockChance = data.spawn.stoneBlockChancePercent * 0.01f;
            _chainedPropertyChance = data.spawn.chainedBlockChancePercent * 0.01f;
            _onSpawnedCallback = onSpawned;
            _spawnBatchChance = data.spawn.batchChance;
            _spawnBatchRange = (Math.Max(1, data.spawn.batchMin), Math.Max(1, data.spawn.batchMax + 1));

            float decreaseSpawnTimeEverySeconds;
            float blockSpeedIncreaseEverySeconds;
            if (data.limit.time <= 0)
            {
                // no time limit, use actual seconds
                decreaseSpawnTimeEverySeconds = data.spawn.timeDecreasePerTimeSeconds;
                blockSpeedIncreaseEverySeconds = data.block.speedIncreasePerTimeSeconds;
            }
            else
            {
                decreaseSpawnTimeEverySeconds = data.limit.time * data.spawn.timeDecreasePerTimeLimitPercent * 0.01f;
                blockSpeedIncreaseEverySeconds = data.limit.time * data.block.speedIncreasePerTimeLimitPercent * 0.01f;
            }

            // spawn time goes from max to min
            _nextSpawnTime = data.spawn.timeMax;
            _nextSpawnTimeTimer = new ProgressiveValueTimer(_nextSpawnTime, data.spawn.timeMin,
                data.spawn.timeDecreaseByTimePercent * 0.01f,
                decreaseSpawnTimeEverySeconds,
                delegate(float value) { _nextSpawnTime = value; },
                new ProgressiveValueTimer.DecrementalOperation());

            // speed goes from min to max
            _blockSpeed = data.block.speedMin;
            _blockSpeedTimer = new ProgressiveValueTimer(_blockSpeed, data.block.speedMax,
                data.block.speedIncreaseBySpeedPercent * 0.01f,
                blockSpeedIncreaseEverySeconds,
                delegate(float value) { _blockSpeed = value; },
                new ProgressiveValueTimer.IncrementalOperation());
        }

        /// <summary>
        /// Enables the spawner, the update will run.
        /// </summary>
        public void StartSpawner()
        {
            enabled = true;
            _elapsedTime = 0;
        }

        /// <summary>
        /// Disables the spawner, the update won't run.
        /// Stops all spawn nodes from spawning any scheduled block.
        /// </summary>
        public void StopSpawner()
        {
            enabled = false;
            foreach (SpawnNode node in spawnNodes)
            {
                node.StopSpawn();
            }
        }

        /// <summary>
        /// Enqueues amount of random entities to be spawned.
        /// </summary>
        /// <param name="amount">Amount to spawn.</param>
        public void SpawnRandomEntities(int amount)
        {
            // limit spawn amount if not unlimited
            if (_spawnsLeft != SpawnUnlimited)
            {
                amount = Mathf.Min(amount, _spawnsLeft);
                _spawnsLeft -= amount;
            }

            float inSeconds;
            float speed;
            IMatchProperty matchProperty;
            for (int i = 0; i < amount; i++)
            {
                // add a bit of variability
                if (amount == 1)
                {
                    inSeconds = 0.5f;
                    speed = _blockSpeed;
                }
                else
                {
                    inSeconds = Mathf.Min(Random.Range(_nextSpawnTime - 0.5f, _nextSpawnTime + 0.5f), 0.1f);
                    speed = Mathf.Min(Random.Range(_blockSpeed - 1f, _blockSpeed + 1f));
                }

                // enqueue new spawn entity
                EBlockType type = (_stoneBlockChance > 0) && (Random.value <= _stoneBlockChance)
                    ? EBlockType.Stone
                    : LevelManager.Instance.GetRandomColorTypeFromAvailable();

                // special match property
                matchProperty = null;
                if (type != EBlockType.Stone && Random.value <= _chainedPropertyChance)
                {
                    matchProperty = MatchPropertyFactory.Instance.NewChainedProperty();
                }
                
                _spawnQueue.Enqueue(new SpawnBlockEntity(type, inSeconds, speed, matchProperty));
            }
        }

        #region Unity

        /// <summary>
        /// Prepares the component and disables it to
        /// avoid Update calls.
        ///
        /// Higher layer must Init and Start the component (StartSpawner)
        /// when required.
        /// </summary>
        private void Awake()
        {
            _spawnQueue = new Queue<ISpawnEntity>();
            _freeSpawnNodes = new List<SpawnNode>(spawnNodes.Count);
            // all nodes are free at the beginning
            _freeSpawnNodes.AddRange(spawnNodes);
            // will be enabled when level starts
            enabled = false;
        }

        /// <summary>
        /// Updates all timers, enqueues new spawn entities and schedules them across available spawn nodes.
        /// </summary>
        private void Update()
        {
            // update block speed, increases from min to max
            _blockSpeedTimer.Update(Time.deltaTime);

            // update next spawn time, decreases from max to min
            _nextSpawnTimeTimer.Update(Time.deltaTime);

            _elapsedTime += Time.deltaTime;

            // auto spawn
            // check if should enqueue more spawns
            if (_elapsedTime >= _nextSpawnTime)
            {
                _elapsedTime = 0;
                int amountToSpawn = 1;

                if (_spawnBatchChance != 0)
                {
                    if (Random.value <= _spawnBatchChance)
                    {
                        amountToSpawn = _spawnBatchRange.min == _spawnBatchRange.max
                            ? _spawnBatchRange.min
                            : Random.Range(_spawnBatchRange.min, _spawnBatchRange.max);
                    }
                }

                SpawnRandomEntities(amountToSpawn);
            }

            // try to schedule spawn entities to nodes
            // as many as we have in the queue
            while ((_freeSpawnNodes.Count != 0) && (_spawnQueue.Count != 0))
            {
                int rndFreeNodeIdx = Random.Range(0, _freeSpawnNodes.Count);

                // could use swap with latest and removal of the latest to avoid list shrinkage
                // but our list is small, so it is not a big problem
                SpawnNode rndFreeNode = _freeSpawnNodes[rndFreeNodeIdx];
                _freeSpawnNodes.RemoveAt(rndFreeNodeIdx);

                rndFreeNode.SpawnEntity(_spawnQueue.Dequeue(), OnSpawnedHandler);
            }
        }

        #endregion Unity
    }
}