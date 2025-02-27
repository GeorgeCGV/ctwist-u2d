using Blocks;
using UnityEngine;
using UnityEngine.Assertions;
using static Model.BlockType;

namespace Spawn
{
    /// <summary>
    /// Allows to spawn <see cref="ColorBlock"/>.
    /// </summary>
    public class ColorBlockEntity : ISpawnEntity
    {
        private readonly EBlockType _blockColor;
        private readonly Color _unityColor;

        private readonly float _inSeconds;
        private readonly float _speed;

        public ColorBlockEntity(EBlockType type, float seconds, float speed)
        {
            Assert.IsTrue(EBlockTypeIsColorBlock(type), "expected color type");

            _blockColor = type;
            _unityColor = UnityColorFromType(type);
            _inSeconds = seconds;
            _speed = speed;
        }

        public Color BacklightColor()
        {
            // give some contrast when black spawn color is used
            return _unityColor == Black ? Color.white : _unityColor;
        }

        public Color SpawnColor()
        {
            return _unityColor;
        }

        public BasicBlock Create()
        {
            return BlocksFactory.Instance.NewColorBlock(_blockColor);
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