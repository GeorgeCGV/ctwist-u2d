using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(MultiplierHandler))]
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public static event Action<int> OnScoreUpdate;

    public static event Action<string, Vector2> OnAnnounce;

    public static event Action<int, int> OnTimeLeftUpdate;
    public static event Action<Data.LevelData, Data.GameOverResults> OnGameOver;
    public static event Action<Data.LevelData> OnBeforeGameStarts;

    [SerializeField]
    private GameObject EfxOnStart;

    public AudioClip BackgroundMusic;
    public AudioClip SfxOnStart;
    public AudioClip SfxOnLost;
    public AudioClip SfxOnWin;

    public List<AudioClip> SfxBlocksClear;

    [SerializeField]
    public List<Spawn> SpawnNodes;

    private Data.LevelData level;

    [SerializeField]
    private ScoreConfig scoreConfig;

    private bool isLevelStarted;

    private bool isGameOver;

    private int score;

    private float timePassedInSeconds;

    private List<ColorBlock.EBlockColor> availableColors;

    #region Spawn
    [SerializeField]
    private float nextSpawnTime;
    private ProgressiveValueTimer nextSpawnTimeTimer;
    private float spawmTimer;
    #endregion
    #region SpawnSpeed
    [SerializeField]
    private float blockSpeed;
    private ProgressiveValueTimer blockSpeedTimer;
    #endregion
    #region Multiplier
    private MultiplierHandler multiplier;
    #endregion

    protected int Score
    {
        get
        {
            return score;
        }
        set
        {
            score = value;
            OnScoreUpdate?.Invoke(score);
        }
    }

    void Awake()
    {
        // prevent mutliple instances
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            multiplier = GetComponent<MultiplierHandler>();
            isLevelStarted = false;
            isGameOver = false;
            Score = 0;
            timePassedInSeconds = 0;
        }
    }

    protected ColorBlock.EBlockColor GetRandomColorFromAvailable()
    {
        ColorBlock.EBlockColor ret = ColorBlock.EBlockColor.Red;

        if (availableColors != null && availableColors.Count > 0)
        {
            ret = availableColors[UnityEngine.Random.Range(0, availableColors.Count)];
        }

        return ret;
    }

    protected void IncrementScore(int val)
    {
        Score += val;
    }

    public void SetPaused(bool value)
    {
        Time.timeScale = value ? 0 : 1;
    }

    public bool IsPaused()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorApplication.isPaused || Time.timeScale == 0;
#else
        return Time.timeScale == 0;
