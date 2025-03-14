using System.Collections;
using Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayTests
{
    public class MultiplierHandlerTests
    {
        private GameObject _tmpObject;
        private MultiplierHandler _multiplier;

        private float _timerMaxValue;
        private float _timerUpdateValue;
        private int _multiplierUpdateValue;

        private readonly Multiplier _config = new()
        {
            decayTime = 2f,
            decayRate = 0.5f,
            max = 5
        };

        [SetUp]
        public void Setup()
        {
            _tmpObject = new GameObject();
            _multiplier = _tmpObject.AddComponent<MultiplierHandler>();

            MultiplierHandler.OnMultiplierTimerUpdate += (currentTime, maxTime) =>
            {
                _timerUpdateValue = currentTime;
                _timerMaxValue = maxTime;
            };
            MultiplierHandler.OnMultiplierUpdate += (newMultiplier) => _multiplierUpdateValue = newMultiplier;

            _multiplier.Init(_config);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_tmpObject);
        }

        [Test]
        public void Multiplier_StartsAtOne()
        {
            Assert.AreEqual(1, _multiplier.Multiplier, "Multiplier shall start at 1");
        }

        [Test]
        public void Multiplier_Increment_IncreasesByOne()
        {
            _multiplier.Increment();
            Assert.AreEqual(2, _multiplier.Multiplier, "Increment() shall increase the multiplier by 1");
        }

        [Test]
        public void Multiplier_Increment_DoesNotExceedMax()
        {
            for (int i = 0; i < _config.max + 1; i++)
            {
                _multiplier.Increment();
            }

            Assert.AreEqual(_config.max, _multiplier.Multiplier,
                "Multiplier shall not exceed max config value " + _config.max);
        }

        [Test]
        public void Multiplier_Decrement_DecreasesMultiplier()
        {
            _multiplier.Increment(); // 2
            _multiplier.Increment(); // 3
            _multiplier.Decrement(); // 2

            Assert.AreEqual(2, _multiplier.Multiplier, "Decrement() shall decrease the multiplier");
        }

        [Test]
        public void Multiplier_Decrement_DoesNotGoBelowOne()
        {
            _multiplier.Decrement();
            Assert.AreEqual(1, _multiplier.Multiplier, "Multiplier shall not go below 1");
        }

        [Test]
        public void Timer_ResetsOnIncrement()
        {
            _multiplier.Increment();
            Assert.AreEqual(_config.decayTime, _timerUpdateValue, "Timer should reset when multiplier is increased.");
        }

        [UnityTest]
        public IEnumerator Multiplier_Timer_DecreasesCorrectly()
        {
            _multiplier.Increment();
            float initialTimer = _timerUpdateValue;

            yield return null;

            Assert.Less(_timerUpdateValue, initialTimer, "Timer shall decrease over time");
        }

        [UnityTest]
        public IEnumerator Multiplier_Timer_DecayOverTime()
        {
            _multiplier.Increment();
            Assert.AreEqual(2, _multiplier.Multiplier, "Multiplier shall increase by 1");
            Assert.AreEqual(2, _multiplierUpdateValue, "Multiplier shall produce event with updated multiplier value");

            // requires a bit more time than config.decayTime
            yield return new WaitForSeconds(5f);

            Assert.AreEqual(1, _multiplier.Multiplier, "Multiplier shall decrease over full decay time");
            Assert.AreEqual(1, _multiplierUpdateValue, "Multiplier shall produce event with updated multiplier value");
        }
    }
}