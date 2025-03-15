using UnityEngine;
using UnityEngine.Assertions;

namespace UI.Menu
{
    /// <summary>
    /// Menu Scene UI coordinator.
    /// </summary>
    /// <remarks>
    /// Controls the UI in the menu scene.
    /// Coordinates other UI components (i.e. shows options/level selection).
    /// As the menu is simple there is no need in many extra controllers.
    /// </remarks>
    public class UIMenuCoordinator : MonoBehaviour
    {
        private static readonly int AnimatorTriggerOpen = Animator.StringToHash("Open");
        private static readonly int AnimatorTriggerClose = Animator.StringToHash("Close");

        /// <summary>
        /// Menu background music.
        /// </summary>
        /// <remarks>
        /// Placed here as the menu coordinator in case of the menu scene
        /// is the main scene logic; stay pragmatic.
        /// </remarks>
        [SerializeField]
        private AudioClip backgroundMusic;

        /// <summary>
        /// Options panel/pop-up animator.
        /// </summary>
        [SerializeField]
        private Animator optionsPanelAnimator;

        /// <summary>
        /// Level selection panel/pop-up.
        /// </summary>
        [SerializeField]
        private GameObject levelSelectionPanel;

        /// <summary>
        /// Animator of <c>levelSelectionPanel</c>.
        /// </summary>
        private Animator _levelSelectionPanelAnimator;

        /// <summary>
        /// Checks that critical references are set.
        /// </summary>
        private void Awake()
        {
            Assert.IsNotNull(optionsPanelAnimator, "missing optionsPanel");
            Assert.IsNotNull(levelSelectionPanel, "missing levelSelectionPanel");

            _levelSelectionPanelAnimator = levelSelectionPanel.GetComponent<Animator>();
            Assert.IsNotNull(_levelSelectionPanelAnimator, "levelSelectionPanel doesn't have Animator");
        }

        /// <summary>
        /// Starts background music and prepares available levels.
        /// </summary>
        private void Start()
        {
            // start with music
            AudioManager.Instance.PlayMusic(backgroundMusic);
            // create ui level selection items for the level selection
            // done manually as the component is not active by default
            levelSelectionPanel.GetComponent<LevelSelectionCtrl>().CreateSelectableLevels();
        }

        /// <summary>
        /// Plays required SFXes on panel close & open and shows the panel.
        /// </summary>
        /// <remarks>
        /// <c>Animator</c> is expected to have <c>animatorTriggerOpen</c> and <c>animatorTriggerClose</c>.
        /// </remarks>
        /// <param name="panelAnimator">Panel animator to trigger.</param>
        /// <param name="open">Close or open flag.</param>
        private static void PanelAction(Animator panelAnimator, bool open)
        {
            panelAnimator.gameObject.SetActive(open);
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.BtnClick);
            panelAnimator.SetTrigger(open ? AnimatorTriggerOpen : AnimatorTriggerClose);
            AudioManager.Instance.PlaySfx(open ? AudioManager.Sfx.DialogAppear : AudioManager.Sfx.DialogDisappear);
        }

        /// <summary>
        /// Callback for play (level selection) button.
        /// </summary>
        /// <remarks>
        /// Set in the editor.
        /// </remarks>
        private void OnLevelSelectionOpen()
        {
            PanelAction(_levelSelectionPanelAnimator, true);
        }

        /// <summary>
        /// Callback for level selection close button.
        /// </summary>
        /// <remarks>
        /// Set in the editor.
        /// </remarks>
        private void OnLevelSelectionClose()
        {
            PanelAction(_levelSelectionPanelAnimator, false);
        }

        /// <summary>
        /// Callback for options button.
        /// </summary>
        /// <remarks>
        /// Set in the editor.
        /// </remarks>
        private void OnOptionsOpen()
        {
            PanelAction(optionsPanelAnimator, true);
        }

        /// <summary>
        /// Callback for options close button.
        /// </summary>
        /// <remarks>
        /// Set in the editor.
        /// </remarks>
        private void OnOptionsClose()
        {
            PanelAction(optionsPanelAnimator, false);
        }
    }
}