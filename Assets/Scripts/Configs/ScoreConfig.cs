using UnityEngine;

[CreateAssetMenu(fileName = "ScoreConfig", menuName = "Config/ScoreConfig")]
public class ScoreConfig : ScriptableObject
{
    [SerializeField]
    public int ScorePerSecond = 5;
    [SerializeField]
    public int ScorePerAttach = 50;
    [SerializeField]
    public int ScorePerMatch3 = 250;
    [SerializeField]
    public int ScorePerMatch4 = 1000;
    [SerializeField]
    public int ScorePerMatchMore = 5000;
    [SerializeField]
    public int ScorePerFloating = 250;
}
