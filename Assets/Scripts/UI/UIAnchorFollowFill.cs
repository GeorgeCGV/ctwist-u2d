using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Makes current component <see cref="RectTransform.anchoredPosition"/> to follow
    /// set image's <see cref="Image.fillAmount"/>.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public class UIAnchorFollowFill : MonoBehaviour
    {
        /// <summary>
        /// This object anchor.
        /// </summary>
        private RectTransform _anchor;
        
        /// <summary>
        /// Helps to avoid new anchor vector allocation on each update. 
        /// </summary>
        private Vector2 _newAnchorValue;

        /// <summary>
        /// The <see cref="Image"/> to monitor <see cref="Image.fillAmount"/> of.
        /// </summary>
        [SerializeField]
        public Image fillImage;

        /// <summary>
        /// The <see cref="Image.fillAmount"/> of the <see cref="fillImage"/>
        /// at below which the self-image component is going to be disabled.
        /// </summary>
        [SerializeField]
        public float hideSelfImageAtFill;

        /// <summary>
        /// Ref. to self-image component to "hide" (controls enable state),
        /// when <see cref="Image.fillAmount"/> of the <see cref="fillImage"/>
        /// reaches <see cref="hideSelfImageAtFill"/>. 
        /// </summary>
        private Image _image;

        /// <summary>
        /// Grab and assert required references.
        /// </summary>
        private void Awake()
        {
            Assert.IsNotNull(fillImage, "missing fillImage");
            _anchor = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
        }

        /// <summary>
        /// Perform animation in late update to guarantee that all fillAmount
        /// related updates that happen on Update are done.
        /// </summary>
        private void LateUpdate()
        {
#if UNITY_EDITOR
            // in case the editor didn't run awake
            if (_anchor is null)
            {
                _anchor = GetComponent<RectTransform>();
            }
            if (_image is null)
            {
                _image = GetComponent<Image>();
            }
#endif
            // compute new anchor position where x is the fill amount 
            _newAnchorValue.Set(fillImage.fillAmount, _anchor.anchorMax.y);
            // set new anchor limits
            _anchor.anchorMax = _newAnchorValue;
            _anchor.anchorMin = _newAnchorValue;
            
            // hide / show the image
            _image.enabled = fillImage.fillAmount > hideSelfImageAtFill;
        }
    }
}