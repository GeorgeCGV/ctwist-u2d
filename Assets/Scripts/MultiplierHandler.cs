using System;
using Model;
using UnityEngine;

/// <summary>
/// Manages score multiplier.
/// </summary>
public class MultiplierHandler : MonoBehaviour
{
    private const int MinMultiplierValue = 1;

    /// <summary>
    /// Invoked every time when current multiplier timer value changes.
    /// </summary>
    /// <param name="currentTimerTime">Current decay timer rime.</param>
    /// <param name="maximumTimerTime">Maximum decay timer time.</param>
    public static event Action<float, float> OnMultiplierTimerUpdate;

    /// <summary>
    /// Invoked when score multiplier value changes.
    /// </summary>
    /// <param name="multiplier">Multiplier value.</param>
    public static event Action<int> OnMultiplierUpdate;

    [SerializeField]
    private int scoreMultiplier = MinMultiplierValue;

    [SerializeField, Min(0.1f)]
    private float scoreMultiplierDecayTime = 1f;

    private float _scoreMultiplierDecayTimer;

    [SerializeField, Min(0.01f)]
    private float scoreMultiplierDecayRate = 0.25f;

    // it is expected the value to be at least 1
    // ideally, the maximum shall be equal to
    // SfxBlocksClear length
    [SerializeField, Min(1)]
    private int scoreMultiplierMax = 6;

    public int Multiplier
    {
        get => scoreMultiplier;
        private set
        {
            if (scoreMultiplier == value)
            {
                return;
            }

            scoreMultiplier = value;
            OnMultiplierUpdate?.Invoke(scoreMultiplier);
        }
    }

    public void Increment()
    {
        if (scoreMultiplier >= scoreMultiplierMax)
        {
            return;
        }

        // avoid extra call in case of ++
        Multiplier = scoreMultiplier + 1;
        // reset timer
        _scoreMultiplierDecayTimer = scoreMultiplierDecayTime;
        // notify
        OnMultiplierTimerUpdate?.Invoke(_scoreMultiplierDecayTimer, scoreMultiplierDecayTime);
    }

    public void Decrement()
    {
        if (scoreMultiplier <= MinMultiplierValue)
        {
            return;
        }

        // avoid extra call in case of --
        Multiplier = scoreMultiplier - 1;
        // reset timer
        _scoreMultiplierDecayTimer = scoreMultiplierDecayTime;
        // notify
        OnMultiplierTimerUpdate?.Invoke(_scoreMultiplierDecayTimer, scoreMultiplierDecayTime);
    }

    public void Init(Multiplier data)
    {
        scoreMultiplierDecayTime = Mathf.Max(data.decayTime, scoreMultiplierDecayTime);
        scoreMultiplierDecayRate = Mathf.Max(data.decayRate, scoreMultiplierDecayRate);
        // some levels might override the multiplier
        scoreMultiplierMax = data.max > MinMultiplierValue ? data.max : scoreMultiplierMax;
    }

    private void Start()
    {
        OnMultiplierTimerUpdate?.Invoke(_scoreMultiplierDecayTimer, scoreMultiplierDecayTime);
        OnMultiplierUpdate?.Invoke(scoreMultiplier);
    }

    private void Update()
    {
        if (_scoreMultiplierDecayTimer <= 0)
        {
            // nothing to run as time is already <= 0
            return;
        }

        _scoreMultiplierDecayTimer =
            Mathf.Max(_scoreMultiplierDecayTimer - scoreMultiplierDecayRate * Time.deltaTime, 0);
        if (_scoreMultiplierDecayTimer <= 0)
        {
            _scoreMultiplierDecayTimer = 0;
            // try to decrement if reached 0
            Decrement();
        }
    
        // notify
        OnMultiplierTimerUpdate?.Invoke(_scoreMultiplierDecayTimer, scoreMultiplierDecayTime);
    }
}