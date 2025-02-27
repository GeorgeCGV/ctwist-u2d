using System;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI.Level
{
    /// <summary>
    /// Controller for the level blocks goal item.
    /// </summary>
    /// <remarks>
    /// The level can have up to 3 block goals.
    /// </remarks>
    public class UIBlockGoalItemCtrl : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI label;

        [SerializeField]
        private Image icon;

        private int _targetMatches;

        /// <summary>
        /// Updates label to remaining matches.
        /// </summary>
        /// <param name="currentAmount">Current matches amount.</param>
        public void UpdateMatchesLeft(int currentAmount)
        {
            label.text = Math.Max(_targetMatches - currentAmount, 0).ToString();
        }

        /// <summary>
        /// Initializers the item.
        /// </summary>
        /// <param name="sprite">Icon sprite.</param>
        /// <param name="target">Target matches amount.</param>
        public void Init(Sprite sprite, int target)
        {
            icon.sprite = sprite ?? throw new ArgumentException("icon sprite can't be null");
            _targetMatches = target;
            UpdateMatchesLeft(0);
        }

        /// <summary>
        /// Checks that critical references are set.
        /// </summary>
        private void Awake()
        {
            Assert.IsNotNull(label, "missing label");
            Assert.IsNotNull(icon, "missing image");
        }
    }
}