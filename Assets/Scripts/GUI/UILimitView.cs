using Data;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Handles limit panel view aspects.
/// Supports the following limits: none, time, spawn amount.
/// </summary>
public class UILimitView : MonoBehaviour
{

    private static readonly string NO_LIMIT_LBL_TEXT = "∞";

    [SerializeField]
    private TextMeshProUGUI label;

    [SerializeField]
    private GameObject spawnsIcon;

    [SerializeField]
    private GameObject timeIcon;

    /// <summary>
    /// Acquire required references.
    /// </summary>
    void Awake()
    {
        Assert.IsNotNull(label);
        Assert.IsNotNull(spawnsIcon);
        Assert.IsNotNull(timeIcon);

        // hide all icons by default and set lbl to no limit txt
        spawnsIcon.SetActive(false);
        timeIcon.SetActive(false);
        label.text = NO_LIMIT_LBL_TEXT;
    }

    void OnDestroy()
    {
        LevelManager.OnTimeLeftUpdate -= HandleTimeLeftUpdate;
        LevelManager.OnSpawnsLeftUpdate -= HandleSpawnsLeftUpdate;
    }

    private void HandleSpawnsLeftUpdate(int spawnedAmount, int totalSpawns)
    {
        label.text = (totalSpawns - spawnedAmount).ToString();
    }

    private void HandleTimeLeftUpdate(int min, int sec)
    {
        label.text = $"{min:D2}:{sec:D2}";
    }

    /// <summary>
    /// Initializes the view and setups required callbacks.
    /// </summary>
    /// <param name="data">Level limit data</param>
    public void Init(Limit data)
    {
        ELimitVariant limit = data.LevelLimit();

        if (limit == ELimitVariant.TIME_LIMIT)
        {
            int seconds = Mathf.FloorToInt(data.time % 60);
            int minutes = Mathf.FloorToInt(data.time / 60);

            HandleTimeLeftUpdate(minutes, seconds);

            timeIcon.SetActive(true);
            LevelManager.OnTimeLeftUpdate += HandleTimeLeftUpdate;
        }
        else if (limit == ELimitVariant.SPAWN_LIMIT)
        {
            HandleSpawnsLeftUpdate(0, data.spawns);

            spawnsIcon.SetActive(true);
            LevelManager.OnSpawnsLeftUpdate += HandleSpawnsLeftUpdate;
        }
    }
}