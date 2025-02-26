using System;

public class ProgressiveValueTimer
{
    /// <summary>
    /// Behaviour interface for the ProgressiveValueTimer.
    /// Dictates how the value is changed and when the timer is done.
    /// </summary>
    public interface IValueChangeOperation
    {
        /// <summary>
        /// Computes new value from current value and rate.
        /// </summary>
        /// <param name="currentValue">Current value.</param>
        /// <param name="rate">Change rate, where rate is [0;1].</param>
        /// <returns>New value</returns>
        float ComputeChange(float currentValue, float rate);

        /// <summary>
        /// Checks if further timer operation is required.
        /// </summary>
        /// <param name="currentValue">Current value.</param>
        /// <param name="endValue">Desired end value.</param>
        /// <returns>True when current value has reached the end value.</returns>
        bool IsEndReached(float currentValue, float endValue);
    }

    public readonly struct IncrementalOperation : IValueChangeOperation
    {
        public float ComputeChange(float currentValue, float rate)
        {
            return currentValue * (1.0f + rate);
        }

        public bool IsEndReached(float currentValue, float endValue)
        {
            return currentValue >= endValue;
        }
    }

    public readonly struct DecrementalOperation : IValueChangeOperation
    {
        public float ComputeChange(float currentValue, float rate)
        {
            return currentValue * (1.0f - rate);
        }

        public bool IsEndReached(float currentValue, float endValue)
        {
            return currentValue <= endValue;
        }
    }

    private readonly float rate;
    private float currentValue;
    private float endValue;

    private float elapsedTime;
    private readonly float intervalInSeconds;
    private bool isComplete;

    private readonly Action<float> onValueChange;
    private readonly IValueChangeOperation valueBehaviour;

    public ProgressiveValueTimer(float initial, float to, float by, float seconds,
                                 Action<float> callback, IValueChangeOperation behaviour)
    {
        onValueChange = callback ?? throw new ArgumentNullException(nameof(callback), "Callback cannot be null");
        valueBehaviour = behaviour ?? throw new ArgumentNullException(nameof(behaviour), "Behaviour cannot be null");
        rate = by;
        intervalInSeconds = seconds;
        Reset(initial, to);
    }

    public void Update(float deltaTime)
    {
        if (isComplete)
        {
            return;
        }

        elapsedTime += deltaTime;
        if (elapsedTime >= intervalInSeconds)
        {
            elapsedTime = 0;

            currentValue = valueBehaviour.ComputeChange(currentValue, rate);

            if (valueBehaviour.IsEndReached(currentValue, endValue))
            {
                currentValue = endValue;
                isComplete = true;
            }

            onValueChange?.Invoke(currentValue);
        }

    }

    public void Reset(float initial, float to)
    {
        currentValue = initial;
        endValue = to;
        elapsedTime = 0;
        isComplete = false;
    }
}