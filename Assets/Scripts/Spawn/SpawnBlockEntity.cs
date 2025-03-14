using Blocks;
using Blocks.SpecialProperties;
using UnityEngine;
using UnityEngine.Assertions;
using static Model.BlockType;

namespace Spawn
{
    /// <summary>
    /// Allows to spawn <see cref="ColorBlock"/>, <see cref="StoneBlock"/>.
    /// </summary>
    public class SpawnBlockEntity : ISpawnEntity
    {
        private readonly EBlockType _blockType;
        private readonly Color _unityColor;

        private readonly float _inSeconds;
        private readonly float _speed;
        private readonly IMatchProperty _matchProperty;

        public SpawnBlockEntity(EBlockType type, float seconds, float speed, IMatchProperty matchProperty = null)
        {
            Assert.IsTrue(EBlockTypeIsColorBlock(type) || type == EBlockType.Stone,
                "expected color type");

            _blockType = type;
            _unityColor = UnityColorFromType(type);
            _inSeconds = seconds;
            _speed = speed;
            _matchProperty = matchProperty;
        }

        public Color BacklightColor()
        {
            // give some contrast when main color ir dark
            return _unityColor == Black || _unityColor == Stone ? Color.white : _unityColor;
        }

        public Color SpawnColor()
        {
            return _unityColor;
        }

        public BasicBlock Create()
        {
            if (_blockType == EBlockType.Stone)
            {
                return BlocksFactory.Instance.NewStoneBlock();
            }

            BasicBlock block = BlocksFactory.Instance.NewColorBlock(_blockType);
            if (_matchProperty != null)
            {
                block.MatchProperty = _matchProperty;
            }

            return block;
        }

        public float SpawnInSeconds()
        {
            return _inSeconds;
        }

        public float BlockStartSpeed()
        {
            return _speed;
        }
    }
}