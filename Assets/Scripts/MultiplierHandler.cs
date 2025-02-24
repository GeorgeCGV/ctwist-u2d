using System;
using UnityEngine;

public class MultiplierHandler : MonoBehaviour
{
    private static int s_minMultiplierValue = 1;

    /// <summary>
    /// Invoked every time when current multiplier timer value changes.
    /// <float, float> - <current_time, start_time/max_time>
    /// </summary>
    public static event Action<float, float> OnMultiplierTimerUpdate;
    public static event Action<int> OnMultiplierUpdate;

    [SerializeField]
    private int scoreMultiplier = s_minMultiplierValue;

    [SerializeField, Min(0.1f)]
    private float scoreMultiplierDecayTime = 1f;

    private float scoreMultiplierDecreaseTimer = 0;

    [SerializeField, Min(0.01f)]
    private float scoreMultiplierDecayRate = 0.25f;

    // it is expected the value to be at least 1
    // ideally, the maximum shall be equal to
    // SfxBlocksClear length
     [SerializeField, Min(1)]
    private int scoreMultiplierMax = 6;

    public int Multiplier
    {
        get { return scoreMultiplier; }
        private set
        {
            if (scoreMultiplier == value) {
                return;
            }

            scoreMultiplier = value;
            OnMultiplierUpdate?.Invoke(scoreMultiplier);
        }
    }

    public void Increment()
    {
        if (scoreMultiplier < scoreMultiplierMax) {
            Multiplier = scoreMultiplier + 1; // avoid extra call in case of ++
            // reset timer
            scoreMultiplierDecreaseTimer = scoreMultiplierDecayTime;
            // notify
            OnMultiplierTimerUpdate?.Invoke(scoreMultiplierDecreaseTimer, scoreMultiplierDecayTime);
        }
    }

    public void Decrement()
    {
        if (scoreMultiplier > s_minMultiplierValue) {
            Multiplier = scoreMultiplier - 1; // avoid extra call in case of --
            // reset timer
            scoreMultiplierDecreaseTimer = scoreMultiplierDecayTime;
            // notify
            OnMultiplierTimerUpdate?.Invoke(scoreMultiplierDecreaseTimer, scoreMultiplierDecayTime);
        }
    }

    public void Init(Data.Multiplier config)
    {
        scoreMultiplierDecayTime = Mathf.Max(config.decayTime, scoreMultiplierDecayTime);
        scoreMultiplierDecayRate = Mathf.Max(config.decayRate, scoreMultiplierDecayRate);
        // some levels might override the multiplier
        scoreMultiplierMax = config.max > s_minMultiplierValue ? config.max : scoreMultiplierMax;
    }

    void Start()
    {
        OnMultiplierTimerUpdate?.Invoke(scoreMultiplierDecreaseTimer, scoreMultiplierDecayTime);
        OnMultiplierUpdate?.Invoke(scoreMultiplier);
    }

    void Update()
    {
        if (scoreMultiplierDecreaseTimer <= 0)
        {
            // nothing to run as time is already <= 0
            return;
        }

        scoreMultiplierDecreaseTimer = Mathf.Max(scoreMultiplierDecreaseTimer - scoreMultiplierDecayRate * Time.deltaTime, 0);
        if (scoreMultiplierDecreaseTimer <= 0)
        {
            // try to decrement if reached 0
            Decrement();
        }

        OnMultiplierTimerUpdate?.Invoke(scoreMultiplierDecreaseTimer, scoreMultiplierDecayTime);
    }
}