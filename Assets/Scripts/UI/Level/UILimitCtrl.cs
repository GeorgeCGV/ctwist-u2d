using System;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace UI.Level
{
    /// <summary>
    /// Handles limit panel view aspects.
    /// Supports the following limits: none, time, spawn amount.
    /// </summary>
    public class UILimitCtrl : MonoBehaviour
    {
        private const string NoLimitLblText = "∞";

        /// <summary>
        /// Label to use to display the amount of time or spawns left.
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI label;

        /// <summary>
        /// Icon to show for spawns limit.
        /// </summary>
        [SerializeField]
        private GameObject spawnsIcon;

        /// <summary>
        /// Icon to show for time limit.
        /// </summary>
        [SerializeField]
        private GameObject timeIcon;

        /// <summary>
        /// Checks that critical references are set and initializes default view.
        /// </summary>
        private void Awake()
        {
            Assert.IsNotNull(label, "missing label");
            Assert.IsNotNull(spawnsIcon, "missing spawnsIcon");
            Assert.IsNotNull(timeIcon, "missing timeIcon");

            // hide all icons by default and set lbl to no limit txt
            spawnsIcon.SetActive(false);
            timeIcon.SetActive(false);

            // show something on the panel,
            // assume no limit mode by default
            label.text = NoLimitLblText;
        }

        private void OnDestroy()
        {
            LevelManager.OnTimeLeftUpdate -= HandleTimeLeftUpdate;
            LevelManager.OnSpawnsLeftUpdate -= HandleSpawnsLeftUpdate;
        }

        /// <summary>
        /// Initializes the view and setups required callbacks.
        /// </summary>
        /// <param name="data">Level limit data.</param>
        public void Init(Limit data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            ELimitVariant limit = data.Variant();

            if (limit == ELimitVariant.TimeLimit)
            {
                int seconds = Mathf.FloorToInt(data.time % 60);
                int minutes = Mathf.FloorToInt(data.time / 60);

                HandleTimeLeftUpdate(minutes, seconds);

                timeIcon.SetActive(true);
                LevelManager.OnTimeLeftUpdate += HandleTimeLeftUpdate;
            }
            else if (limit == ELimitVariant.SpawnLimit)
            {
                HandleSpawnsLeftUpdate(0, data.spawns);

                spawnsIcon.SetActive(true);
                LevelManager.OnSpawnsLeftUpdate += HandleSpawnsLeftUpdate;
            }

            // no limit
        }

        /// <summary>
        /// Callback to update spawns/moves left.
        /// </summary>
        /// <remarks>
        /// <see cref="LevelManager.OnSpawnsLeftUpdate"/>
        /// </remarks>
        /// <param name="spawnedAmount">Spawned block amount during the game.</param>
        /// <param name="totalSpawns">Spawns limit.</param>
        private void HandleSpawnsLeftUpdate(int spawnedAmount, int totalSpawns)
        {
            label.text = (totalSpawns - spawnedAmount).ToString();
        }

        /// <summary>
        /// Callback to update remaining time.
        /// </summary>
        /// <remarks>
        /// <see cref="LevelManager.OnTimeLeftUpdate"/>
        /// </remarks>
        /// <param name="min">Minutes left.</param>
        /// <param name="sec">Seconds left.</param>
        private void HandleTimeLeftUpdate(int min, int sec)
        {
            label.text = $"{min:D2}:{sec:D2}";
        }
    }
}