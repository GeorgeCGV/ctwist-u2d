using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Blocks.SpecialProperties
{
    /// <summary>
    /// Block chain property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Matches that have any block with such property can't be destroyed on the first match.
    /// </para>
    /// <para>
    /// The first match destroys all chains in a match group. That allows consequent match
    /// to destroy the blocks.
    /// </para>
    /// </remarks>
    public class ChainedProperty : IMatchProperty
    {
        /// <summary>
        /// Property configuration, holds references to resources (i.e. sprites).
        /// </summary>
        private readonly ChainedPropertyConfig _config;

        /// <summary>
        /// Dynamically created object that holds chain sprite.
        /// </summary>
        private GameObject _chain;

        /// <summary>
        /// Instantiate new property.
        /// </summary>
        /// <param name="config">Configuration.</param>
        public ChainedProperty(ChainedPropertyConfig config)
        {
            Assert.IsNotNull(config, "missing property config");
            _config = config;
        }

        #region IMatchProperty

        public void Activate(BasicBlock parent)
        {
            _chain = new(GetType().Name)
            {
                transform =
                {
                    parent = parent.transform,
                    localPosition = Vector3.zero
                },
                layer = parent.gameObject.layer
            };

            SpriteRenderer renderer = _chain.AddComponent<SpriteRenderer>();
            renderer.sprite = _config.chainSprite;
        }

        public void ExecuteSpecial(BasicBlock parent, HashSet<BasicBlock> matches)
        {
            // empty, no special match execution
        }

        public EMatchPropertyOutcome Execute(out bool removeProperty)
        {
            // block shall remove this property
            removeProperty = true;

            // play sfx, efx, and destroy the chain
            AudioManager.Instance.PlaySfx(_config.sfxOnDestroy);
            Object.Instantiate(_config.efxOnDestroy, _chain.transform.position, Quaternion.identity).Play();
            Object.Destroy(_chain);

            // don't destroy any blocks
            return EMatchPropertyOutcome.StopMatching;
        }

        #endregion IMatchProperty
    }
}