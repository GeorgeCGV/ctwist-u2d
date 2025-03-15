using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// Level selection item controller.
    /// </summary>
    /// <remarks>
    /// Manages level selection item animations and interactions.
    /// </remarks>
    public class UILevelSelectionItemCtrl : MonoBehaviour
    {
        /// <summary>
        /// Level ID.
        /// </summary>
        [SerializeField]
        private int id;

        /// <summary>
        /// Highlight object.
        /// </summary>
        /// <remarks>
        /// Active for all unlocked levels that don't have stars yet.
        /// </remarks>
        [SerializeField]
        private GameObject highlight;

        /// <summary>
        /// Play level button.
        /// </summary>
        [SerializeField]
        private Button playButton;

        /// <summary>
        /// Shows level number.
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI levelLabel;

        /// <summary>
        /// References to stars.
        /// </summary>
        [SerializeField]
        private List<GameObject> stars = new(3);

        /// <summary>
        /// Initializes the item.
        /// </summary>
        /// <remarks>
        /// Determines if level is locked/unlocked, how many stars (if any),
        /// and highlights the level.
        /// </remarks>
        /// <param name="levelId">Level ID.</param>
        public void Init(int levelId)
        {
            id = levelId;
            levelLabel.text = $"{id + 1}";

            // ease life in the editor
            name = "PlayLevel_" + id;

            bool unlocked = GameManager.IsLevelUnlocked(id);

            // activate/deactivate play button if level is unlocked/locked
            playButton.interactable = unlocked;

            if (!unlocked)
            {
                return;
            }
            
            // get level stars
            int starsAmount = GameManager.GetLevelStars(id);
            for (int i = 0; i < starsAmount; i++)
            {
                stars[i].SetActive(true);
            }

            // highlight level that is unlocked and has no stars
            highlight.SetActive(starsAmount == 0);
        }

        /// <summary>
        /// Callback of <c>playButton</c>.
        /// </summary>
        /// <remarks>
        /// Set in the Editor.
        /// </remarks>
        private void OnPlay()
        {
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.BtnClick);
            LevelLoader.Instance.LoadLevel(id);
        }
    }
}