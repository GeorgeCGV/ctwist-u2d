using System;
using System.Collections.Generic;
using System.IO;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.U2D;
using UnityEngine.UI;
using static Model.BlockType;

namespace UI.Level
{
    /// <summary>
    /// UI level goal controller.
    /// </summary>
    /// <remarks>
    /// Supports score and up to 3 block goals.
    /// </remarks>
    public class UIGoalCtrl : MonoBehaviour
    {
        #region Score Goal

        [SerializeField]
        private GameObject scoreGoalIcon;

        [SerializeField]
        private TMP_Text scoreGoalLabel;

        [SerializeField]
        private Image fillBar;

        #endregion

        #region Block Goal

        [SerializeField]
        private SpriteAtlas blockGoalItemIcons;

        [SerializeField]
        private GameObject blockGoalsParent;

        [SerializeField]
        private GameObject blockGoalsItemPrefab;

        /// <summary>
        /// Maps level's block goal <see cref="EBlockType"/> to <see cref="UIBlockGoalItemCtrl"/>.
        /// </summary>
        private readonly Dictionary<EBlockType, UIBlockGoalItemCtrl> _blockGoals = new(3);

        /// <summary>
        /// Constructs icon name from block type located in the <c>_blockGoalItemIcons</c> sprite atlas.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string AtlasSpriteNameFromEBlockType(EBlockType type)
        {
            return $"hex_{type.ToString().ToLowerInvariant()}_0";
        }

        #endregion

        /// <summary>
        /// Initializes the goal controller.
        /// </summary>
        /// <param name="data">Level goal data, can't be null.</param>
        public void Init(Goal data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            EGoalVariant goal = data.Variant();
            if (goal == EGoalVariant.Score)
            {
                scoreGoalIcon.gameObject.SetActive(true);
                scoreGoalLabel.gameObject.SetActive(true);
                fillBar.gameObject.SetActive(true);

                scoreGoalLabel.text = data.score.ToString();
                HandleScoreUpdate(0);
                LevelManager.OnScoreUpdate += HandleScoreUpdate;
            }
            else if (goal == EGoalVariant.Blocks)
            {
                blockGoalsParent.gameObject.SetActive(true);

                foreach (BlocksGoal blockGoal in data.blocks)
                {
                    GameObject prefab = Instantiate(blockGoalsItemPrefab, blockGoalsParent.transform);
                    UIBlockGoalItemCtrl uiItem = prefab.GetComponent<UIBlockGoalItemCtrl>();

                    uiItem.Init(blockGoalItemIcons.GetSprite(AtlasSpriteNameFromEBlockType(blockGoal.ParsedType)),
                        blockGoal.amount);

                    _blockGoals.Add(blockGoal.ParsedType, uiItem);

                    LevelManager.OnBlocksStatsUpdate += HandleMatchChanges;
                }
            }
            else
            {
                throw new InvalidDataException("unknown level goal variant");
            }
        }

        /// <summary>
        /// Checks that critical references are set and hides all goals by default.
        /// </summary>
        private void Awake()
        {
            Assert.IsNotNull(scoreGoalIcon, "missing score goal icon");
            scoreGoalIcon.gameObject.SetActive(false);
            Assert.IsNotNull(scoreGoalLabel, "missing score goal label");
            scoreGoalLabel.gameObject.SetActive(false);
            Assert.IsNotNull(fillBar, "missing fillbar");
            fillBar.gameObject.SetActive(false);

            Assert.IsNotNull(blockGoalItemIcons, "missing block goal item icons atlas");
            Assert.IsNotNull(blockGoalsParent, "missing block goals parent");
            blockGoalsParent.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            LevelManager.OnScoreUpdate -= HandleScoreUpdate;
            LevelManager.OnBlocksStatsUpdate -= HandleMatchChanges;
        }

        /// <summary>
        /// Handles level block stats event.
        /// </summary>
        /// <param name="matched">BlocksStats.</param>
        /// <remarks>
        /// <see cref="LevelManager.OnBlocksStatsUpdate"/>
        /// </remarks>
        private void HandleMatchChanges(BlocksStats matched)
        {
            foreach (KeyValuePair<EBlockType, UIBlockGoalItemCtrl> entry in _blockGoals)
            {
                if (matched.Matched.TryGetValue(entry.Key, out int matchedAmounts))
                {
                    entry.Value.UpdateMatchesLeft(matchedAmounts);
                }
            }
        }

        /// <summary>
        /// Handles level score update.
        /// </summary>
        /// <param name="score">Current score.</param>
        /// <remarks>
        /// <see cref="LevelManager.OnScoreUpdate"/>
        /// </remarks>
        private void HandleScoreUpdate(int score)
        {
            float goal = float.Parse(scoreGoalLabel.text);
            if (goal <= 0)
            {
                // nothing to do if the goal is not set
                return;
            }

            fillBar.fillAmount = score / goal;
        }
    }
}