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
        private GameObject efxOnDestroy;

        [SerializeField]
        private GameObject efxOnAttach;

        [SerializeField]
        private AudioClip sfxAttach;
        
        #region Block Overrides
        
        protected override void Awake()
        {
            base.Awake();
            
            BlockType = EBlockType.Stone;
        }
        
        public override bool MatchesWith(GameObject obj)
        {
            // can't be matched
            return false;
        }

        protected override ParticleSystem NewDestroyEfx()
        {
            GameObject efx = Instantiate(efxOnDestroy, transform.position, Quaternion.identity);
            ParticleSystem particleSys = efx.GetComponent<ParticleSystem>();

            ParticleSystem.MainModule mainModule = particleSys.main;
            mainModule.startColor = UnityColorFromType(BlockType);

            return particleSys;
        }

        protected override ParticleSystem NewAttachEfx()
        {
            GameObject efx = Instantiate(efxOnAttach, transform.position, Quaternion.identity);

            return efx.GetComponent<ParticleSystem>();
        }

        public override AudioClip SfxOnAttach()
        {
            return sfxAttach;
        }
        
        #endregion
    }
}