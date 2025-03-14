using System;
using System.Collections;
using System.Collections.Generic;
using Blocks;
using Blocks.SpecialProperties;
using Configs;
using Model;
using Spawn;
using UI.Level;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Utils;
using static Model.BlockType;
using Array = System.Array;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MultiplierHandler), typeof(Spawner))]
public class LevelManager : MonoBehaviour
{
    private static readonly int EnvAnimatorNearTimeoutTrigger = Animator.StringToHash("NearTimeout");
    private static readonly int EnvAnimatorNearTimeoutSpeedValue = Animator.StringToHash("NearTimeoutSpeed");

    /// <summary>
    /// Maximum capacity of the field, excluding central block.
    /// </summary>
    private const int FieldBlocksCapacity = 450;
    
    public static LevelManager Instance { get; private set; }

    #region Actions
    
    public static event Action<int> OnScoreUpdate;
    public static event Action<BlocksStats> OnBlocksStatsUpdate;
    public static event Action<string, Vector2> OnAnnounce;
    public static event Action<int, int> OnTimeLeftUpdate;
    public static event Action<int, int> OnSpawnsLeftUpdate;
    public static event Action<LevelResults> OnGameOver;
    public static event Action<LevelData> OnBeforeGameStarts;
    
    #endregion

    #region SFX
    
    [SerializeField]
    private ParticleSystem efxOnStart;

    [SerializeField]
    public AudioClip backgroundMusic;

    [SerializeField]
    public AudioClip sfxOnStart;

    [SerializeField]
    public AudioClip sfxOnLost;

    [SerializeField]
    public AudioClip sfxOnWin;

    [SerializeField]
    public AudioClip sfxOnNearTimeout;
    
    public List<AudioClip> sfxBlocksClear;
    
    #endregion
    
    #region NearTimeout

    [SerializeField]
    private float nearTimeoutTime = 10.15f;

    [SerializeField]
    private Animator envAnimator;

    private bool _onNearTimeoutStarted;

    #endregion
    
    [SerializeField]
    private LevelData level;

    [SerializeField]
    private ScoreConfig scoreConfig;

    [SerializeField]
    private List<EBlockType> availableBlocks = new(Enum.GetValues(typeof(EBlockType)).Length);

    /// <summary>
    /// Is level started flag.
    /// </summary>
    /// <remarks>
    /// The level/game starts after startup animation is complete.
    /// </remarks>
    private bool _isLevelStarted;

    /// <summary>
    /// Is game-over flag.
    /// </summary>
    private bool _isGameOver;

    /// <summary>
    /// Current score.
    /// </summary>
    private int _score;

    /// <summary>
    /// Elapsed level time in seconds.
    /// </summary>
    /// <remarks>
    /// Won't count when paused.
    /// </remarks>
    private float _elapsedTime;

    /// <summary>
    /// Stores spawned but not yet attached blocks.
    /// </summary>
    /// <remarks>
    /// Newly spawned blocks are added here from <see cref="OnSpawnedBlock"/>
    /// and removed in <see cref="OnBlocksAttach"/>.
    /// </remarks>
    private readonly HashSet<BasicBlock> _notAttachedBlocks = new();

    /// <summary>
    /// Blocks stats.
    /// </summary>
    private readonly BlocksStats _blocksStats = new(Enum.GetValues(typeof(EBlockType)).Length);
    
    /// <summary>
    /// Spawner - responsible for blocks spawn queue and spawn nodes.
    /// </summary>
    private Spawner _spawner;
    
    /// <summary>
    /// Reference to <see cref="CentralBlock"/> present in the level.
    /// </summary>
    /// <remarks>
    /// Central block is tagged with <code>central</code> tag.
    /// </remarks>
    private CentralBlock _central;
    
    /// <summary>
    /// All attached blocks end up as a child of this object.
    /// </summary>
    /// <remarks>
    /// Player controls its rotation.
    /// </remarks>
    private GameObject _activeBlocks;
    
    /// <summary>
    /// Multiplier manager.
    /// </summary>
    private MultiplierHandler _multiplier;
    
    #region Obstruction

