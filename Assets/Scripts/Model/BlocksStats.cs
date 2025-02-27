using System.Collections.Generic;
using UnityEngine.Assertions;
using static Model.BlockType;

namespace Model
{
    /// <summary>
    /// Stores information about matched and spawned blocks during level play.
    /// </summary>
    /// <remarks>
    /// Such information is required across multiple places.
    /// </remarks>
    public class BlocksStats
    {
        /// <summary>
        /// Tracks matched block counts by <see cref="EBlockType"/>.
        /// </summary>
        public Dictionary<EBlockType, int> Matched { get; private set; }

        /// <summary>
        /// Tracks spawned block counts by <see cref="EBlockType"/>.
        /// </summary>
        public Dictionary<EBlockType, int> TotalSpawned { get; private set; }

        /// <summary>
        /// The sum of all spawned blocks.
        /// </summary>
        public int TotalSpawnedBlocksAmount { get; private set; }

        /// <summary>
        /// Creates new block stats.
        /// </summary>
        /// <param name="blockTypesLength">Amount of block types in the level.</param>
        public BlocksStats(int blockTypesLength)
        {
            Assert.IsTrue(blockTypesLength > 0, "the amount of blocks shall be > 0");
            Matched = new(blockTypesLength);
            TotalSpawned = new(blockTypesLength);
            TotalSpawnedBlocksAmount = 0;
        }

        /// <summary>
        /// Increases the count of matched blocks for a given type.
        /// </summary>
        /// <param name="type">Block type.</param>
        /// <param name="amount">Amount.</param>
        public void AddMatched(EBlockType type, int amount)
        {
            int currentAmount = Matched.GetValueOrDefault(type, 0);
            Matched[type] = currentAmount + amount;
        }

        /// <summary>
        /// Increases the count of spawned blocks for a given type.
        /// </summary>
        /// <param name="type">Block type.</param>
        /// <param name="amount">Amount.</param>
        public void AddSpawned(EBlockType type, int amount)
        {
            TotalSpawnedBlocksAmount++;
            int currentAmount = TotalSpawned.GetValueOrDefault(type, 0);
            TotalSpawned[type] = currentAmount + amount;
        }
    }
}