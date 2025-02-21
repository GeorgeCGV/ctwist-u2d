using UnityEngine;

public class LevelSelectionCtrl : MonoBehaviour
{
    [SerializeField]
    private GameObject parentForUiLevelSelectPlayLevel;

    [SerializeField]
    private GameObject uiLevelSelectPlayLevelPrefab;

    [SerializeField]
    private int amountOfLevels;

    public void CreateSelectableLevels()
    {
        for (int i = 0; i < amountOfLevels; i++) {
            GameObject newPlayLevel = Instantiate(uiLevelSelectPlayLevelPrefab, parentForUiLevelSelectPlayLevel.transform);
            newPlayLevel.GetComponent<UISelectLevelPlayBtn>().Init(i);
        }
    }
}
