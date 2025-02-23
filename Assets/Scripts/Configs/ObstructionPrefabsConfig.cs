using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ObstructionPrefabsConfig", menuName = "Factory/ObstructionPrefabsConfig")]
public class ObstructionPrefabsConfig : ScriptableObject
{
    [SerializeField]
    public List<GameObject> obstructionTilemapPrefabs;
}
