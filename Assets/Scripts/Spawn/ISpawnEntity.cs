using Blocks;
using UnityEngine;

namespace Spawn
{
    /// <summary>
    /// Interface to provide information to the spawn node
    /// what entity has to be spawned.
    /// </summary>
    public interface ISpawnEntity
    {
        /// <summary>
        /// Provides secondary/backlight color for the spawn node.
        /// </summary>
        /// <returns>UnityEngine.Color</returns>
        Color BacklightColor();

        /// <summary>
        /// Provides main color for the spawn node itself.
        /// </summary>
        /// <returns>UnityEngine.Color</returns>
        Color SpawnColor();

        /// <summary>
        /// How much time the spawn node has for its animation
        /// before spawned object is created/spawned.
        /// </summary>
        /// <returns></returns>
        float SpawnInSeconds();

        /// <summary>
        /// Desired start speed/force of the spawned block.
        /// </summary>
        /// <returns>Float value > 0</returns>
        float BlockStartSpeed();

        /// <summary>
        /// Called by the spawn node when it is time to swpan the
        /// entity (i.e. after the animation is complete).
        /// </summary>
        /// <returns>Instantiated Entity as GameObject.</returns>
        BasicBlock Create();
    }
}