using System;

namespace Data
{
    /// <summary>
    /// Holds level information.
    /// Deserialized from JSON.
    /// </summary>
    [Serializable]
    public class LevelData
    {
        /// <summary>
        /// Seed to be used for the RNG during start blocks creation.
        /// </summary>
        public int seed;
        /// <summary>
        /// Level ID, infered by the code during level selection.
        /// </summary>
        public int id;
        /// <summary>
        /// Level can have an obstruction tilemap (i.e. ice).
        /// Provide value >= 0 to instantiate one of the available
        /// obstructions. The actual tilemap list is created in the
        /// Scene via ObstructionPrefabsConfig.
        /// </summary>
        public int obstructionIdx;
        /// <summary>
        /// Available block colors in the level.
        /// Contains EBlockColor enum values as strings.
        /// </summary>
        public string[] colorsInLevel;
        /// <summary>
        /// Amount of blocks the level begins with.
        /// </summary>
        public int startBlocksNum;
        /// <summary>
        /// Score goal the player must achieve to complete
        /// the level.
        /// </summary>
        public int goalScore;
        /// <summary>
        /// Level time limit in seconds.
        /// </summary>
        public int limitTime;
        /// <summary>
        /// Level spawn moves limit.
        /// </summary>
        public int limitMove;
        /// <summary>
        /// Spawner related.
        /// </summary>
        public Spawn spawn;
        /// <summary>
        /// Blocks related.
        /// </summary>
        public Block block;
        /// <summary>
        /// Score multiplier.
        /// </summary>
        public Multiplier multiplier;
        /// <summary>
        /// Star reward rules.
        /// Currently uses plain scores.
        /// ; each element contains required score value
        /// to grant a star.
        /// </summary>
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
        public float batchChance;
        public int batchMin;
        public int batchMax;
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