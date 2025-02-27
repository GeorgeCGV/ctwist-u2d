using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// Options view controller.
    /// </summary>
    public class UIOptionsCtrl : MonoBehaviour
    {
        [SerializeField]
        private Toggle sfxToggle;
        [SerializeField]
        private Toggle musicToggle;

        /// <summary>
        /// Checks that critical references are set
        /// and sets initial state to toggles.
        /// </summary>
        private void Awake()
        {
            Assert.IsNotNull(sfxToggle, "missing sfxToggle");
            Assert.IsNotNull(musicToggle, "missing musicToggle");

            sfxToggle.SetIsOnWithoutNotify(GameManager.IsSFXOn());
            musicToggle.SetIsOnWithoutNotify(GameManager.IsMusicOn());
        }

        /// <summary>
        /// Callback of the <c>sfxToggle</c>.
        /// </summary>
        /// <remarks>
        /// Set in the Editor.
        /// </remarks>
        /// <param name="value"></param>
        private void OnSfxValueChanged(bool value)
        {
            AudioManager.Instance.PlaySfx(sfxKey: AudioManager.Sfx.BtnClick);
            GameManager.ToggleSfx();
        }

        /// <summary>
        /// Callback of the <c>musicToggle</c>.
        /// </summary>
        /// <remarks>
        /// Set in the Editor.
        /// </remarks>
        /// <param name="value"></param>
        private void OnMusicValueChanged(bool value)
        {
            AudioManager.Instance.PlaySfx(sfxKey: AudioManager.Sfx.BtnClick);
            GameManager.ToggleMusic();
        }
    }
}