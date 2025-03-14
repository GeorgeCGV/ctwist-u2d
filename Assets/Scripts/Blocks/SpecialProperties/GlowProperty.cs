using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Blocks.SpecialProperties
{
    /// <summary>
    /// Block glow property.
    /// </summary>
    /// <remarks>
    /// Matches that have any block with such property destroy all blocks of the same type.
    /// </remarks>
    public class GlowProperty : IMatchProperty
    {
        /// <summary>
        /// Property configuration, holds references to resources (i.e. sprites).
        /// </summary>
        private readonly GlowPropertyConfig _config;

        /// <summary>
        /// Dynamically created object that holds glow overlay.
        /// </summary>
        private GameObject _glow;

        /// <summary>
        /// Instantiate new property.
        /// </summary>
        /// <param name="config">Configuration.</param>
        public GlowProperty(GlowPropertyConfig config)
        {
            Assert.IsNotNull(config, "missing property config");
            _config = config;
        }
        
        #region IMatchProperty

        public void Activate(BasicBlock parent)
        {
            _glow = Object.Instantiate(_config.glowOverlay, parent.transform);
            _glow.gameObject.layer = parent.gameObject.layer;
            // _glow.GetComponent<SpriteRenderer>().color = BlockType.UnityColorFromType(parent.BlockType);
        }

        public void ExecuteSpecial(BasicBlock parent, HashSet<BasicBlock> matches)
        {
            ColorBlock[] allObjects = Object.FindObjectsByType<ColorBlock>(FindObjectsSortMode.None);
            foreach (ColorBlock cb in allObjects)
            {
                if (cb.Destroyed || !cb.attached || cb.ColorType != parent.BlockType)
                {
                    continue;
                }

                matches.Add(cb);
            }
        }

        public EMatchPropertyOutcome Execute(out bool removeProperty)
        {
            // don't remove as we plan to call ExecuteSpecial
            removeProperty = false;
            // cleanup
            Object.Destroy(_glow);
            // require ExecuteSpecial execution
            return EMatchPropertyOutcome.SpecialMatchRule;
        }

        #endregion IMatchProperty
    }
}