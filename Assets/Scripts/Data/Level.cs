using System;

namespace Data
{
    [Serializable]
    public class LevelData
    {
        public int id;
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
        public int[] starRewards;
    }

    [Serializable]
    public class Spawn
    {
        public float timeMin;
        public float timeMax;
        public int timeDecreasePerTimePercent;
        public int timeDecreaseByTimePercent;
    }

    [Serializable]
    public class Block
    {
        public int speedMin;
        public int speedMax;
        public int speedIncreasePerTimePercent;
        public int speedIncreaseBySpeedPercent;
    }

}