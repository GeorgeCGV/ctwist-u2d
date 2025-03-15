using UnityEngine;
using static Model.BlockType;

namespace Blocks
{
    /// <summary>
    /// Stone block.
    /// </summary>
    /// <remarks>
    /// Special block that can't be destructed via match,
    /// only as "floating". 
    /// </remarks>
    public class StoneBlock : BasicBlock
    {
        [SerializeField]
        private ParticleSystem efxOnDestroy;

        [SerializeField]
        private ParticleSystem efxOnAttach;

        [SerializeField]
        private AudioClip sfxAttach;
        
        #region Block Overrides
        
        protected override void Awake()
        {
            base.Awake();
            BlockType = EBlockType.Stone;
        }
        
        public override bool MatchesWith(BasicBlock other)
        {
            // can't be matched
            return false;
        }

        protected override ParticleSystem NewDestroyEfx()
        {
            if (!efxOnDestroy)
            {
                return null;
            }
            // used to be instantiation, replaced by simply using the object
            // set to null to move object to the scene
            efxOnDestroy.transform.parent = null;
            ParticleSystem.MainModule mainModule = efxOnDestroy.main;
            mainModule.startColor = UnityColorFromType(BlockType);
            return efxOnDestroy;
        }

        protected override ParticleSystem NewAttachEfx()
        {
            return Instantiate(efxOnAttach, transform.position, Quaternion.identity);
        }

        public override AudioClip SfxOnAttach()
        {
            return sfxAttach;
        }
        
        #endregion
    }
}