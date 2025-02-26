using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Obstruction tilemap prefabs configuration.
/// </summary>
[CreateAssetMenu(fileName = "ObstructionPrefabsConfig", menuName = "Factory/ObstructionPrefabsConfig")]
public class ObstructionPrefabsConfig : ScriptableObject
{
    [SerializeField]
    public List<GameObject> obstructionTilemapPrefabs;
}
