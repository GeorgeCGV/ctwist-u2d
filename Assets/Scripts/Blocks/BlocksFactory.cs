using System;
using System.IO;
using UnityEngine;
using static Model.BlockType;
using Random = UnityEngine.Random;

namespace Blocks
{
    /// <summary>
    /// Factory to instantiate different game blocks/crystals.
    /// </summary>
    public class BlocksFactory : MonoBehaviour
    {
        public static BlocksFactory Instance { get; private set; }

        [SerializeField]
        private BlocksFactoryPrefabsConfig config;

        /// <summary>
        /// Setups the factory.
        /// </summary>
        /// <exception cref="InvalidDataException">If config is not set.</exception>
        private void Awake()
        {
            // prevent multiple instances
            if (Instance != null && Instance != this)
            {
                // safe to destroy the whole object
                Destroy(gameObject);
            }
            else
            {
                // config that provides prefabs is required
                if (config == null)
                {
                    throw new InvalidDataException("Prefabs config is not set");
                }

                Instance = this;
            }
        }

        /// <summary>
        /// Instantiates <see cref="ColorBlock"/>.
        /// </summary>
        /// <param name="type">Subset of <see cref="EBlockType"/>.</param>
        /// <returns><see cref="ColorBlock"/> as <see cref="BasicBlock"/>.</returns>
        public BasicBlock NewColorBlock(EBlockType type)
        {
            if (!EBlockTypeIsColorBlock(type))
            {
                throw new ArgumentException("expected color type");
            }

            ColorBlock block = Instantiate(config.colorBlockPrefab).GetComponent<ColorBlock>();

            block.name = $"{type}#{Random.Range(0, 10000)}";
            block.ColorType = type;

            return block;
        }
        
        /// <summary>
        /// Instantiates <see cref="StoneBlock"/>.
        /// </summary>
        /// <returns><see cref="StoneBlock"/> as <see cref="BasicBlock"/>.</returns>
        public BasicBlock NewStoneBlock()
        {
            StoneBlock block = Instantiate(config.stoneBlockPrefab).GetComponent<StoneBlock>();
            block.name = $"{block.BlockType}#{Random.Range(0, 10000)}";
            return block;
        }
    }
}