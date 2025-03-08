using UnityEngine;

namespace Blocks.SpecialProperties
{
    /// <summary>
    /// <see cref="ChainedProperty"/> configuration.
    /// </summary>
    /// <remarks>
    /// Holds references to required resources.
    /// </remarks>
    [CreateAssetMenu(fileName = "ChainedPropertyConfig", menuName = "BlockProperties/ChainedPropertyConfig")]
    public class ChainedPropertyConfig : ScriptableObject
    {
        /// <summary>
        /// Chain sprite.
        /// </summary>
        [SerializeField]
        public Sprite chainSprite;

        /// <summary>
        /// Sound effect to play on chain destruction.
        /// </summary>
        [SerializeField]
        public AudioClip sfxOnDestroy;

        /// <summary>
        /// Effect to play on chain destruction.
        /// </summary>
        [SerializeField]
        public ParticleSystem efxOnDestroy;
    }
}