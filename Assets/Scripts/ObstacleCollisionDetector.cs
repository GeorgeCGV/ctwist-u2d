using Blocks;
using UnityEngine;

/// <summary>
/// Attached to every Obstacle tilemap prefab.
/// </summary>
/// <remarks>
/// Destroys collided blocks.
/// </remarks>
[RequireComponent(typeof(Rigidbody2D))]
public class ObstacleCollisionDetector : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // tilemap is configured to process collisions
        // only against attached blocks (blocks layer)
        LevelManager.Instance.OnBlocksObstructionCollision(other.gameObject.GetComponent<BasicBlock>());
    }
}