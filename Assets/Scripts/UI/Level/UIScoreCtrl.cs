using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace UI.Level
{
    /// <summary>
    /// Handles score view aspects.
    /// </summary>
    public class UIScoreCtrl : MonoBehaviour
    {
        /// <summary>
        /// Label to display the score with.
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI label;

        /// <summary>
        /// Checks that critical references are set.
        /// </summary>
        /// <remarks>
        /// Initial value is set to <c>0</c>.
        /// </remarks>
        private void Awake()
        {
            Assert.IsNotNull(label, "missing score label");
            label.text = "0";
        }

        private void OnEnable()
        {
            LevelManager.OnScoreUpdate += HandleScoreUpdate;
        }

        private void OnDestroy()
        {
            LevelManager.OnScoreUpdate -= HandleScoreUpdate;
        }

        /// <summary>
        /// Callback to update current score label.
        /// </summary>
        /// <remarks>
        /// <c>LevelManager.OnScoreUpdate</c>
        /// </remarks>
        /// <param name="newScore">New game score.</param>
        private void HandleScoreUpdate(int newScore)
        {
            // play bump animation if present
            UILabelBumpAnimator bumpAnimator = label.GetComponent<UILabelBumpAnimator>();
            if (bumpAnimator != null)
            {
                bumpAnimator.enabled = true;
            }
            
            label.text = newScore.ToString();
        }
    }
}