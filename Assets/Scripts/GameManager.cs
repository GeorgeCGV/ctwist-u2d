using UnityEngine;

/// <summary>
/// Manages overall game state.
/// Provides info on how many levels are there.
/// Provides generic player and settins persistence.
///
/// The instance is not destructable, as it has to be present
/// across the scenes.
///
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Total amount of available levels in the game.
    /// </summary>
    [SerializeField]
    private int availableLevelsAmount;

    public int TotalLevels => availableLevelsAmount;

    void Awake()
    {
        // prevent mutliple instances
        if (Instance != null && Instance != this)
        {
            // destroy script and the entire object
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // prevent destruction
            DontDestroyOnLoad(this);
        }
    }

    void Start()
    {
        AudioManager.Instance.MuteSfx(!IsSFXOn());
    }

    public int CurrentLevelId()
    {
        return PlayerPrefs.GetInt("currentLevel", 0);
    }

    public bool IsLastLevel(int levelId)
    {
        return levelId == (availableLevelsAmount - 1);
    }

    public bool IsLevelUnlocked(int levelId)
    {
        return levelId <= CurrentLevelId();
    }

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
        if (IsLastLevel(playedLevelId)) {
            return playedLevelId;
        }

        // get last unlocked lvl id
        int lastUnlockedLevelId = CurrentLevelId();
        // if we player the last unlocked level
        // then unlock the next one
        if (playedLevelId == lastUnlockedLevelId)
        {
            int nextLevelId = lastUnlockedLevelId + 1;
            PlayerPrefs.SetInt("currentLevel", nextLevelId);
            PlayerPrefs.Save();
            return nextLevelId;
        }

        // replaying, simply return the next level id
        return playedLevelId + 1;
    }

    public int GetLevelStars(int levelId)
    {
        return PlayerPrefs.GetInt("stars" + levelId, 0);
    }

    public void SetLevelStars(int levelId, int stars)
    {
        // update level stars, but only if earned more than before
        int previouslyEarnedAmount = PlayerPrefs.GetInt("stars" + levelId, 0);
        if (stars > previouslyEarnedAmount)
        {
            PlayerPrefs.SetInt("stars" + levelId, stars);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Sets achieved score if it is higher than previously achieved.
    /// </summary>
    /// <param name="levelId">Level id</param>
    /// <param name="score">New score</param>
    /// <returns>True if new score is new highscore, otherwise False.</returns>
    public bool SetLevelScoreChecked(int levelId, int score)
    {
        // update level stars, but only if earned more than before
        int previouslyEarnedAmount = PlayerPrefs.GetInt("score" + levelId, 0);
        if (score > previouslyEarnedAmount)
        {
            PlayerPrefs.SetInt("score" + levelId, score);
            PlayerPrefs.Save();
            return true;
        }

        return false;
    }


    public bool IsMusicOn()
    {
        return PlayerPrefs.GetInt("musicOn", 1) == 1;
    }

    public void ToggleMusic()
    {
        int value = PlayerPrefs.GetInt("musicOn", 1)  ^ 1;
        PlayerPrefs.SetInt("musicOn", value);
        PlayerPrefs.Save();

        if (value == 0) {
            AudioManager.Instance.StopMusic();
        } else {
            AudioManager.Instance.PlayMusic();
        }
    }

    public bool IsSFXOn()
    {
        return PlayerPrefs.GetInt("sfxOn", 1) == 1;
    }

    public void ToggleSFX()
    {
        int value = PlayerPrefs.GetInt("sfxOn", 1)  ^ 1;
        PlayerPrefs.SetInt("sfxOn", value);
        PlayerPrefs.Save();

        AudioManager.Instance.MuteSfx(value == 0);
    }
}
