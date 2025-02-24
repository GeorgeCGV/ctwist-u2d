using System;

namespace Data
{
    [Serializable]
    public class LevelData
    {
        public int seed;
        public int id;
        public int obstructionIdx;
        public string[] colorsInLevel;
        public int startBlocksNum;
        public int goalScore;
        /// <summary>
        /// Level time limit in seconds.
        /// </summary>
        public int limitTime;
        /// <summary>
        /// Level spawn moves limit.
        /// </summary>
        public int limitMove;
        public Spawn spawn;
        public Block block;
        public Multiplier multiplier;
        public int[] starRewards;
    }

    [Serializable]
    public struct Multiplier
    {
        public float decayTime;
        public float decayRate;
        public int max;
    }

    [Serializable]
    public struct Spawn
    {
        public float timeMin;
        public float timeMax;
        public int timeDecreasePerTimeLimitPercent;
        public float timeDecreasePerTimeSeconds;
        public int timeDecreaseByTimePercent;
    }

    [Serializable]
    public struct Block
    {
        public int speedMin;
        public int speedMax;
        public int speedIncreasePerTimeLimitPercent;
        public float speedIncreasePerTimeSeconds;
        public int speedIncreaseBySpeedPercent;
    }

}