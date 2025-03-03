using System;
using UnityEngine;

namespace Model
{
    /// <summary>
    /// Block type related functions and types.
    /// </summary>
    public static class BlockType
    {
        public static readonly Color Red = new Color32(255, 103, 99, 255);
        public static readonly Color Blue = new Color32(109, 164, 255, 255);
        public static readonly Color White = new Color32(230, 230, 230, 255);
        public static readonly Color Black = new Color32(67, 67, 67, 255);
        public static readonly Color Yellow = new Color32(190, 178, 72, 255);
        public static readonly Color Green = new Color32(46, 159, 99, 255);
        public static readonly Color Pink = new Color32(204, 113, 184, 255);
        public static readonly Color Purple = new Color32(132, 42, 195, 255);
        public static readonly Color Stone = new Color32(98, 98, 112, 255);

        /// <summary>
        /// Game consists of blocks, each block has a type.
        /// </summary>
        public enum EBlockType
        {
            Central,
            Blue,
            Red,
            Yellow,
            Green,
            White,
            Purple,
            Pink,
            Black,
            Stone,
        }

        /// <summary>
        /// Returns <see cref="Color"/> based on <see cref="EBlockType"/>.
        /// </summary>
        /// <remarks>
        /// Used for different elements like EFX, sprite color.
        /// </remarks>
        /// <param name="value">Block type.</param>
        /// <returns>Unity's color.</returns>
        /// <exception cref="NotImplementedException">If such block type is unsupported or unknown.</exception>
        public static Color UnityColorFromType(EBlockType value)
        {
            switch (value)
            {
                case EBlockType.Red:
                    return Red;
                case EBlockType.Blue:
                    return Blue;
                case EBlockType.White:
                    return White;
                case EBlockType.Yellow:
                    return Yellow;
                case EBlockType.Green:
                    return Green;
                case EBlockType.Purple:
                    return Purple;
                case EBlockType.Pink:
                    return Pink;
                case EBlockType.Black:
                    return Black;
                case EBlockType.Stone:
                    return Stone;
                default:
                    throw new NotImplementedException("not supported");
            }
        }

        /// <summary>
        /// Checks if type is a color block.
        /// </summary>
        /// <param name="type">Block type.</param>
        /// <returns>True if so, otherwise False.</returns>
        public static bool EBlockTypeIsColorBlock(EBlockType type)
        {
            return type != EBlockType.Central && type != EBlockType.Stone;
        }

        /// <summary>
        /// Tries to parse block type from string.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <exception cref="ArgumentException">If unable to parse.</exception>
        /// <returns>EBlockType.</returns>
        public static EBlockType EBlockTypeFromString(string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "central":
                    return EBlockType.Central;
                case "stone":
                    return EBlockType.Stone;
                case "blue":
                    return EBlockType.Blue;
                case "red":
                    return EBlockType.Red;
                case "yellow":
                    return EBlockType.Yellow;
                case "green":
                    return EBlockType.Green;
                case "white":
                    return EBlockType.White;
                case "purple":
                    return EBlockType.Purple;
                case "pink":
                    return EBlockType.Pink;
                case "black":
                    return EBlockType.Black;
                default:
                    throw new ArgumentException($"unknown type {value}");
            }
        }
    }
}