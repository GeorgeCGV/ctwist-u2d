using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MultiplierHandlerTests
{
    private GameObject tmpObject;
    private MultiplierHandler multiplier;

    private float timerMaxValue;
    private float timerUpdateValue;
    private int multiplierUpdateValue;

    private Data.Multiplier config = new Data.Multiplier
    {
        decayTime = 2f,
        decayRate = 0.5f,
        max = 5
    };

    [SetUp]
    public void Setup()
    {
        tmpObject = new GameObject();
        multiplier = tmpObject.AddComponent<MultiplierHandler>();

        MultiplierHandler.OnMultiplierTimerUpdate += (currentTime, maxTime) => { timerUpdateValue = currentTime; timerMaxValue = maxTime; };
        MultiplierHandler.OnMultiplierUpdate += (newMultiplier) => multiplierUpdateValue = newMultiplier;

        multiplier.Init(config);
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(tmpObject);
    }

    [Test]
    public void Multiplier_StartsAtOne()
    {
        Assert.AreEqual(1, multiplier.Multiplier, "Multiplier shall start at 1");
    }

    [Test]
    public void Multiplier_Increment_IncreasesByOne()
    {
        multiplier.Increment();
        Assert.AreEqual(2, multiplier.Multiplier, "Increment() shall increase the multiplier by 1");
    }

    [Test]
    public void Multiplier_Increment_DoesNotExceedMax()
    {
        for (int i = 0; i < config.max + 1; i++)
        {
            multiplier.Increment();
        }

        Assert.AreEqual(config.max, multiplier.Multiplier, "Multiplier shall not exceed max config value " + config.max);
    }

    [Test]
    public void Multiplier_Decrement_DecreasesMultiplier()
    {
        multiplier.Increment(); // 2
        multiplier.Increment(); // 3
        multiplier.Decrement(); // 2

        Assert.AreEqual(2, multiplier.Multiplier, "Decrement() shall decrease the multiplier");
    }

    [Test]
    public void Multiplier_Decrement_DoesNotGoBelowOne()
    {
        multiplier.Decrement();
        Assert.AreEqual(1, multiplier.Multiplier, "Multiplier shall not go below 1");
    }

    [Test]
    public void Timer_ResetsOnIncrement()
    {
        multiplier.Increment();
        Assert.AreEqual(config.decayTime, timerUpdateValue, "Timer should reset when multiplier is increased.");
    }

    [UnityTest]
    public IEnumerator Multiplier_Timer_DecreasesCorrectly()
    {
        multiplier.Increment();
        float initialTimer = timerUpdateValue;

        yield return null;

        Assert.Less(timerUpdateValue, initialTimer, "Timer shall decrease over time");
    }

    [UnityTest]
    public IEnumerator Multiplier_Timer_DecayOverTime()
    {
        multiplier.Increment();
        Assert.AreEqual(2, multiplier.Multiplier, "Multiplier shall increase by 1");
        Assert.AreEqual(2, multiplierUpdateValue, "Multiplier shall produce event with updated multiplier value");

        // requires a bit more time than config.decayTime
        yield return new WaitForSeconds(5f);

        Assert.AreEqual(1, multiplier.Multiplier, "Multiplier shall decrease over full decay time");
        Assert.AreEqual(1, multiplierUpdateValue, "Multiplier shall produce event with updated multiplier value");
    }
}
