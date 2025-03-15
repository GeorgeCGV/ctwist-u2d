using UnityEngine;

/// <summary>
/// Manages overall game state.
/// </summary>
/// <remarks>
/// <para>
/// Provides info on how many levels are there.
/// Provides generic player and settings persistence.
/// </para>
/// <para>
/// The instance is not destructible, as it has to be present
/// across scenes.
/// </para>
/// </remarks>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Total amount of available levels in the game.
    /// </summary>
    [SerializeField, Min(0)]
    private int availableLevelsAmount;

    public int TotalLevels => availableLevelsAmount;
    
    /// <summary>
    /// Checks if the level ID is the final level in the game.
    /// </summary>
    /// <param name="levelId">Level ID.</param>
    /// <returns>True if the leve is final, otherwise False.</returns>
    public bool IsLastLevel(int levelId)
    {
        return levelId == availableLevelsAmount - 1;
    }

    #region Unity
    
    private void Awake()
    {
        // prevent multiple instances
        if (Instance != null && Instance != this)
        {
            // destroy the entire object
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // prevent destruction
            DontDestroyOnLoad(this);
        }
    }
    
    #endregion Unity

    /// <summary>
    /// Gets next level based on played level id.
    /// Unlocks next level if required.
    /// </summary>
    /// <param name="playedLevelId">Played level id.</param>
    /// <returns>Unlocked level id or current level id when not unlocked.</returns>
    public int NextLevel(int playedLevelId)
    {
        // if it was the last available level,
        // then there is nothing to unlock
        if (IsLastLevel(playedLevelId))
        {
            return playedLevelId;
        }

        // get last unlocked lvl id
        int lastUnlockedLevelId = CurrentLevelId();

        // replaying, simply return the next level id
        if (playedLevelId != lastUnlockedLevelId)
        {
            return playedLevelId + 1;
        }

        // unlock the next level if we played the last unlocked level
        int nextLevelId = lastUnlockedLevelId + 1;
        PlayerPrefs.SetInt("currentLevel", nextLevelId);
        PlayerPrefs.Save();
        return nextLevelId;
    }

    #region PlayerPrefs

    public static int CurrentLevelId()
    {
        return PlayerPrefs.GetInt("currentLevel", 0);
    }

    public static bool IsLevelUnlocked(int levelId)
    {
        return levelId <= CurrentLevelId();
    }

    public static int GetLevelStars(int levelId)
    {
        return PlayerPrefs.GetInt($"stars{levelId}", 0);
    }

    public static void SetLevelStars(int levelId, int stars)
    {
        // update level stars, but only if earned more than before
        int previouslyEarnedAmount = PlayerPrefs.GetInt($"stars{levelId}", 0);
        if (stars <= previouslyEarnedAmount)
        {
            return;
        }
        PlayerPrefs.SetInt($"stars{levelId}", stars);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Sets achieved score if it is higher than previously achieved.
    /// </summary>
    /// <param name="levelId">Level id</param>
    /// <param name="score">New score</param>
    /// <returns>True if new score is new high score, otherwise False.</returns>
    public static bool SetLevelScoreChecked(int levelId, int score)
    {
        // update level stars, but only if earned more than before
        int previouslyEarnedAmount = PlayerPrefs.GetInt($"score{levelId}", 0);

        if (score <= previouslyEarnedAmount)
        {
            return false;
        }
        
        PlayerPrefs.SetInt($"score{levelId}", score);
        PlayerPrefs.Save();
        return true;
    }

    public static bool IsMusicOn()
    {
        return PlayerPrefs.GetInt("musicOn", 1) == 1;
    }

    public static void SetMusicOption(bool musicOn)
    {
        PlayerPrefs.SetInt("musicOn", musicOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool IsSFXOn()
    {
        return PlayerPrefs.GetInt("sfxOn", 1) == 1;
    }

    public static void SetSfxOption(bool sfxOn)
    {
        PlayerPrefs.SetInt("sfxOn", sfxOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    #endregion
}