using System;
using UnityEngine;

public class BlocksFactory : MonoBehaviour
{
    public static BlocksFactory Instance { get; private set; }

    [SerializeField]
    private BlocksFactoryPrefabsConfig config;

    void Awake()
    {
        // prevent mutliple instances
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            if (config == null) {
                throw new InvalidOperationException("Prefabs config is not set");
            }
            Instance = this;
            // it is safe to keep the factory around
            DontDestroyOnLoad(gameObject);
        }
    }

    public GameObject NewColorBlock(ColorBlock.EBlockColor color)
    {
        GameObject block = Instantiate(config.ColorBlockPrefab);

        block.name = "CB:" + color.ToString() + "#" + UnityEngine.Random.Range(0, 10000);
        block.GetComponent<ColorBlock>().ColorType = color;

        return block;
    }
}
