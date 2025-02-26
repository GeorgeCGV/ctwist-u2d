using System;
using UnityEngine;

/// <summary>
/// Factory to instantiate different game blocks/crystals.
/// </summary>
public class BlocksFactory : MonoBehaviour
{
    public static BlocksFactory Instance { get; private set; }

    [SerializeField]
    private BlocksFactoryPrefabsConfig config;

    /// <summary>
    /// Setups the factory.
    /// /// </summary>
    void Awake()
    {
        // prevent mutliple instances
        if (Instance != null && Instance != this)
        {
            // safe to destroy the whole object
            Destroy(gameObject);
        }
        else
        {
            // config that provides prefabs is required
            if (config == null)
            {
                throw new InvalidOperationException("Prefabs config is not set");
            }
            Instance = this;
        }
    }

    /// <summary>
    /// Instantiates ColorBlock of provided EBlockColor.
    /// </summary>
    /// <param name="color">ColorBlock.EBlockColor.</param>
    /// <returns>GameObject with ColorBlock script.</returns>
    public GameObject NewColorBlock(ColorBlock.EBlockColor color)
    {
        GameObject block = Instantiate(config.ColorBlockPrefab);

        block.name = "CB:" + color.ToString() + "#" + UnityEngine.Random.Range(0, 10000);
        block.GetComponent<ColorBlock>().ColorType = color;

        return block;
    }
}
