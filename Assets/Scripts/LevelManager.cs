using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(MultiplierHandler), typeof(Spawner))]
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public static event Action<int> OnScoreUpdate;

    public static event Action<string, Vector2> OnAnnounce;

    public static event Action<int, int> OnTimeLeftUpdate;
    public static event Action<Data.LevelData, Data.LevelResults> OnGameOver;
    public static event Action<Data.LevelData> OnBeforeGameStarts;

    [SerializeField]
    private GameObject EfxOnStart;

    [SerializeField]
    public AudioClip BackgroundMusic;
    [SerializeField]
    public AudioClip SfxOnStart;
    [SerializeField]
    public AudioClip SfxOnLost;
    [SerializeField]
    public AudioClip SfxOnWin;
    [SerializeField]
    public AudioClip SfxOnNearTimeout;

    #region NearTimeout
    [SerializeField]
    private float nearTimeoutTime = 10.15f;

    [SerializeField]
    private Animator envAnimator;

    private bool onNearTimeoutStarted;
    #endregion

    public List<AudioClip> SfxBlocksClear;

    private Data.LevelData level;

    [SerializeField]
    private ScoreConfig scoreConfig;

    private bool isLevelStarted;

    private bool isGameOver;

    private int score;

    private float timePassedInSeconds;

    [SerializeField]
    private List<ColorBlock.EBlockColor> availableColors = new List<ColorBlock.EBlockColor>();

    #region Multiplier
    private MultiplierHandler multiplier;
    #endregion
    #region Obstruction
    [SerializeField]
    private GameObject obstructionTmParent;
    [SerializeField]
    private List<AudioClip> SfxOnObstruction;
    [SerializeField]
    private ObstructionPrefabsConfig obstructionTilemaps;
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

    public ColorBlock.EBlockColor GetRandomColorFromAvailable()
    {
        Assert.IsFalse(availableColors.Count == 0, "availableColors must have elements");
        return availableColors[UnityEngine.Random.Range(0, availableColors.Count)];
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

    protected IEnumerator GameOverDelayed(bool won)
    {
        yield return new WaitForSeconds(1);

        AudioManager.Instance.StopMusic();

        int nextLevelId = level.id;
        if (won)
        {
            // try to unlock next level
            nextLevelId = GameManager.Instance.NextLevel(level.id);
            AudioManager.Instance.PlaySfxPausable(SfxOnWin);
        }
        else
        {
            AudioManager.Instance.PlaySfxPausable(SfxOnLost);
        }

        // Determine how many stars were earned
        int starsEarned = 0;
        for (int i = 0; i < 3; i++)
        {
            starsEarned += score >= level.starRewards[i] ? 1 : 0;
        }
        // store earned stars
        GameManager.Instance.SetLevelStars(level.id, starsEarned);

        // try to store achieved score
        bool isHighscore = GameManager.Instance.SetLevelScoreChecked(level.id, score);

        OnGameOver?.Invoke(level, new Data.LevelResults(score, won,
                                                        nextLevelId,
                                                        GameManager.Instance.IsLastLevel(level.id),
                                                        starsEarned,
                                                        isHighscore));
    }

    protected void GameOver(bool won)
    {
        isGameOver = true;

        GetComponent<Spawner>().StopSpawner();
        DestroyAll();

        StartCoroutine(GameOverDelayed(won));
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

                // 10 seconds clip
                if (!onNearTimeoutStarted && (timeDiffInSeconds <= nearTimeoutTime))
                {
                    onNearTimeoutStarted = true;
                    AudioManager.Instance.PlaySfxPausable(SfxOnNearTimeout);
                    envAnimator.SetFloat("NearTimeoutSpeed", Utils.GetAnimatorClipLength(envAnimator, "NearTimeout") / nearTimeoutTime);
                    envAnimator.SetTrigger("NearTimeout");
                }
            }

            OnTimeLeftUpdate?.Invoke(minutes, seconds);
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

        if (active.GetComponent<BasicBlock>().destroyed)
        {
            return;
        }

        if (SfxOnObstruction != null)
        {
            AudioManager.Instance.PlaySfx(SfxOnObstruction[UnityEngine.Random.Range(0, SfxOnObstruction.Count)]);
        }

        active.GetComponent<BasicBlock>().Destroy();
        Destroy(active);

        List<GameObject> floatingBlocks = GetFloatingBlocks();
        for (int i = floatingBlocks.Count - 1; i >= 0; i--)
        {
            GameObject block = floatingBlocks[i];
            block.GetComponent<BasicBlock>().Destroy();
            Destroy(block);
        }
    }

    public void OnBlocksAttach(GameObject active)
    {
        AudioManager.Instance.PlaySfx(active.GetComponent<BasicBlock>()?.SfxOnAttach());

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

        // create obstructions
        if (data.obstructionIdx >= 0)
        {
            Instantiate(obstructionTilemaps.obstructionTilemapPrefabs[data.obstructionIdx], obstructionTmParent.transform);
        }

        UILevelController.OnGameStartAllAnimationsDone += OnLevelStart;

        level = data;
        Assert.IsFalse(level.colorsInLevel.Length == 0, "level msut have color blocks");

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

        GetComponent<Spawner>().Init(level);

        multiplier.Init(level.multiplier);

        CreateStartupBlocks(level.seed > 0 ? level.seed : UnityEngine.Random.Range(0, int.MaxValue), level.startBlocksNum, null);
        OnBeforeGameStarts?.Invoke(level);
    }

    private void OnLevelStart()
    {
        UILevelController.OnGameStartAllAnimationsDone -= OnLevelStart;
        AudioManager.Instance.PlayMusic(BackgroundMusic);
        AudioManager.Instance.PlaySfx(SfxOnStart);

        GameObject root = GameObject.FindGameObjectWithTag("central");
        Assert.IsNotNull(root);

        GameObject efx = Instantiate(EfxOnStart, root.transform.position, Quaternion.identity);
        efx.GetComponent<ParticleSystem>().Play();

        // enable blocks layer render
        Camera.main.cullingMask |= 1 << LayerMask.NameToLayer("blocks");

        isLevelStarted = true;

        GetComponent<Spawner>().StartSpawner();
    }

    void OnDestroy()
    {
        // subsribed at level startup; however, a user might exit
        // before level fully starts. Therefore, we shall remove
        // dangling subscription
        UILevelController.OnGameStartAllAnimationsDone -= OnLevelStart;
    }

    private void CreateStartupBlocks(int seed, int num, GameObject root = null)
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

        System.Random rnd = new System.Random(seed);

        Array edges = Enum.GetValues(typeof(BasicBlock.EdgeIndex));

        for (int i = 0; i < num; i++)
        {
            edge = (BasicBlock.EdgeIndex)edges.GetValue(rnd.Next(0, edges.Length));
            neighbour = block.GetComponent<BasicBlock>().GetNeighbour(edge);

            // find any free edge
            while (neighbour != null)
            {
                block = neighbour;
                edge = (BasicBlock.EdgeIndex)edges.GetValue(rnd.Next(0, edges.Length));
                neighbour = block.GetComponent<BasicBlock>().GetNeighbour(edge);
            }

            GameObject newBlock = BlocksFactory.Instance.NewColorBlock(availableColors[rnd.Next(0, availableColors.Count)]);
            // set to correct location
            newBlock.transform.parent = block.transform.parent;
            newBlock.transform.rotation = block.transform.rotation;
            newBlock.transform.position = (Vector2)block.transform.position + block.GetComponent<BasicBlock>().GetEdgeOffset(edge);
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

    public void DestroyAll()
    {
        BasicBlock[] allObjects = FindObjectsByType<BasicBlock>(FindObjectsSortMode.None);
        foreach (BasicBlock block in allObjects)
        {
            if ((block == null) || (block is CentralBlock) || (!block.gameObject.activeInHierarchy) || block.destroyed)
            {
                continue;
            }

            block.Destroy();
            Destroy(block.gameObject);
        }
    }

#if UNITY_EDITOR // simple way to extend editor without adding a ton of extra code
    public int seed;
    public int spawnNum;
    public bool recreateStartupBlocks = false;

    void OnValidate()
    {
        if (recreateStartupBlocks)
        {
            seed++;
            DestroyAll();
            CreateStartupBlocks(seed, spawnNum);
            recreateStartupBlocks = false;
        }
    }

#endif // UNITY_EDITOR
}