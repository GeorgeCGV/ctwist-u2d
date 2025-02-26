using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages everything related to the blocks spawn.
///
/// Spawn blocks are queued and then independently
/// spawned using spawn nodes.
/// </summary>
public class Spawner : MonoBehaviour
{
    /// <summary>
    /// All available spawn nodes in the level.
    /// </summary>
    [SerializeField]
    public List<SpawnNode> SpawnNodes;

    /// <summary>
    /// Maintained list of "free" not busy with the spawn
    /// spawn nodes.
    /// </summary>
    private List<SpawnNode> freeSpawnNodes;

    /// <summary>
    /// Time until the next spawm enqueue.
    ///
    /// Ideally that can be factored out in a
    /// standalone class following a strategy
    /// pattern. That can allow to create
    /// different spawn sequences.
    /// </summary>
    private float nextSpawnTime;

    /// <summary>
    /// Elapsed time before nextSpawnTime.
    /// Accumulates Time.deltaTime.
    /// </summary>
    private float elapsedTime;

    /// <summary>
    /// Timer that decrements the nextSpawnTime.
    /// </summary>
    private ProgressiveValueTimer nextSpawnTimeTimer;

    /// <summary>
    /// Current block start speed to use for new
    /// block enqueue entities.
    /// </summary>
    private float blockSpeed;

    /// <summary>
    /// Timer that increments spawned block start
    /// speed (blockSpeed).
    /// </summary>
    private ProgressiveValueTimer blockSpeedTimer;

    /// <summary>
    /// Entities spawn queue.
    /// </summary>
    private Queue<ISpawnEntity> spawnQueue;

    /// <summary>
    /// Chance value [0.0; 1.0] for batch spawn enqueue.
    /// </summary>
    private float spawnBatchChance;

    /// <summary>
    /// Range for how many entities are generated
    /// When batch spawn enqueue happens.
    /// </summary>
    private (int min, int max) spawnBatchRange;

    /// <summary>
    /// Executed when any spawn nodes spawned an Entity.
    /// </summary>
    private Action<GameObject> onSpawnedCallback;

    /// <summary>
    /// Prepares the component and disables it to
    /// avoid Update calls.
    ///
    /// Higher layer must Init and Start the component (StartSpawner)
    /// when required.
    /// </summary>
    void Awake()
    {
        spawnQueue = new Queue<ISpawnEntity>();
        freeSpawnNodes = new List<SpawnNode>(SpawnNodes.Count);
        // all nodes are free at the beginning
        freeSpawnNodes.AddRange(SpawnNodes);
        // will be enabled when level starts
        enabled = false;
    }

    /// <summary>
    /// Initializes the spawner based on the level data.
    /// </summary>
    /// <param name="level">Level data.</param>
    internal void Init(Data.LevelData level, Action<GameObject> onSpawned = null)
    {
        onSpawnedCallback = onSpawned;
        spawnBatchChance = level.spawn.batchChance;
        spawnBatchRange = (Math.Max(1, level.spawn.batchMin), Math.Max(1, level.spawn.batchMax + 1));

        float decreaseSpawnTimeEverySeconds;
        float blockSpeedIncreaseEverySeconds;
        if (level.limit.time <= 0)
        {
            // no time limit, use actual seconds
            decreaseSpawnTimeEverySeconds = level.spawn.timeDecreasePerTimeSeconds;
            blockSpeedIncreaseEverySeconds = level.block.speedIncreasePerTimeSeconds;
        }
        else
        {
            decreaseSpawnTimeEverySeconds = level.limit.time * level.spawn.timeDecreasePerTimeLimitPercent * 0.01f;
            blockSpeedIncreaseEverySeconds = level.limit.time * level.block.speedIncreasePerTimeLimitPercent * 0.01f;
        }

        // spawn time goes from max to min
        nextSpawnTime = level.spawn.timeMax;
        nextSpawnTimeTimer = new ProgressiveValueTimer(nextSpawnTime, level.spawn.timeMin, level.spawn.timeDecreaseByTimePercent * 0.01f,
                                                       decreaseSpawnTimeEverySeconds,
                                                       delegate (float value) { nextSpawnTime = value; },
                                                       new ProgressiveValueTimer.DecrementalOperation());

        // speed goes from min to max
        blockSpeed = level.block.speedMin;
        blockSpeedTimer = new ProgressiveValueTimer(blockSpeed, level.block.speedMax, level.block.speedIncreaseBySpeedPercent * 0.01f,
                                                    blockSpeedIncreaseEverySeconds,
                                                    delegate (float value) { blockSpeed = value; },
                                                    new ProgressiveValueTimer.IncrementalOperation());
    }

