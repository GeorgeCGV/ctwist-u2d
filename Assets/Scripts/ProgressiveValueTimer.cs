using System;

/// <summary>
/// Progressive value timer decreases or increases the value from
/// start value to end value at specified time interval.
/// </summary>
/// <remarks>
/// The rate is computed based on the current value.
/// </remarks>
/// <see cref="IValueChangeOperation"/>
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

    /// <summary>
    /// Increments start value until it reaches the end value.
    /// </summary>
    public struct IncrementalOperation : IValueChangeOperation
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

    /// <summary>
    /// Decrements start value until it reaches end value.
    /// </summary>
    public struct DecrementalOperation : IValueChangeOperation
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

    private readonly float _rate;
    private float _currentValue;
    private float _endValue;

    private float _elapsedTime;
    private readonly float _intervalInSeconds;
    private bool _isComplete;

    private readonly Action<float> _onValueChange;
    private readonly IValueChangeOperation _valueBehaviour;

    public ProgressiveValueTimer(float initial, float to, float by, float seconds,
        Action<float> callback, IValueChangeOperation behaviour)
    {
        _onValueChange = callback ?? throw new ArgumentNullException(nameof(callback), "Callback cannot be null");
        _valueBehaviour = behaviour ?? throw new ArgumentNullException(nameof(behaviour), "Behaviour cannot be null");
        _rate = by;
        _intervalInSeconds = seconds;
        Reset(initial, to);
    }

    public void Update(float deltaTime)
    {
        if (_isComplete)
        {
            return;
        }

        _elapsedTime += deltaTime;
        if (_elapsedTime >= _intervalInSeconds)
        {
            _elapsedTime = 0;

            _currentValue = _valueBehaviour.ComputeChange(_currentValue, _rate);

            if (_valueBehaviour.IsEndReached(_currentValue, _endValue))
            {
                _currentValue = _endValue;
                _isComplete = true;
            }

            _onValueChange?.Invoke(_currentValue);
        }
    }

    public void Reset(float initial, float to)
    {
        _currentValue = initial;
        _endValue = to;
        _elapsedTime = 0;
        _isComplete = false;
    }
}