#endif
    }

    public bool IsStarted()
    {
        return isLevelStarted;
    }

    public bool IsRunning()
    {
        return IsStarted() && !IsPaused() && !isGameOver;
    }

    protected void GameOver(bool won)
    {
        isGameOver = true;

        AudioManager.Instance.StopMusic();

        if (won)
        {
            AudioManager.Instance.PlaySfx(SfxOnWin);
        }
        else
        {
            AudioManager.Instance.PlaySfx(SfxOnLost);
        }

        OnGameOver?.Invoke(level, new Data.GameOverResults(score, won));
    }

    void Update()
    {
        if (!IsRunning())
        {
            return;
        }

        timePassedInSeconds += Time.deltaTime;

        // check win conditions
        if (level.goalScore != 0)
        {
            // check if reached the score goal
            if (score >= level.goalScore)
            {
                // won
                GameOver(true);
            }
        }

        // check limit conditions
        if (level.limitTime != 0)
        {
            int seconds = 0;
            int minutes = 0;

            if (timePassedInSeconds >= level.limitTime)
            {
                // lost due to timeout
                GameOver(false);
            }
            else
            {
                float timeDiffInSeconds = level.limitTime - timePassedInSeconds;
                seconds = Mathf.FloorToInt(timeDiffInSeconds % 60);
                minutes = Mathf.FloorToInt(timeDiffInSeconds / 60);
            }

            OnTimeLeftUpdate?.Invoke(minutes, seconds);
        }

        // update block speed, increases from min to max
        blockSpeedTimer.Update(Time.deltaTime);

        // update next spawn time, decreases from max to min
        nextSpawnTimeTimer.Update(Time.deltaTime);

        // check if must spawn
        spawmTimer += Time.deltaTime;
        if (spawmTimer >= nextSpawnTime)
        {
            // try to spawn
            ColorBlock.EBlockColor colorToSpawn = GetRandomColorFromAvailable();

            // reset spawn timer if spawned, otherwise repeat on the next update
            if (SpawnNodes[UnityEngine.Random.Range(0, SpawnNodes.Count)].SpawnColorBlock(colorToSpawn, nextSpawnTime, blockSpeed))
            {
                spawmTimer = 0;
            }
        }
    }

    protected List<GameObject> GetFloatingBlocks()
    {
        GameObject active_blocks = GameObject.FindGameObjectWithTag("active_blocks");
        GameObject central = GameObject.FindGameObjectWithTag("central");
        List<GameObject> floatingBlocks = new List<GameObject>();

        // iterate over all blocks from the central
        // and reset the 'attached' flag
        Queue<GameObject> blocks = new Queue<GameObject>();
        blocks.Enqueue(central);
        GameObject current;
        GameObject other;
        while (blocks.Count != 0)
        {
            current = blocks.Dequeue();
            if (current.GetComponent<BasicBlock>().destroyed)
            {
                continue;
            }

            current.GetComponent<BasicBlock>().attached = false;

            foreach (BasicBlock.EdgeIndex edgeIndex in Enum.GetValues(typeof(BasicBlock.EdgeIndex)))
            {
                other = current.GetComponent<BasicBlock>().GetNeighbour(edgeIndex);
                if (other == null)
                {
                    continue;
                }

                if (other.GetComponent<BasicBlock>().attached)
                {
                    blocks.Enqueue(other);
                }
            }
        }

        // iterate over all active blocks and find all blocks that
        // still have 'attached' set
        foreach (Transform child in active_blocks.transform)
        {
            BasicBlock block = child.gameObject.GetComponent<BasicBlock>();
            if ((block == null) || (!block.gameObject.activeInHierarchy) || block.destroyed)
            {
                continue;
            }

            // invert the attached flag
            // block is floating if we couldn't
            // reach the block from central
            block.attached = !block.attached;

            // after attached flag inversion
            // it has correct state
            if (!block.attached)
            {
                floatingBlocks.Add(block.gameObject);
            }
        }

        return floatingBlocks;
    }

    public void OnBlocksObstructionCollision(GameObject active)
    {
        // game over for now
        // protect against subsequent calls due to called from blocks update
        if (isGameOver)
        {
            return;
        }

        GameOver(false);
    }

    public void OnBlocksAttach(GameObject active)
    {
        Queue<GameObject> blocks = new Queue<GameObject>();
        HashSet<GameObject> matches = new HashSet<GameObject>
        {
            active
        };

        // IncrementScore(ScorePerAttach);
        // OnAnnounce?.Invoke("+" + ScorePerAttach, active.transform.position);

        blocks.Enqueue(active);

        while (blocks.Count != 0)
        {
            GameObject current = blocks.Dequeue();

            foreach (BasicBlock.EdgeIndex edgeIndex in Enum.GetValues(typeof(BasicBlock.EdgeIndex)))
            {
                GameObject other = current.GetComponent<BasicBlock>().GetNeighbour(edgeIndex);
                if (other == null)
                {
                    continue;
                }

                if (matches.Contains(other))
                {
                    continue;
                }

                if (current.GetComponent<BasicBlock>().MatchesWith(other))
                {
                    blocks.Enqueue(other);
                    matches.Add(other);
                }
            }
        }

        int matchedAmount = matches.Count;
        Logger.Debug($"Got {matchedAmount} matches");

        if (matchedAmount >= 3)
        {
            foreach (GameObject block in matches)
            {
                block.GetComponent<BasicBlock>().Destroy();
                Destroy(block);
            }

            int matchScore;
            switch (matchedAmount)
            {
                case 3:
                    matchScore = scoreConfig.ScorePerMatch3;
                    break;
                case 4:
                    matchScore = scoreConfig.ScorePerMatch4;
                    break;
                case 5:
                    matchScore = scoreConfig.ScorePerMatchMore;
                    break;
                default:
                    matchScore = scoreConfig.ScorePerMatch3 + (matchedAmount - 5) * scoreConfig.ScorePerMatch3;
                    break;
            }

            matchScore *= multiplier.Multiplier;

            IncrementScore(matchScore);
            OnAnnounce?.Invoke("+" + matchScore, active.transform.position);

            int sfxBlockClearIdx = Math.Clamp(multiplier.Multiplier - 1, 0, SfxBlocksClear.Count - 1);
            AudioManager.Instance.PlaySfx(SfxBlocksClear[sfxBlockClearIdx]);

            // find floating/disconnected blocks
            List<GameObject> floatingBlocks = GetFloatingBlocks();
            int floatingScore = scoreConfig.ScorePerFloating * floatingBlocks.Count * multiplier.Multiplier;
            if (floatingScore != 0)
            {
                IncrementScore(floatingScore);
                OnAnnounce?.Invoke("+" + floatingScore, floatingBlocks[0].transform.position);
            }

            for (int i = floatingBlocks.Count - 1; i >= 0; i--)
            {
                GameObject block = floatingBlocks[i];
                block.GetComponent<BasicBlock>().Destroy();
                Destroy(block);
            }

            multiplier.Increment();
        }
    }

    /// <summary>
    /// Called when LoadScreen switched to the level scene and finished with
    /// its closing animation.
    /// </summary>
    public void OnLevelSceneLoaded(GameObject loadScreen, Data.LevelData data)
    {
        // disable blocks layer render
        Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("blocks"));

        UILevelController.OnGameStarAllAnimationsDone += OnLevelStart;

        level = data;

        if (level.colorsInLevel.Length == 0)
        {
            availableColors = null;
        }
        else
        {
            availableColors = new List<ColorBlock.EBlockColor>(level.colorsInLevel.Length);

            foreach (string colorStr in level.colorsInLevel)
            {
                if (Enum.TryParse(colorStr, out ColorBlock.EBlockColor color))
                {
                    availableColors.Add(color);
                }
                else
                {
                    Logger.Debug($"Failed to convert {colorStr} to block color, skip");
                }
            }
        }

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


        multiplier.Init(level.multiplier);

        CreateStartupBlocks(level.startBlocksNum, null);
        OnBeforeGameStarts?.Invoke(level);
    }

    private void OnLevelStart()
    {
        UILevelController.OnGameStarAllAnimationsDone -= OnLevelStart;
        AudioManager.Instance.PlayMusic(BackgroundMusic);
        AudioManager.Instance.PlaySfx(SfxOnStart);

        GameObject root = GameObject.FindGameObjectWithTag("central");
        Assert.IsNotNull(root);

        GameObject efx = Instantiate(EfxOnStart, root.transform.position, Quaternion.identity);
        efx.GetComponent<ParticleSystem>().Play();

        // enable blocks layer render
        Camera.main.cullingMask |= 1 << LayerMask.NameToLayer("blocks");

        isLevelStarted = true;
    }

    private void CreateStartupBlocks(int num, GameObject root = null)
    {
        int blocksLayer = LayerMask.NameToLayer("blocks");

        if (root == null)
        {
            root = GameObject.FindGameObjectWithTag("central");
        }

        Assert.IsNotNull(root);

        GameObject block = root;
        GameObject neighbour;
        BasicBlock.EdgeIndex edge;

        for (int i = 0; i < num; i++)
        {
            edge = BasicBlock.GetRandomEdge();
            neighbour = block.GetComponent<BasicBlock>().GetNeighbour(edge);

            // find any free edge
            while (neighbour != null)
            {
                block = neighbour;
                edge = BasicBlock.GetRandomEdge();
                neighbour = block.GetComponent<BasicBlock>().GetNeighbour(edge);
            }

            GameObject newBlock = BlocksFactory.Instance.NewColorBlock(GetRandomColorFromAvailable());
            // set to correct location
            newBlock.transform.parent = block.transform.parent;
            newBlock.transform.rotation = block.transform.rotation;
            newBlock.transform.position = block.transform.position + block.GetComponent<BasicBlock>().GetEdgeOffset(edge);
            // mark as already attached and disable rigidbody physics
            newBlock.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            newBlock.GetComponent<Rigidbody2D>().totalForce = Vector2.zero;
            newBlock.GetComponent<BasicBlock>().attached = true;
            // add to blocks layer
            // as physics update won't run between object linkage - force physics update
            // that shall update rigidbody after applied tranforms
            Physics2D.SyncTransforms();
            // link new block to neighbours
            int linkedNeighboursCount = newBlock.GetComponent<BasicBlock>().LinkWithNeighbours(blocksLayer);
            Assert.IsTrue(linkedNeighboursCount > 0);
            // add block to the blocks layer to allow raycasting against it
            newBlock.gameObject.layer = blocksLayer;


            Logger.Debug($"Created {newBlock.name} at {newBlock.transform.position} with {linkedNeighboursCount} links");

            // begin from root
            block = root;
        }
    }
}