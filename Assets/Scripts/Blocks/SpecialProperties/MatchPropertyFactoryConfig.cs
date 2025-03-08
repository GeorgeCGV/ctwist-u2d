using UnityEngine;
using UnityEngine.Serialization;

namespace Blocks.SpecialProperties
{
    /// <summary>
    /// <see cref="MatchPropertyFactory"/> configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "MatchPropertyFactoryAssetsConfig",
                     menuName = "Factory/MatchPropertyFactoryAssetsConfig")]
    public class MatchPropertyFactoryConfig : ScriptableObject
    {
        [FormerlySerializedAs("ChainedPropertyConfig")] [SerializeField]
        public ChainedPropertyConfig chainedPropertyConfig;
    }
}