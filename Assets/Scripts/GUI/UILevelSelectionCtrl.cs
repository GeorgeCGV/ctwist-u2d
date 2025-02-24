using UnityEngine;

public class LevelSelectionCtrl : MonoBehaviour
{
    [SerializeField]
    private Transform parentForUiLevelSelectPlayLevel;

    [SerializeField]
    private GameObject uiLevelSelectPlayLevelPrefab;

    public void CreateSelectableLevels()
    {
        for (int i = 0; i < GameManager.Instance.TotalLevels; i++)
        {
            GameObject newPlayLevel = Instantiate(uiLevelSelectPlayLevelPrefab, parentForUiLevelSelectPlayLevel);
            newPlayLevel.GetComponent<UISelectLevelPlayBtn>().Init(i);
        }
    }
}
