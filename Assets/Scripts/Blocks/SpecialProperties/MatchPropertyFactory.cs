using System.IO;
using UnityEngine;

namespace Blocks.SpecialProperties
{
    /// <summary>
    /// Factory to instantiate special properties of a block/crystal.
    /// </summary>
    public class MatchPropertyFactory : MonoBehaviour
    {
        public static MatchPropertyFactory Instance { get; private set; }

        [SerializeField] private MatchPropertyFactoryConfig config;

        /// <summary>
        /// Setups the factory.
        /// </summary>
        /// <exception cref="InvalidDataException">If config is not set.</exception>
        private void Awake()
        {
            // prevent multiple instances
            if (Instance != null && Instance != this)
            {
                Destroy(this);
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
        /// Instantiates <see cref="ChainedProperty"/>.
        /// </summary>
        public IMatchProperty NewChainedProperty()
        {
            return new ChainedProperty(config.chainedPropertyConfig);
        }
    }
}