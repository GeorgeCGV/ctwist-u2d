using UnityEngine;

/// <summary>
/// Block Prefabs configuration for the BlocksFactory.
/// </summary>
[CreateAssetMenu(fileName = "BlocksFactoryPrefabsConfig", menuName = "Factory/BlocksFactoryPrefabsConfig")]
public class BlocksFactoryPrefabsConfig : ScriptableObject
{
    [SerializeField]
    public GameObject ColorBlockPrefab;
}
