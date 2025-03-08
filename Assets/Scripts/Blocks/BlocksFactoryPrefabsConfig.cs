using UnityEngine;

namespace Blocks
{
    /// <summary>
    /// <see cref="BlocksFactory"/> configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "BlocksFactoryPrefabsConfig", menuName = "Factory/BlocksFactoryPrefabsConfig")]
    public class BlocksFactoryPrefabsConfig : ScriptableObject
    {
        /// <summary>
        /// <see cref="ColorBlock"/> prefab.
        /// </summary>
        [SerializeField]
        public GameObject colorBlockPrefab;
        
        /// <summary>
        /// <see cref="StoneBlock"/> prefab.
        /// </summary>
        [SerializeField]
        public GameObject stoneBlockPrefab;
    }
}