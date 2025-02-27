using System.Collections.Generic;
using UnityEngine;

namespace Configs
{
    /// <summary>
    /// Obstruction tilemap prefabs configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "ObstructionPrefabsConfig", menuName = "Factory/ObstructionPrefabsConfig")]
    public class ObstructionPrefabsConfig : ScriptableObject
    {
        /// <summary>
        /// Available obstruction tilemap prefabs.
        /// </summary>
        [SerializeField]
        public List<GameObject> obstructionTilemapPrefabs;
    }

}