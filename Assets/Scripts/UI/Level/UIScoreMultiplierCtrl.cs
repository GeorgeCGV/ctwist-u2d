using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI.Level
{
    /// <summary>
    /// Manages multiplier view.
    /// </summary>
    /// <remarks>
    /// Used by <c>MultiplierHandler</c>.
    /// </remarks>
    public class UIScoreMultiplierCtrl : MonoBehaviour
    {
        /// <summary>
        /// Fill bar image to control fill amount of.
        /// </summary>
        /// <remarks>
        /// Shows the state of the multiplier decay.
        /// </remarks>
        [SerializeField]
        private Image fillBar;

        /// <summary>
        /// Label to display current multiplier value.
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI label;

        /// <summary>
        /// Checks that critical references are set.
        /// </summary>
        private void Awake()
        {
            Assert.IsNotNull(fillBar, "missing fillBar");
            Assert.IsNotNull(label, "missing multiplier label");
        }

        private void OnEnable()
        {
            MultiplierHandler.OnMultiplierTimerUpdate += HandleMultiplierTimerUpdate;
            MultiplierHandler.OnMultiplierUpdate += HandleMultiplierUpdate;
        }

        public void OnDestroy()
        {
            MultiplierHandler.OnMultiplierTimerUpdate -= HandleMultiplierTimerUpdate;
            MultiplierHandler.OnMultiplierUpdate += HandleMultiplierUpdate;
        }

        /// <summary>
        /// Callback to update current score label.
        /// </summary>
        /// <remarks>
        /// <c>MultiplierHandler.OnMultiplierUpdate</c>
        /// </remarks>
        /// <param name="val">Current multiplier.</param>
        private void HandleMultiplierUpdate(int val)
        {
            label.text = $"x{val}";
        }

        /// <summary>
        /// Callback to multiplier timer update.
        /// </summary>
        /// <remarks>
        /// <c>MultiplierHandler.OnMultiplierTimerUpdate</c>
        /// </remarks>
        /// <param name="current">Current time.</param>
        /// <param name="max">Maximum time.</param>
        private void HandleMultiplierTimerUpdate(float current, float max)
        {
            fillBar.fillAmount = current / max;
        }
    }
}