    /// <summary>
    /// An obstruction tilemap <see cref="obstructionTilemaps"/> is created
    /// as a child of this object.
    /// </summary>
    [SerializeField]
    private GameObject obstructionTmParent;

    /// <summary>
    /// SFX to play on collision with obstruction.
    /// </summary>
    [SerializeField]
    private List<AudioClip> sfxOnObstruction;

    /// <summary>
    /// Config that stores available level obstruction tilemaps.
    /// </summary>
    [SerializeField]
    private ObstructionPrefabsConfig obstructionTilemaps;

    #endregion

    private int Score
    {
        // no need for getter, avoid extra fn. call
        set
        {
            _score = value;
            // notify others
            OnScoreUpdate?.Invoke(_score);
        }
    }
    
    /// <summary>
    /// Increments current score by <c>val</c> amount.
    /// </summary>
    /// <param name="val">Amount to increment by.</param>
    private void IncrementScore(int val)
    {
        Score = _score + val;
    }
    
    public EBlockType GetRandomColorTypeFromAvailable()
    {
        Assert.IsFalse(availableBlocks.Count == 0, "availableBlocks must have elements");
        EBlockType type;

        do
        {
            type = availableBlocks[Random.Range(0, availableBlocks.Count)];
        } while (!EBlockTypeIsColorBlock(type));

        return type;
    }

    public static void SetPaused(bool value)
    {
        Time.timeScale = value ? 0 : 1;
    }

    public static bool IsPaused()
    {
#if UNITY_EDITOR
        return EditorApplication.isPaused || Time.timeScale == 0;
#else
        return Time.timeScale == 0;
#endif
    }

    public bool IsStarted()
    {
        return _isLevelStarted;
    }

    public bool IsRunning()
    {
        return IsStarted() && !IsPaused() && !_isGameOver;
    }

    private IEnumerator GameOverDelayed(bool won)
    {
        // delay to give effects some time
        yield return new WaitForSeconds(1);
        
        DestroyAllBlocks();

        // the music can be stopped to not interfere
        // with won / lost sfx.
        AudioManager.Instance.StopMusic();
        
        int baseScore = _score;
        int totalScore = baseScore;
        // bonus score points that will be added to the base score
        Dictionary<string, int> bonusScores = new();
        
        // set to current, unless player won
        int nextLevelId = level.ID;

        if (won)
        {
            AudioManager.Instance.PlaySfxPausable(sfxOnWin);
            
            // try to unlock next level
            nextLevelId = GameManager.Instance.NextLevel(level.ID);

            // compute bonus scores
            if (level.limit.Variant() == ELimitVariant.TimeLimit)
            {
                // from time left
                bonusScores["+Time"] = scoreConfig.ComputeBonusForTimeLimit(_elapsedTime,
                    level.limit.time);
            }
            else if (level.limit.Variant() == ELimitVariant.SpawnLimit)
            {
                // from elapsed time and spawns/moves left
                bonusScores["+Moves"] = scoreConfig.ComputeBonusForSpawnLimit(_elapsedTime,
                    _blocksStats.TotalSpawnedBlocksAmount,
                    level.limit.spawns);
            }
        }
        else
        {
            AudioManager.Instance.PlaySfxPausable(sfxOnLost);
        }

        // add bonus scores to total score
        foreach (int bonusScore in bonusScores.Values)
        {
            totalScore += bonusScore;
        }

        // determine how many stars were earned
        int starsEarned = 0;
        for (int i = 0; i < 3; i++)
        {
            starsEarned += totalScore >= level.starRewards[i] ? 1 : 0;
        }
        // store earned stars
        GameManager.SetLevelStars(level.ID, starsEarned);

        // try to store achieved score
        bool isHighscore = GameManager.SetLevelScoreChecked(level.ID, totalScore);
        OnGameOver?.Invoke(new LevelResults(baseScore, bonusScores, starsEarned,
            nextLevelId, GameManager.Instance.IsLastLevel(level.ID),
            won, isHighscore));
    }

    private void GameOver(bool won)
    {
        _isGameOver = true;
        _spawner.StopSpawner();
        StartCoroutine(GameOverDelayed(won));
    }