    /// <summary>
    /// Enables the spawner, the update will run.
    /// </summary>
    public void StartSpawner()
    {
        enabled = true;
        elapsedTime = 0;
    }

    /// <summary>
    /// Disables the spawner, the update won't run.
    /// Stops all spawn nodes from spawning any scheduled block.
    /// </summary>
    public void StopSpawner()
    {
        enabled = false;
        foreach (SpawnNode node in SpawnNodes)
        {
            node.StopSpawn();
        }
    }

    void Update()
    {
        // update block speed, increases from min to max
        blockSpeedTimer.Update(Time.deltaTime);

        // update next spawn time, decreases from max to min
        nextSpawnTimeTimer.Update(Time.deltaTime);

        elapsedTime += Time.deltaTime;

        // auto spawn
        // check if must spawn
        if (elapsedTime >= nextSpawnTime)
        {
            elapsedTime = 0;
            int amountToSpawn = 1;

            if (spawnBatchChance != 0)
            {
                if (UnityEngine.Random.value <= spawnBatchChance)
                {
                    amountToSpawn = spawnBatchRange.min == spawnBatchRange.max
                                    ? spawnBatchRange.min
                                    : UnityEngine.Random.Range(spawnBatchRange.min, spawnBatchRange.max);
                }
            }

            float inSeconds;
            float speed;
            for (int i = 0; i < amountToSpawn; i++)
            {
                // add a bit of variability
                inSeconds = amountToSpawn == 1
                                    ? 0.5f : UnityEngine.Random.Range(nextSpawnTime - 0.5f, nextSpawnTime + 0.5f);
                speed = amountToSpawn == 1 ? blockSpeed : UnityEngine.Random.Range(blockSpeed - 1f, blockSpeed + 1f);

                // enqueue new spawn entity
                ColorBlock.EBlockColor rndColor = LevelManager.Instance.GetRandomColorFromAvailable();
                spawnQueue.Enqueue(new ColorBlockEntity(rndColor, inSeconds, blockSpeed));
            }
        }

        // try to schedule spawn entities to nodes
        // as many as we have in the queue
        while ((freeSpawnNodes.Count != 0) && (spawnQueue.Count != 0))
        {
            int rndFreeNodeIdx = UnityEngine.Random.Range(0, freeSpawnNodes.Count);

            // could use swap with latest and removal of the latest to avoid list shrinkage
            // but our list is small, so it is not a big problem
            SpawnNode rndFreeNode = freeSpawnNodes[rndFreeNodeIdx];
            freeSpawnNodes.RemoveAt(rndFreeNodeIdx);

            rndFreeNode.SpawnEntity(spawnQueue.Dequeue(), OnSpawnedHandler);
        }
    }

    /// <summary>
    /// Callback for the spawn node.
    /// Used to enqeue the spawn node back to
    /// "free"/not busy spawn points.
    /// </summary>
    /// <param name="node">Spawn node.</param>
    /// <param name="obj">Spawned entity.</param>
    private void OnSpawnedHandler(SpawnNode node, GameObject obj)
    {
        freeSpawnNodes.Add(node);
        if (obj) {
            onSpawnedCallback?.Invoke(obj);
        }
    }
}