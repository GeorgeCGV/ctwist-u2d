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
        /// Instantiates ColorBlock of provided EBlockColor.
        /// </summary>
        /// <param name="type">Subset of EBlockType.</param>
        /// <returns>GameObject with ColorBlock script.</returns>
        public BasicBlock NewColorBlock(EBlockType type)
        {
            if (!EBlockTypeIsColorBlock(type))
            {
                throw new ArgumentException("expected color type");
            }

            ColorBlock colorBlock = Instantiate(config.colorBlockPrefab).GetComponent<ColorBlock>();

            colorBlock.name = $"{type}#{Random.Range(0, 10000)}";
            colorBlock.ColorType = type;

            return colorBlock;
        }
    }
}