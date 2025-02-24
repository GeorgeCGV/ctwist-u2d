using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField]
    private float spawnNodeAnimation = 0.5f;

    [SerializeField]
    public List<Spawn> SpawnNodes;

    [SerializeField]
    private float nextSpawnTime;
    private ProgressiveValueTimer nextSpawnTimeTimer;
    private float spawmTimer;

    [SerializeField]
    private float blockSpeed;
    private ProgressiveValueTimer blockSpeedTimer;

    internal void Init(Data.LevelData level)
    {
        float decreaseSpawnTimeEverySeconds;
        float blockSpeedIncreaseEverySeconds;
        if (level.limitTime <= 0)
        {
            // no time limit, use actual seconds
            decreaseSpawnTimeEverySeconds = level.spawn.timeDecreasePerTimeSeconds;
            blockSpeedIncreaseEverySeconds = level.block.speedIncreasePerTimeSeconds;
        }
        else
        {
            decreaseSpawnTimeEverySeconds = level.limitTime * level.spawn.timeDecreasePerTimeLimitPercent * 0.01f;
            blockSpeedIncreaseEverySeconds = level.limitTime * level.block.speedIncreasePerTimeLimitPercent * 0.01f;
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

    void Update()
    {
        if ((blockSpeedTimer == null) || (nextSpawnTimeTimer == null)) {
            return;
        }

        // update block speed, increases from min to max
        blockSpeedTimer.Update(Time.deltaTime);

        // update next spawn time, decreases from max to min
        nextSpawnTimeTimer.Update(Time.deltaTime);

        // check if must spawn
        spawmTimer += Time.deltaTime;
        if (spawmTimer >= (nextSpawnTime - spawnNodeAnimation))
        {
            // try to spawn
            ColorBlock.EBlockColor colorToSpawn = LevelManager.Instance.GetRandomColorFromAvailable();

            // reset spawn timer if spawned, otherwise repeat on the next update
            if (SpawnNodes[UnityEngine.Random.Range(0, SpawnNodes.Count)].SpawnColorBlock(colorToSpawn, spawnNodeAnimation, blockSpeed))
            {
                spawmTimer = 0;
            }
        }
    }
}