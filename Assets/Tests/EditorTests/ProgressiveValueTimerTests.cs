using NUnit.Framework;

namespace Tests.EditorTests
{
    public class ProgressiveValueTimerTests
    {
        private float _callbackValue;

        [SetUp]
        public void Setup()
        {
            _callbackValue = 0f;
        }

        [Test]
        public void ProgressiveValueTimer_IncrementalOperation_UpdatesValueCorrectly()
        {
            // Arrange
            float initial = 10f;
            float end = 20f;
            float rate = 0.1f;
            float interval = 1f;

            ProgressiveValueTimer timer = new ProgressiveValueTimer(
                initial, end, rate, interval,
                value => _callbackValue = value,
                new ProgressiveValueTimer.IncrementalOperation()
            );

            // Act - Simulate multiple updates
            for (int i = 0; i < 10; i++)
            {
                timer.Update(interval); // Simulate passing time
            }

            // Assert
            Assert.AreEqual(end, _callbackValue, 0.01f, "Incremental operation did not reach the expected value.");
        }

        [Test]
        public void DecrementalOperation_UpdatesValueCorrectly()
        {
            // Arrange
            float initial = 20f;
            float end = 10f;
            float rate = 0.1f;
            float interval = 1f;

            ProgressiveValueTimer timer = new ProgressiveValueTimer(
                initial, end, rate, interval,
                value => _callbackValue = value,
                new ProgressiveValueTimer.DecrementalOperation()
            );

            // Act - Simulate multiple updates
            for (int i = 0; i < 10; i++)
            {
                timer.Update(interval);
            }

            // Assert
            Assert.AreEqual(end, _callbackValue, 0.01f, "Decremental operation did not reach the expected value.");
        }

        [Test]
        public void Timer_DoesNotUpdate_WhenComplete()
        {
            // Arrange
            float initial = 10f;
            float end = 20f;
            float rate = 0.5f;
            float interval = 1f;

            ProgressiveValueTimer timer = new ProgressiveValueTimer(
                initial, end, rate, interval,
                value => _callbackValue = value,
                new ProgressiveValueTimer.IncrementalOperation()
            );

            // Act - Fully complete the timer
            for (int i = 0; i < 10; i++)
            {
                timer.Update(interval);
            }

            float finalValue = _callbackValue;

            // Another update should NOT change value
            timer.Update(interval);

            // Assert
            Assert.AreEqual(finalValue, _callbackValue, "Timer should not update after reaching the end.");
        }

        [Test]
        public void Timer_ResetsCorrectly()
        {
            // Arrange
            float initial = 5f;
            float end = 50f;
            float rate = 0.2f;
            float interval = 1f;

            ProgressiveValueTimer timer = new ProgressiveValueTimer(
                initial, end, rate, interval,
                value => _callbackValue = value,
                new ProgressiveValueTimer.IncrementalOperation()
            );

            // Simulate updates
            for (int i = 0; i < 5; i++)
            {
                timer.Update(interval);
            }

            // Act - Reset the timer
            timer.Reset(initial, end);
            timer.Update(interval); // Apply one update to ensure it starts over

            // Assert
            Assert.AreEqual(initial * 1.2f, _callbackValue, 0.01f, "Reset did not correctly apply the initial value.");
        }
    }
}