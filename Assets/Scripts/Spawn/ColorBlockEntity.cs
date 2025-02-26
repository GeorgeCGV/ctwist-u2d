using UnityEngine;

/// <summary>
/// ColorBlockEntity provides a way to spawn ColorBlock.
/// </summary>
public class ColorBlockEntity : ISpawnEntity
{
    private readonly ColorBlock.EBlockColor blockColor;
    private readonly Color unityColor;

    private readonly float inSeconds;
    private readonly float speed;

    public ColorBlockEntity(ColorBlock.EBlockColor color, float seconds, float speed)
    {
        blockColor = color;
        unityColor = ColorBlock.UnityColorFromBlockColor(color);
        inSeconds = seconds;
        this.speed = speed;
    }

    public Color BacklightColor()
    {
        // give some contrast when black spawn color is used
        return unityColor == ColorBlock.black ? Color.white : unityColor;
    }

    public Color SpawnColor()
    {
        return unityColor;
    }

    public GameObject Create()
    {
        return BlocksFactory.Instance.NewColorBlock(blockColor);
    }

    public float SpawnInSeconds()
    {
        return inSeconds;
    }

    public float BlockStartSpeed()
    {
        return speed;
    }
}