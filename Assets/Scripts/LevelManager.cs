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
    public AudioClip sfxOnMoreBlocks;
    
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
    /// Stores spawned but not yet attached blocks.
    /// </summary>
    /// <remarks>
    /// Managing references to attached blocks helps to avoid expensive
    /// calls during blocks iteration.
    /// </remarks>
    private readonly HashSet<BasicBlock> _attachedBlocks = new(FieldBlocksCapacity);

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
    
    /// <summary>
    /// Reused to store matched blocks, avoids new allocation.
    /// </summary>
    /// <remarks>
    /// Used in <see cref="OnBlocksAttach"/>. Capacity is set based on expected average case./>.
    /// </remarks>
    private readonly HashSet<BasicBlock> _matches = new(FieldBlocksCapacity / 4);
    
    /// <summary>
    /// Flag that is set after a successful match.
    /// </summary>
    /// <remarks>
    /// Used for post match processing (i.e. remove floating blocks).
    /// </remarks>
    private bool _matchPerformedInFrame;
    
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
    private AudioClip sfxOnObstruction;

    /// <summary>
    /// Config that stores available level obstruction tilemaps.
    /// </summary>
    [SerializeField]
    private ObstructionPrefabsConfig obstructionTilemaps;
    
    /// <summary>
    /// Stores blocks that collided with obstructions and must be destroyed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Destruction happens in <see cref="LateUpdate"/>.
    /// </para>
    /// <para>
    /// Helps to avoid destruction in Physic's subsystem callback,
    /// reduces GC calls. 
    /// </para>
    /// <para>
    /// Capacity is based on expected average case.
    /// </para>
    /// </remarks>
    private readonly HashSet<BasicBlock> _obstructedToDestroy = new(25);
    
    #endregion
    
    /// <summary>
    /// Traverses blocks starting from start block.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses BFS.
    /// </para>
    /// <para>
    /// Ignores destroyed blocks.
    /// </para>
    /// </remarks>
    /// <param name="startBlock">Starting block.</param>
    /// <param name="visitedBlocks">Visited blocks following provided condition.</param>
    /// <param name="condition">
    /// Optional condition to check the blocks based on some criteria, can be Null.
    /// <para>
    /// Takes current block and its neighbour as parameters. Returns True, if neighbour shall be visited.
    /// </para>
    /// </param>
    public static void TraverseBlocks(BasicBlock startBlock, HashSet<BasicBlock> visitedBlocks,
                                      Func<BasicBlock, BasicBlock, bool> condition = null)
    {
        // Stopwatch traverseStopwatch = Stopwatch.StartNew();

        Queue<BasicBlock> blocks = new();

        blocks.Enqueue(startBlock);
        visitedBlocks.Add(startBlock);

        while (blocks.Count != 0)
        {
            BasicBlock current = blocks.Dequeue();
            for (int edgeIndex = 0; edgeIndex < BasicBlock.EdgeIndexes.Length; edgeIndex++)
            {
                BasicBlock other = current.GetNeighbour(edgeIndex);
                if (other is null || visitedBlocks.Contains(other) || other.Destroyed)
                {
                    continue;
                }
                
                bool shouldAdd = condition == null || condition(current, other);
                if (!shouldAdd)
                {
                    continue;
                }
                
                blocks.Enqueue(other);
                visitedBlocks.Add(other);
            }
        }
        
        // traverseStopwatch.Stop();
        // Logger.Debug($"TraverseBlocks {traverseStopwatch.ElapsedTicks} ticks {traverseStopwatch.ElapsedMilliseconds} ms");
    }
        
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
        DestroyAllBlocks();

        // delay to give effects some time
        yield return new WaitForSeconds(1);
        
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
                int movesBonus = scoreConfig.ComputeBonusForSpawnLimit(_elapsedTime,
                    _blocksStats.TotalSpawnedBlocksAmount,
                    level.limit.spawns);
                if (movesBonus > 0)
                {
                    bonusScores["+Moves"] = movesBonus;
                }
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
        Assert.AreEqual(3, level.starRewards.Length, "level data has misses starRewards, expected 3");
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
            if (!_onNearTimeoutStarted && timeDiffInSeconds <= nearTimeoutTime)
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
            BasicBlock.OnBlockDestroyed += OnBlockDestroyed;
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

    /// <summary>
    /// Checks if reached level goals.
    /// </summary>
    /// <returns>True if achieved a goal, otherwise False.</returns>
    private bool CheckGoals()
    {
        EGoalVariant goal = level.goal.Variant();
        if (goal == EGoalVariant.Score)
        {
            // check if reached the score goal
            if (_score >= level.goal.score)
            {
                // won
                return true;
            }
        }
        else if (goal == EGoalVariant.Blocks)
        {
#if !UNITY_EDITOR
            // hack: fix block goals label update
            // outside of unity it doesn't go to 0
            // after the match has updated it in the LateUpdate;
            // re-setting it here works as in the Editor...
            OnBlocksStatsUpdate?.Invoke(_blocksStats);
#endif
            // check for all match goals to be complete
            int matchesTarget = level.goal.blocks.Length;
            foreach (BlocksGoal blocksGoal in level.goal.blocks)
            {
                if (!_blocksStats.Matched.TryGetValue(blocksGoal.ParsedType, out int currentMatches))
                {
                    continue;
                }
                
                if (currentMatches >= blocksGoal.amount)
                {
                    matchesTarget--;
                }
            }

            if (matchesTarget == 0)
            {
                // won
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if reached level limit.
    /// </summary>
    /// <returns>True if reached a limit, otherwise False.</returns>
    private bool CheckLimits()
    {
        ELimitVariant limit = level.limit.Variant();
        if (limit == ELimitVariant.TimeLimit)
        {
            if (IsTimeLimitReached())
            {
                // lost due to timeout
                return true;
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
                   return true;
                }
            }
        }
        
        return false;
    }
    
    
    private void Update()
    {
        if (!IsRunning())
        {
            return;
        }

        _elapsedTime += Time.deltaTime;

        // process goals
        if (CheckGoals())
        {
            GameOver(true);
            return;
        }

        // process level limit(s) condition(s)
        if (CheckLimits())
        {
            GameOver(false);
        }
    }

    private void LateUpdate()
    {
        
#if !UNITY_EDITOR // allow late update for editor tests
        if (!IsRunning())
        {
            return;
        }
#endif
        List<BasicBlock> floatingBlocks = new();

        // process match related event first
        // that allows player to get points
        // when obstructed blocks were destroyed
        // by a match (i.e. in some root node)
        if (_matchPerformedInFrame)
        {
            // find floating/disconnected blocks, grant score and destroy them
            GetFloatingBlocks(floatingBlocks);

            int floatingScore = scoreConfig.scorePerFloating * floatingBlocks.Count * _multiplier.Multiplier;
            if (floatingScore != 0)
            {
                IncrementScore(floatingScore);
                OnAnnounce?.Invoke("+" + floatingScore, _central.transform.position);
            }

            foreach (BasicBlock floatingBlock in floatingBlocks)
            {
                if (floatingBlock is ColorBlock cb)
                {
                    _blocksStats.AddMatched(cb.ColorType, 1);
                }

                floatingBlock.DestroyBlock();
            }
            
            // notify about matched amounts change
            OnBlocksStatsUpdate?.Invoke(_blocksStats);
            
            _matchPerformedInFrame = false;
            floatingBlocks.Clear();
            
        
            // the level might get boring when no blocks are present;
            // create some blocks if central has none
            if (_central.LinksCount() == 0)
            {
                CreateStartupBlocks((int)Time.deltaTime, Random.Range(2, 7));
                Instantiate(efxOnStart, _central.transform.position, Quaternion.identity).Play();
                AudioManager.Instance.PlaySfx(sfxOnMoreBlocks);
            }
        }

        // only run if there are any blocks collided
        // with an obstacle/obstruction tm this frame
        if (_obstructedToDestroy.Count != 0)
        {
            AudioManager.Instance.PlaySfx(sfxOnObstruction);
            // first, destroy collided blocks
            foreach (BasicBlock block in _obstructedToDestroy)
            {
                block.DestroyBlock();
            }
            _obstructedToDestroy.Clear();
            // then clean floating
            GetFloatingBlocks(floatingBlocks);
            foreach (BasicBlock floatingBlock in floatingBlocks)
            {
                floatingBlock.DestroyBlock();
            }
        }
    }

    private int ProcessMatches()
    {
        // process special properties
        bool stopNormalMatching = false;
        BasicBlock specialMatchRuleBlock = null;
        foreach (BasicBlock block in _matches)
        {
            EMatchPropertyOutcome outcome = block.CheckMatchProperty();
            if (outcome == EMatchPropertyOutcome.StopMatching)
            {
                stopNormalMatching = true;
            }
            else if (outcome == EMatchPropertyOutcome.SpecialMatchRule)
            {
                // matches contain blocks of the same type
                // ignore if the same special rule is present more than once
                // atm, there is only 1 such effect - glow property;
                // so we can simply skip if it is already present
                if (specialMatchRuleBlock == null)
                {
                    specialMatchRuleBlock = block;
                }
            }
        }

        // stop if any special match property required to stop further matching
        if (stopNormalMatching)
        {
            return 0;
        }
        
        // if special block match rule exist, process it
        if (specialMatchRuleBlock != null)
        {
            IMatchProperty special = specialMatchRuleBlock.MatchProperty;
            special.ExecuteSpecial(specialMatchRuleBlock, _matches);
        }
        
        // destroy all matched blocks
        foreach (BasicBlock block in _matches)
        {
            if (block is ColorBlock cb)
            {
                _blocksStats.AddMatched(cb.ColorType, 1);
            }
        
            block.DestroyBlock();
        }

        return _matches.Count;
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
        // subscribed in awake
        BasicBlock.OnBlockDestroyed -= OnBlockDestroyed;
    }

    private void OnBlockDestroyed(BasicBlock block)
    {
        _attachedBlocks.Remove(block);
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
    private void GetFloatingBlocks(List<BasicBlock> floatingBlocks)
    {
        HashSet<BasicBlock> visitedBlocks = new();
        
        // iterate over all blocks from the central
        TraverseBlocks(_central, visitedBlocks);  

        // iterate over all attached blocks and find all blocks that
        // still have 'attached' set
        foreach (BasicBlock attachedBlock in _attachedBlocks)
        {
            if (attachedBlock.Destroyed || visitedBlocks.Contains(attachedBlock))
            {
                continue;
            }
            attachedBlock.attached = false;
            floatingBlocks.Add(attachedBlock);
        }
    }
    
    // private readonly HashSet<BasicBlock> _obstructedToDestroy = new(FieldBlocksCapacity);

    /// <summary>
    /// Handles block collision with obstruction.
    /// </summary>
    /// <remarks>
    /// Executed by <see cref="BasicBlock.Update"/> upon collision with obstruction tile.
    /// </remarks>
    /// <param name="block">Collided block.</param>
    public void OnBlocksObstructionCollision(BasicBlock block)
    {
        // game over for now
        // protect against subsequent calls due to called from blocks update
        if (_isGameOver)
        {
            return;
        }

        // schedule for destruction
        _obstructedToDestroy.Add(block);
    }

    /// <summary>
    /// Handles block collision event.
    /// </summary>
    /// <remarks>
    /// Executed by <see cref="BasicBlock.OnCollisionEnter2D"/> upon successful attachment/placement.
    /// </remarks>
    /// <param name="attachedBlock">Attached block.</param>
    public void OnBlocksAttach(BasicBlock attachedBlock)
    {
        // System.Diagnostics.Stopwatch onAttachStopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        AudioManager.Instance.PlaySfx(attachedBlock.SfxOnAttach());
        
        _notAttachedBlocks.Remove(attachedBlock);
        _attachedBlocks.Add(attachedBlock);

        // iterate over all blocks from the attachedBlock
        TraverseBlocks(attachedBlock, _matches, (block, neighbour) => block.MatchesWith(neighbour));  
        // FindMatches(attachedBlock, _matches);

        int matchedAmount = _matches.Count;
        Logger.Debug($"Got {matchedAmount} matches");

        // match must have at least 3 blocks
        if (matchedAmount < 3)
        {
            _matches.Clear();
            return;
        }

        // match count could change
        matchedAmount = ProcessMatches();
        // clear matches hash for the next event
        _matches.Clear();
        
        // nothing to process
        if (matchedAmount == 0)
        {
            return;
        }
        
        // grant match scores
        int matchScore = scoreConfig.ComputeScoreForMatchAmount(matchedAmount) * _multiplier.Multiplier;
        IncrementScore(matchScore);
        OnAnnounce?.Invoke("+" + matchScore, attachedBlock.transform.position);

        // play match sound
        int sfxBlockClearIdx = Math.Clamp(_multiplier.Multiplier - 1, 0, sfxBlocksClear.Count - 1);
        AudioManager.Instance.PlaySfx(sfxBlocksClear[sfxBlockClearIdx]);

        _multiplier.Increment();
        
        // signal about a need to process floating blocks
        _matchPerformedInFrame = true;
        
        // onAttachStopwatch.Stop();
        // Logger.Debug($"onAttach {onAttachStopwatch.ElapsedTicks} ticks {onAttachStopwatch.ElapsedMilliseconds} ms");
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
        AudioManager.Instance.StopMusic();

        // disable blocks layer render
        if (Camera.main != null)
        {
            Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("blocks"));
        }

        Assert.IsNotNull(data, "LevelData is null");
        level = data;

        // create obstructions
        if (level.obstructionIdx >= 0)
        {
            Assert.IsNotNull(obstructionTilemaps, "missing obstructionTilemaps");
            Assert.IsNotNull(obstructionTmParent, "missing obstructionTmParent");
            Assert.IsTrue(level.obstructionIdx >= 0 && level.obstructionIdx < obstructionTilemaps.obstructionTilemapPrefabs.Count, 
                 "level requires obstruction idx that is not present");
            
            GameObject tilemap = obstructionTilemaps.obstructionTilemapPrefabs[level.obstructionIdx];
            Assert.IsTrue(tilemap?.GetComponent<ObstacleCollisionDetector>() != null,
                "missing obstruction tilemap; must have ObstacleCollisionDetector script");
            
            Instantiate(tilemap, obstructionTmParent.transform);
        }

        Assert.IsFalse(level.ParsedBlocksInLevel == null || level.ParsedBlocksInLevel.Length == 0,
            "level must have blocks");
        availableBlocks.Clear(); // clear blocks set in the Editor 
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
        if (Camera.main is not null)
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

        for (int i = 0; i < num; i++)
        {
            BasicBlock.EdgeIndex edge = BasicBlock.EdgeIndexes[rnd.Next(0, BasicBlock.EdgeIndexes.Length)];
            BasicBlock neighbour = block.GetNeighbour(edge);

            // find any free edge
            while (neighbour != null)
            {
                block = neighbour;
                edge = BasicBlock.EdgeIndexes[rnd.Next(0, BasicBlock.EdgeIndexes.Length)];
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
            _attachedBlocks.Add(newBlock);
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
                newBlock.MatchProperty = MatchPropertyFactory.Instance.NewProperty(MatchPropertyFactory.EMatchProperty.ChainProperty);
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
    private static void DestroyAllBlocks()
    {
        BasicBlock[] allObjects = FindObjectsByType<BasicBlock>(FindObjectsSortMode.None);
        foreach (BasicBlock block in allObjects)
        {
            if (!block || block is CentralBlock || !block.gameObject.activeInHierarchy || block.Destroyed)
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
    public bool testDestroyAllBlocks;

    private void OnValidate()
    {
        if (testRecreateStartupBlocks)
        {
            testSeed++;
            DestroyAllBlocks();
            CreateStartupBlocks(testSeed, testSpawnNum);
            testRecreateStartupBlocks = false;
        }

        if (testDestroyAllBlocks)
        {
            DestroyAllBlocks();
            testDestroyAllBlocks  = false;
        }
    }
#endif // UNITY_EDITOR
}