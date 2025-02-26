using UnityEngine;

/// <summary>
/// Score configuration that provides values to use
/// when player is awarded.
/// </summary>
[CreateAssetMenu(fileName = "ScoreConfig", menuName = "Config/ScoreConfig")]
public class ScoreConfig : ScriptableObject
{
    [SerializeField]
    public int ScorePerMatch3 = 250;
    [SerializeField]
    public int ScorePerMatch4 = 1000;
    [SerializeField]
    public int ScorePerMatchMore = 5000;
    [SerializeField]
    public int ScorePerFloating = 25;
}
