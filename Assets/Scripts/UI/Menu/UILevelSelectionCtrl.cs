using UnityEngine;

namespace UI.Menu
{
    /// <summary>
    /// Level selection menu controller.
    /// </summary>
    /// <remarks>
    /// Creates <c>UILevelSelectionItemCtrl</c> within its scroll view content.
    /// </remarks>
    public class LevelSelectionCtrl : MonoBehaviour
    {
        /// <summary>
        /// Reference to scroll-view content.
        /// </summary>
        [SerializeField]
        private Transform parentForUiLevelSelectPlayLevel;

        /// <summary>
        /// Prefab for <c>UILevelSelectionItemCtrl</c> scroll-view item.
        /// </summary>
        [SerializeField]
        private GameObject uiLevelSelectPlayLevelPrefab;

        /// <summary>
        /// Creates <c>UILevelSelectionItemCtrl</c> items.
        /// </summary>
        /// <remarks>
        /// The amount of levels is taken from <c>GameManager.Instance.TotalLevels</c>.
        /// </remarks>
        public void CreateSelectableLevels()
        {
            for (int i = 0; i < GameManager.Instance.TotalLevels; i++)
            {
                GameObject newPlayLevel = Instantiate(uiLevelSelectPlayLevelPrefab, parentForUiLevelSelectPlayLevel);
                newPlayLevel.GetComponent<UILevelSelectionItemCtrl>().Init(i);
            }
        }
    }
}