using UnityEngine;

namespace Blocks.SpecialProperties
{
    /// <summary>
    /// <see cref="MatchPropertyFactory"/> configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "MatchPropertyFactoryAssetsConfig",
                     menuName = "Factory/MatchPropertyFactoryAssetsConfig")]
    public class MatchPropertyFactoryConfig : ScriptableObject
    {
        [SerializeField]
        public ChainedPropertyConfig chainedPropertyConfig;
        
        [SerializeField]
        public GlowPropertyConfig glowPropertyConfig;
    }
}