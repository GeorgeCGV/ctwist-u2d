using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Useful utils.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Searches for the clip with provided clip name in the animator
        /// and returns its animation length in seconds.
        /// Ignores casing.
        /// Throws exception when the clip is not found.
        /// </summary>
        /// <param name="animator">Animator.</param>
        /// <param name="clipName">Clip name.</param>
        /// <returns>Animation length in seconds.</returns>
        public static float GetAnimatorClipLength(Animator animator, string clipName)
        {
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            AnimationClip clip = controller.animationClips.FirstOrDefault(c =>
                string.Equals(c.name, clipName, StringComparison.OrdinalIgnoreCase));

            if (clip == null)
            {
                throw new KeyNotFoundException($"Clip '{clipName}' not found in Animator '{animator.name}'");
            }

            return clip.length;
        }

        /// <summary>
        /// Finds distance from the point to the line
        /// </summary>
        /// <param name="point">Point.</param>
        /// <param name="lineStartPoint">Line start point.</param>
        /// <param name="lineEndPoint">Line end point.</param>
        /// <returns>Squared distance.</returns>
        public static float DistancePointToLineSegment(Vector2 point, Vector2 lineStartPoint, Vector2 lineEndPoint)
        {
            Vector2 lineDirection = lineEndPoint - lineStartPoint;
            float lineLengthSquared = lineDirection.sqrMagnitude;

            // if line segment is a point
            if (Mathf.Approximately(lineLengthSquared, 0.0f))
            {
                return (point - lineStartPoint).sqrMagnitude;
            }

            // project point onto the line and clamp to [0, 1] (ensure it's on the segment).
            float t = Mathf.Clamp01(Vector2.Dot(point - lineStartPoint, lineDirection) / lineLengthSquared);
            Vector2 closestPoint = lineStartPoint + t * lineDirection;

            // Calculate and return the distance.
            return (point - closestPoint).sqrMagnitude;
        }
    }
}