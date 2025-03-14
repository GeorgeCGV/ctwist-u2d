using UnityEngine;

namespace Blocks.SpecialProperties
{
    /// <summary>
    /// <see cref="GlowProperty"/> configuration.
    /// </summary>
    /// <remarks>
    /// Holds references to required resources.
    /// </remarks>
    [CreateAssetMenu(fileName = "GlowPropertyConfig", menuName = "BlockProperties/GlowPropertyConfig")]
    public class GlowPropertyConfig : ScriptableObject
    {
        /// <summary>
        /// Glow overlay.
        /// </summary>
        [SerializeField]
        public GameObject glowOverlay;
    }
}