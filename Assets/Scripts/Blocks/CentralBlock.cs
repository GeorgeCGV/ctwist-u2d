using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CentralBlock : BasicBlock
{

#if UNITY_EDITOR
    protected override void Update()
    {
        DrawCollider();

        DrawEdgeAttachmentRays();

        DrawNeighbourLinkRays();
    }
#endif

    protected override void FixedUpdate()
    {
        // we don't need gravity in central block
    }

    protected override void OnCollisionEnter2D(Collision2D other)
    {
        // nothing to do
    }
}
