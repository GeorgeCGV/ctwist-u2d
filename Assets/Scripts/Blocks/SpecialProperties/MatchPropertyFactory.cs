using System;
using System.IO;
using UnityEngine;

namespace Blocks.SpecialProperties
{
    /// <summary>
    /// Factory to instantiate special properties of a block/crystal.
    /// </summary>
    public class MatchPropertyFactory : MonoBehaviour
    {
        [Serializable]
        public enum EMatchProperty
        {
            /// <summary>
            /// <see cref="ChainedProperty"/>.
            /// </summary>
            ChainProperty,
            /// <summary>
            /// <see cref="GlowProperty"/>.
            /// </summary>
            GlowProperty
        }
        
        public static MatchPropertyFactory Instance { get; private set; }

        [SerializeField]
        private MatchPropertyFactoryConfig config;

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
        /// Instantiates <see cref="IMatchProperty"/>.
        /// </summary>
        public IMatchProperty NewProperty(EMatchProperty property)
        {
            switch (property)
            {
                case EMatchProperty.ChainProperty:
                    return new ChainedProperty(config.chainedPropertyConfig);
                case EMatchProperty.GlowProperty:
                    return new GlowProperty(config.glowPropertyConfig);
                default:
                    throw new ArgumentOutOfRangeException(nameof(property), property, null);
            }
        }
    }
}