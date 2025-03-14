using System;
using System.IO;
using static Model.BlockType;

namespace Model
{
    /// <summary>
    /// Holds level information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deserialized from JSON.
    /// </para>
    /// <para>
    /// Doesn't require any external JSON library.
    /// However, as Unity's JsonUtility doesn't handle
    /// enum arrays we do it manually in ParseInternal.
    /// </para>
    /// </remarks>
    [Serializable]
    public class LevelData
    {
        /// <summary>
        /// Seed to be used for the RNG during start blocks creation.
        /// </summary>
        public int seed;
        /// <summary>
        /// Level ID.
        /// </summary>
        /// <remarks>
        /// Inferred by the level loader.
        /// </remarks>
        [NonSerialized]
        public int ID;
        /// <summary>
        /// Level can have an obstruction tilemap (i.e. ice).
        /// </summary>
        /// <remarks>
        /// Provide value >= 0 to instantiate one of the available
        /// obstructions. The actual tilemap list is created in the
        /// Scene via ObstructionPrefabsConfig.
        /// </remarks>
        public int obstructionIdx;
        /// <summary>
        /// Available block colors in the level.
        /// Contains EBlockType color subset values as string.
        /// </summary>
        public string[] blocksInLevel;
        /// <summary>
        /// Parsed colorsInLevel.
        /// </summary>
        /// <remarks>
        /// The ParseInternal must be called before
        /// accessing the field.
        /// </remarks>
        [NonSerialized]
        public EBlockType[] ParsedBlocksInLevel;
        /// <summary>
        /// Amount of blocks the level begins with.
        /// </summary>
        public int startBlocksNum;
        /// <summary>
        /// Chance in percent that a start block might have chained property. 
        /// </summary>
        public int startBlocksChainedBlockChancePercent;
        /// <summary>
        /// Goal a player must achieve to complete the level.
        /// </summary>
        public Goal goal;
        /// <summary>
        /// Level limit (if any).
        /// </summary>
        public Limit limit;
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
        /// </summary>
        /// <remarks>
        /// Currently uses plain scores.
        /// ; each element contains required score value
        /// to grant a star.
        /// </remarks>
        public int[] starRewards;

        /// <summary>
        /// Parses internal structures.
        /// </summary>
        public void ParseInternal()
        {
            ParsedBlocksInLevel = new EBlockType[blocksInLevel.Length];
            for (int i = 0; i < blocksInLevel.Length; i++)
            {
                ParsedBlocksInLevel[i] = EBlockTypeFromString(blocksInLevel[i]);
            }

            goal.ParseInternal();
        }
    }

    /// <summary>
    /// Level's goal type.
    /// </summary>
    public enum EGoalVariant
    {
        Score, Blocks
    }

    [Serializable]
    public class BlocksGoal
    {
        /// <summary>
        /// Block type.
        /// Contains EBlockType.
        /// </summary>
        public string type;
        /// <summary>
        /// Parsed type.
        /// </summary>
        /// <remarks>
        /// The ParseInternal must be called before
        /// accessing the field.
        /// </remarks>
        [NonSerialized]
        public EBlockType ParsedType;
        /// <summary>
        /// Required amount.
        /// </summary>
        public int amount;

        /// <summary>
        /// Parses internal structures.
        /// </summary>
        public void ParseInternal()
        {
            ParsedType = EBlockTypeFromString(type);
        }
    }

    [Serializable]
    public class Goal
    {
        /// <summary>
        /// Score goal/target.
        /// </summary>
        public int score;
        /// <summary>
        /// Blocks goals/targets.
        /// Maximum 3 entries.
        /// </summary>
        public BlocksGoal[] blocks;

        /// <summary>
        /// Goal variant.
        /// </summary>
        /// <returns>EGoalVariant</returns>
        public EGoalVariant Variant()
        {
            if (score > 0)
            {
                return EGoalVariant.Score;
            }

            if ((blocks != null) && (blocks.Length < 4))
            {
                return EGoalVariant.Blocks;
            }

            throw new InvalidDataException("Level goal can be either score or blocks (max 3 entries) based");
        }

        /// <summary>
        /// Parses internal structures.
        /// </summary>
        public void ParseInternal()
        {
            if (Variant() == EGoalVariant.Blocks)
            {
                foreach (BlocksGoal goal in blocks)
                {
                    goal.ParseInternal();
                }
            }
        }
    }

    /// <summary>
    /// What limit the level has.
    /// The level can have up to one limit.
    /// </summary>
    public enum ELimitVariant
    {
        NoLimit,
        TimeLimit,
        SpawnLimit
    }

    [Serializable]
    public class Limit
    {
        /// <summary>
        /// Time limit in seconds.
        /// </summary>
        public int time;
        /// <summary>
        /// Spawns amount limit.
        /// </summary>
        public int spawns;

        /// <summary>
        /// Limit variant.
        /// </summary>
        /// <returns>ELevelLimit</returns>
        public ELimitVariant Variant()
        {
            if (time > 0)
            {
                return ELimitVariant.TimeLimit;
            }

            if (spawns > 0)
            {
                return ELimitVariant.SpawnLimit;
            }

            return ELimitVariant.NoLimit;
        }
    }

    [Serializable]
    public class Multiplier
    {
        public float decayTime;
        public float decayRate;
        public int max;
    }

    [Serializable]
    public class Spawn
    {
        public float batchChance;
        public int batchMin;
        public int batchMax;
        public float timeMin;
        public float timeMax;
        public int timeDecreasePerTimeLimitPercent;
        public float timeDecreasePerTimeSeconds;
        public int timeDecreaseByTimePercent;
        public int stoneBlockChancePercent;
        public int chainedBlockChancePercent;
        public int glowBlockChancePercent;
    }

    [Serializable]
    public class Block
    {
        public int speedMin;
        public int speedMax;
        public int speedIncreasePerTimeLimitPercent;
        public float speedIncreasePerTimeSeconds;
        public int speedIncreaseBySpeedPercent;
    }

}