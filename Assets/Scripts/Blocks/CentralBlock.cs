using UnityEngine;
using static Model.BlockType;

namespace Blocks
{
    /// <summary>
    /// Central block.
    /// </summary>
    /// <remarks>
    /// Only one instance shall exist per level.
    /// It is not affected by gravity or collisions.
    /// Acts as a player.
    /// </remarks>
    [RequireComponent(typeof(Animator))]
    public class CentralBlock : BasicBlock
    {
        #region Unity
        
        protected override void Awake()
        {
            attached = true;
            BlockType = EBlockType.Central;
        }

        protected override void FixedUpdate()
        {
            // we don't need gravity in central block
        }

        protected override void OnCollisionEnter2D(Collision2D other)
        {
            // nothing to do
        }
        
        #endregion
    }
}