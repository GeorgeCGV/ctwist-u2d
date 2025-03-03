using UnityEngine;

namespace Blocks
{
    /// <summary>
    /// Block Prefabs configuration for the BlocksFactory.
    /// </summary>
    [CreateAssetMenu(fileName = "BlocksFactoryPrefabsConfig", menuName = "Factory/BlocksFactoryPrefabsConfig")]
    public class BlocksFactoryPrefabsConfig : ScriptableObject
    {
        [SerializeField]
        public GameObject colorBlockPrefab;
        
        [SerializeField]
        public GameObject stoneBlockPrefab;
    }
}