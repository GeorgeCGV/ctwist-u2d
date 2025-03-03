using System;
using UnityEngine;
using static Model.BlockType;

namespace Blocks
{
    /// <summary>
    /// One of the main game entities.
    /// </summary>
    /// <remarks>
    /// The most basic block in the game.
    /// Colored blocks match with blocks of the same color.
    /// </remarks>
    [RequireComponent(typeof(Animator))]
    public class ColorBlock : BasicBlock
    {
        private static readonly int AnimatorColorIntParam = Animator.StringToHash("Color");

        [SerializeField]
        private GameObject efxOnDestroy;

        [SerializeField]
        private GameObject efxOnAttach;

        [SerializeField]
        private AudioClip sfxAttach;

        public EBlockType ColorType
        {
            get => BlockType;
            set
            {
                int animatorTriggerValue;
                switch (value)
                {
                    case EBlockType.Red:
                        animatorTriggerValue = 0;
                        break;
                    case EBlockType.Blue:
                        animatorTriggerValue = 1;
                        break;
                    case EBlockType.White:
                        animatorTriggerValue = 2;
                        break;
                    case EBlockType.Black:
                        animatorTriggerValue = 3;
                        break;
                    case EBlockType.Green:
                        animatorTriggerValue = 4;
                        break;
                    case EBlockType.Yellow:
                        animatorTriggerValue = 5;
                        break;
                    case EBlockType.Pink:
                        animatorTriggerValue = 6;
                        break;
                    case EBlockType.Purple:
                        animatorTriggerValue = 7;
                        break;
                    default:
                        throw new NotImplementedException("unknown color");
                }

                GetComponent<Animator>().SetInteger(AnimatorColorIntParam, animatorTriggerValue);
                
                BlockType = value;
            }
        }

        #region Block Overrides
        
        public override bool MatchesWith(GameObject obj)
        {
            ColorBlock other = obj.GetComponent<ColorBlock>();
            return (other != null) && (other.BlockType == BlockType);
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