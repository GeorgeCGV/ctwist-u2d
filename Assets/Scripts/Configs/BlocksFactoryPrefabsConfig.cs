using UnityEngine;

[CreateAssetMenu(fileName = "BlocksFactoryPrefabsConfig", menuName = "Factory/BlocksFactoryPrefabsConfig")]
public class BlocksFactoryPrefabsConfig : ScriptableObject
{
    [SerializeField]
    public GameObject ColorBlockPrefab;
}