    /// <summary>
    /// Checks if level time limit is reached.
    /// </summary>
    /// <returns>True if limit reached, otherwise False.</returns>
    private bool IsTimeLimitReached()
    {
        bool limitReached = false;

        int seconds = 0;
        int minutes = 0;

        if (_elapsedTime >= level.limit.time)
        {
            // don't return here so that we could invoke OnTimeLeftUpdate
            limitReached = true;
        }
        else
        {
            float timeDiffInSeconds = level.limit.time - _elapsedTime;
            seconds = Mathf.FloorToInt(timeDiffInSeconds % 60);
            minutes = Mathf.FloorToInt(timeDiffInSeconds / 60);

            // 10 seconds clip
            if (!_onNearTimeoutStarted && (timeDiffInSeconds <= nearTimeoutTime))
            {
                _onNearTimeoutStarted = true;
                AudioManager.Instance.PlaySfxPausable(sfxOnNearTimeout);
                envAnimator.SetFloat(EnvAnimatorNearTimeoutSpeedValue,
                    Helpers.GetAnimatorClipLength(envAnimator, "NearTimeout") / nearTimeoutTime);
                envAnimator.SetTrigger(EnvAnimatorNearTimeoutTrigger);
            }
        }

        OnTimeLeftUpdate?.Invoke(minutes, seconds);
        return limitReached;
    }

    #region Unity
    
    private void Awake()
    {
        // prevent multiple instances
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            _isLevelStarted = false;
            _isGameOver = false;
            _score = 0;
            _elapsedTime = 0;
            _spawner = GetComponent<Spawner>();
            _central = GameObject.FindGameObjectWithTag("central").GetComponent<CentralBlock>();
            Assert.IsNotNull(_central, "missing central block, must have 'central' tag");
            _activeBlocks = GameObject.FindGameObjectWithTag("active_blocks");
            Assert.IsNotNull(_activeBlocks,
                "missing active blocks (attached blocks parent), must have 'active_blocks' tag");
            _multiplier = GetComponent<MultiplierHandler>();
            Assert.IsNotNull(_multiplier, "missing multiplier component");
        }
    }
    private void Update()
    {
        if (!IsRunning())
        {
            return;
        }

        _elapsedTime += Time.deltaTime;

        // check win conditions
        EGoalVariant goal = level.goal.Variant();
        if (goal == EGoalVariant.Score)
        {
            // check if reached the score goal
            if (_score >= level.goal.score)
            {
                // won
                GameOver(true);
                return;
            }
        }
        else if (goal == EGoalVariant.Blocks)
        {
            // check for all match goals to be complete
            int matchesTarget = level.goal.blocks.Length;
            foreach (BlocksGoal blocksGoal in level.goal.blocks)
            {
                if (_blocksStats.Matched.TryGetValue(blocksGoal.ParsedType, out int currentMatches))
                {
                    if (currentMatches >= blocksGoal.amount)
                    {
                        matchesTarget--;
                    }
                }
            }

            if (matchesTarget == 0)
            {
                // won
                GameOver(true);
                return;
            }
        }

        // process level limit(s) condition(s)
        ELimitVariant limit = level.limit.Variant();
        if (limit == ELimitVariant.TimeLimit)
        {
            if (IsTimeLimitReached())
            {
                // lost due to timeout
                GameOver(false);
            }
        }
        else if (limit == ELimitVariant.SpawnLimit)
        {
            // spawn/moves limited level
            if (_blocksStats.TotalSpawnedBlocksAmount >= level.limit.spawns)
            {
                // game-over state, but we shall await all blocks to "fall" / attach
                if (_notAttachedBlocks.Count == 0)
                {
                    // lost due to no more spawns/moves
                    GameOver(false);
                }
            }
        }
    }
    
    /// <summary>
    /// Unsubscribe from all actions.
    /// </summary>
    private void OnDestroy()
    {
        // subscribed at level startup; however, a user might exit
        // before level fully starts. Therefore, we shall remove
        // dangling subscription
        UILevelCoordinator.OnGameStartAllAnimationsDone -= OnLevelStart;
    }
    
    #endregion Unity

    /// <summary>
    /// Callback for the spawner when block spawns
    /// </summary>
    /// <remarks>
    /// <see cref="Spawner.Init"/>
    /// </remarks>
    /// <param name="block"></param>
    private void OnSpawnedBlock(BasicBlock block)
    {
        _blocksStats.AddSpawned(block.BlockType, 1);
        // we have to keep track of not yet attached objects
        // so that spawn level mode could wait until all blocks
        // are attached before game-over
        _notAttachedBlocks.Add(block);
        // notify others
        OnSpawnsLeftUpdate?.Invoke(_blocksStats.TotalSpawnedBlocksAmount, level.limit.spawns);
    }

    /// <summary>
    /// Get "floating" blocks.
    /// </summary>
    /// <remarks>
    /// Floating blocks - previously attached blocks that lost connection to the central block.
    /// </remarks>
    /// <returns>
    /// List of floating block game objects, never <c>null</c>.
    /// </returns>
    private List<BasicBlock> GetFloatingBlocks()
    {
        List<BasicBlock> floatingBlocks = new();

        // iterate over all blocks from the central
        // and reset the 'attached' flag
        Queue<GameObject> blocks = new();
        blocks.Enqueue(_central);
        
        while (blocks.Count != 0)
        {
            GameObject current = blocks.Dequeue();
            if (current.GetComponent<BasicBlock>().Destroyed)
            {
                continue;
            }

            current.GetComponent<BasicBlock>().attached = false;

            foreach (BasicBlock.EdgeIndex edgeIndex in Enum.GetValues(typeof(BasicBlock.EdgeIndex)))
            {
                GameObject other = current.GetComponent<BasicBlock>().GetNeighbour(edgeIndex);
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
        foreach (Transform child in _activeBlocks.transform)
        {
            BasicBlock block = child.gameObject.GetComponent<BasicBlock>();
            if ((block == null) || (!block.gameObject.activeInHierarchy) || block.Destroyed)
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
                floatingBlocks.Add(block);
            }
        }

        return floatingBlocks;
    }

    /// <summary>
    /// Handles block collision with obstruction.
    /// </summary>
    /// <param name="block"></param>
    public void OnBlocksObstructionCollision(BasicBlock block)
    {
        // game over for now
        // protect against subsequent calls due to called from blocks update
        if (_isGameOver)
        {
            return;
        }
        
        if (block.Destroyed)
        {
            return;
        }

        // play some random obstruction sfx
        if ((sfxOnObstruction != null) && (sfxOnObstruction.Count > 0))
        {
            AudioManager.Instance.PlaySfx(sfxOnObstruction[Random.Range(0, sfxOnObstruction.Count)]);
        }

        // destroy collided block
        block.DestroyBlock();
        Destroy(block.gameObject);

        // find and destroy possible floating blocks
        List<BasicBlock> floatingBlocks = GetFloatingBlocks();
        foreach (BasicBlock floatingBlock in floatingBlocks)
        {
            floatingBlock.DestroyBlock();
            Destroy(floatingBlock.gameObject);
        }
    }

    public void OnBlocksAttach(BasicBlock active)
    {
        _notAttachedBlocks.Remove(active);

        AudioManager.Instance.PlaySfx(active.GetComponent<BasicBlock>()?.SfxOnAttach());

        Queue<GameObject> blocks = new();
        HashSet<GameObject> matches = new()
        {
            active.gameObject
        };

        blocks.Enqueue(active.gameObject);

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
            bool stopNormalMatching = false;
            // process special properties
            List<EBlockType> bulkDestroyTypes = new List<EBlockType>();
            foreach (GameObject block in matches)
            {
                BasicBlock basicBlock = block.GetComponent<BasicBlock>();
                EMatchPropertyOutcome outcome = basicBlock.ProcessMatchProperties();
                if (outcome == EMatchPropertyOutcome.StopMatching)
                {
                    stopNormalMatching = true;
                } else if (outcome == EMatchPropertyOutcome.DestroyAllOfSameType)
                {
                    bulkDestroyTypes.Add(basicBlock.BlockType);
                }
            }

            if (bulkDestroyTypes.Count != 0)
            {
                // TODO: destroy all block of specific type
            }

            // stop if one of outcome was to stop further matching
            if (stopNormalMatching)
            {
                return;
            }
            
            foreach (GameObject block in matches)
            {
                BasicBlock basicBlock = block.GetComponent<BasicBlock>();
    
                if (basicBlock is ColorBlock cb)
                {
                    _blocksStats.AddMatched(cb.ColorType, 1);
                }
            
                basicBlock.DestroyBlock();
                Destroy(block);
            }
            
            int matchScore = scoreConfig.ComputeScoreForMatchAmount(matchedAmount) * _multiplier.Multiplier;

            IncrementScore(matchScore);
            OnAnnounce?.Invoke("+" + matchScore, active.transform.position);

            int sfxBlockClearIdx = Math.Clamp(_multiplier.Multiplier - 1, 0, sfxBlocksClear.Count - 1);
            AudioManager.Instance.PlaySfx(sfxBlocksClear[sfxBlockClearIdx]);

            // find floating/disconnected blocks
            List<BasicBlock> floatingBlocks = GetFloatingBlocks();
            int floatingScore = scoreConfig.scorePerFloating * floatingBlocks.Count * _multiplier.Multiplier;
            if (floatingScore != 0)
            {
                IncrementScore(floatingScore);
                OnAnnounce?.Invoke("+" + floatingScore, floatingBlocks[0].transform.position);
            }

            foreach (BasicBlock floatingBlock in floatingBlocks)
            {
                if (floatingBlock is ColorBlock cb)
                {
                    _blocksStats.AddMatched(cb.ColorType, 1);
                }

                floatingBlock.DestroyBlock();
                Destroy(floatingBlock.gameObject);
            }
            
            // notify about matched amounts change
            OnBlocksStatsUpdate?.Invoke(_blocksStats);

            _multiplier.Increment();

            int centralLinksCount = _central.GetComponent<BasicBlock>().LinksCount();
            // the level might get boring when no blocks are present;
            // however, don't do it for the spawn limit mode
            if (level.limit.Variant() != ELimitVariant.SpawnLimit)
            {
                // spawn if we have <= 1 connected links on the central block
                if (centralLinksCount <= 2)
                {
                    _spawner.SpawnRandomEntities(availableBlocks.Count > 2 ? Random.Range(1, 2) : 1);
                }
            }
        }
    }

    /// <summary>
    /// Initialize the level and kickstart startup animation sequence.
    /// </summary>
    /// <remarks>
    /// Called when <see cref="LevelLoader"/> switched to the level scene and finished with
    /// its closing animation.
    /// </remarks>
    public void OnLevelSceneLoaded(LevelData data)
    {
        // disable blocks layer render
        if (Camera.main != null)
        {
            Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("blocks"));
        }

        level = data;

        // create obstructions
        if (level.obstructionIdx >= 0)
        {
            GameObject tilemap = obstructionTilemaps.obstructionTilemapPrefabs[data.obstructionIdx];
            Assert.IsTrue(tilemap != null && tilemap.GetComponent<ObstacleCollisionDetector>() != null,
                "missing obstruction tilemap; must have ObstacleCollisionDetector script");
            Instantiate(tilemap, obstructionTmParent.transform);
        }

        Assert.IsFalse((level.ParsedBlocksInLevel == null) || (level.ParsedBlocksInLevel.Length == 0),
            "level must have blocks");
        availableBlocks.AddRange(level.ParsedBlocksInLevel);

        _spawner.Init(level, OnSpawnedBlock);
        _multiplier.Init(level.multiplier);

        CreateStartupBlocks(level.seed > 0 ? level.seed : Random.Range(0, int.MaxValue),
            level.startBlocksNum);

        UILevelCoordinator.OnGameStartAllAnimationsDone += OnLevelStart;
        OnBeforeGameStarts?.Invoke(level);
    }

    /// <summary>
    /// Called when UI finishes with startup animation.
    /// </summary>
    /// <remarks>
    /// <see cref="UILevelCoordinator.OnGameStartAllAnimationsDone"/>
    /// </remarks>
    private void OnLevelStart()
    {
        UILevelCoordinator.OnGameStartAllAnimationsDone -= OnLevelStart;

        AudioManager.Instance.PlayMusic(backgroundMusic);
        AudioManager.Instance.PlaySfx(sfxOnStart);

        Instantiate(efxOnStart, _central.transform.position, Quaternion.identity).Play();

        // enable blocks layer render
        if (Camera.main != null)
        {
            Camera.main.cullingMask |= 1 << LayerMask.NameToLayer("blocks");
        }

        // start the game
        _spawner.StartSpawner();
        _isLevelStarted = true;
    }
    
    /// <summary>
    /// Creates startup blocks starting from the root.
    /// </summary>
    /// <remarks>
    /// The central block is used when provided <c>root</c> is <c>null</c>.
    /// </remarks>
    /// <param name="seed">RNG seed.</param>
    /// <param name="num">Number of blocks to generate.</param>
    /// <param name="root">Root block or null.</param>
    private void CreateStartupBlocks(int seed, int num, BasicBlock root = null)
    {
        int blocksLayer = LayerMask.NameToLayer("blocks");

        if (root == null)
        {
            root = _central;
        }

        Assert.IsNotNull(root);

        BasicBlock block = root;

        System.Random rnd = new System.Random(seed);

        float chanceForChainedBlock = level.startBlocksChainedBlockChancePercent * 0.01f;

        Array edges = Enum.GetValues(typeof(BasicBlock.EdgeIndex));
        for (int i = 0; i < num; i++)
        {
            BasicBlock.EdgeIndex edge = (BasicBlock.EdgeIndex)edges.GetValue(rnd.Next(0, edges.Length));
            BasicBlock neighbour = block.GetNeighbour(edge);

            // find any free edge
            while (neighbour != null)
            {
                block = neighbour;
                edge = (BasicBlock.EdgeIndex)edges.GetValue(rnd.Next(0, edges.Length));
                neighbour = block.GetNeighbour(edge);
            }

            BasicBlock newBlock =
                BlocksFactory.Instance.NewColorBlock(availableBlocks[rnd.Next(0, availableBlocks.Count)]);
            // set to correct location
            newBlock.transform.parent = block.transform.parent;
            newBlock.transform.rotation = block.transform.rotation;
            newBlock.transform.position =
                (Vector2)block.transform.position + block.GetEdgeOffset(edge);
            // mark as already attached and disable rigidbody physics
            newBlock.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            newBlock.GetComponent<Rigidbody2D>().totalForce = Vector2.zero;
            newBlock.attached = true;
            // add to blocks layer
            // as physics update won't run between object linkage - force physics update
            // that shall update rigidbody after applied transforms
            Physics2D.SyncTransforms();
            // link new block to neighbours
            int linkedNeighboursCount = newBlock.LinkWithNeighbours(blocksLayer);
            Assert.IsTrue(linkedNeighboursCount > 0);
            // add block to the blocks layer to allow ray-casting against it
            newBlock.gameObject.layer = blocksLayer;
            
            // add special match property
            if (rnd.NextDouble() <= chanceForChainedBlock)
            {
                newBlock.SetMatchProperty(MatchPropertyFactory.Instance.NewChainedProperty());
            }
            
            Logger.Debug(
                $"Created {newBlock.name} at {newBlock.transform.position} with {linkedNeighboursCount} links");

            // begin from root
            block = root;
        }
    }

    /// <summary>
    /// Destroy all blocks (spawned, attached, floating) except <see cref="CentralBlock"/>.
    /// </summary>
    private void DestroyAllBlocks()
    {
        BasicBlock[] allObjects = FindObjectsByType<BasicBlock>(FindObjectsSortMode.None);
        foreach (BasicBlock block in allObjects)
        {
            if ((!block) || (block is CentralBlock) || (!block.gameObject.activeInHierarchy) || block.Destroyed)
            {
                continue;
            }

            block.DestroyBlock();
        }
    }

#if UNITY_EDITOR // simple way to extend editor without adding a ton of extra code
    public int testSeed;
    public int testSpawnNum;
    public bool testRecreateStartupBlocks;

    private void OnValidate()
    {
        if (!testRecreateStartupBlocks)
        {
            return;
        }

        testSeed++;
        DestroyAllBlocks();
        CreateStartupBlocks(testSeed, testSpawnNum);
        testRecreateStartupBlocks = false;
    }
#endif // UNITY_EDITOR
}