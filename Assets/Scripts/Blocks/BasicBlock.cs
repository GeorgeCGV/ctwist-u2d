using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
[RequireComponent(typeof(PolygonCollider2D))]
public class BasicBlock : MonoBehaviour
{
    public enum EdgeIndex : int
    {
        RightTop = 0,
        RightBottom = 1,
        Bottom = 2,
        LeftBottom = 3,
        LeftTop = 4,
        Top = 5,
    };

    public static readonly Dictionary<EdgeIndex, Vector2> EdgeOffsets = new Dictionary<EdgeIndex, Vector2>()
    {
        {EdgeIndex.RightTop, new Vector2(0.35f, 0.2f)},
        {EdgeIndex.RightBottom, new Vector2(0.35f, -0.2f)},
        {EdgeIndex.Bottom, new Vector2(0.0f, -0.4f)},
        {EdgeIndex.LeftBottom, new Vector2(-.35f, -0.2f)},
        {EdgeIndex.LeftTop, new Vector2(-0.35f, 0.2f)},
        {EdgeIndex.Top, new Vector2(0.0f, 0.4f)},
    };

    [SerializeField]
    protected float neighbourRange = 0.36f;

    [SerializeField]
    protected float edgeAttachPositionOffset = 0.85f;

    [SerializeField]
    protected Vector2 gravityPoint = Vector2.zero;

    [SerializeField]
    public float GravityStrength = 1.0f;

    /// <summary>
    /// Block's links/joints to its neighbours.
    /// </summary>
    /// <typeparam name="EdgeIndex">Edge Index.</typeparam>
    /// <typeparam name="AnchoredJoint2D">Link.</typeparam>
    public SerializedDictionary<EdgeIndex, AnchoredJoint2D> links = new SerializedDictionary<EdgeIndex, AnchoredJoint2D>()
    {
        {EdgeIndex.RightTop, null},
        {EdgeIndex.RightBottom, null},
        {EdgeIndex.Bottom, null},
        {EdgeIndex.LeftBottom, null},
        {EdgeIndex.LeftTop, null},
        {EdgeIndex.Top, null},
    };

    /// <summary>
    /// Attached flag.
    // Is set to True when block is attached to another block
    // that leads to central block.
    /// </summary>
    public bool attached = false;

    /// <summary>
    /// Destroyed flag.
    /// Is set to True when block is marked for destruction.
    /// </summary>
    public bool destroyed = false;

    /// <summary>
    /// Possible obstruction tilemap present in the level.
    /// Used here to detect collisions against it.
    /// </summary>
    private Tilemap obstructionsTilemap;

    /// <summary>
    /// Layer where all attached block gameobjects are.
    /// </summary>
    private int blocksLayer;

    #region Helpers
    /// <summary>
    /// Finds closes collider edge to provided point.
    /// </summary>
    /// <param name="collider">Block's collider.</param>
    /// <param name="point">Point.</param>
    /// <returns>Edge points and index.</returns>
    public static (Vector2 point1, Vector2 point2, EdgeIndex edgeIdx) FindClosestColliderEdge(PolygonCollider2D collider, Vector2 point)
    {
        Vector2[] points = collider.points;
        int pointsLength = points.Length;

        Vector2 closestPoint1 = Vector2.zero;
        Vector2 closestPoint2 = Vector2.zero;
        Vector2 point1;
        Vector2 point2;
        int edgeIndex = -1;
        float minDistance = float.MaxValue;

        // transform all points once
        Vector2[] transformedPoints = new Vector2[pointsLength];
        for (int i = 0; i < pointsLength; i++)
        {
            transformedPoints[i] = collider.transform.TransformPoint(points[i]);
        }

        // find the closest edge
        for (int i = 1; i <= pointsLength; i++)
        {
            if (i == pointsLength)
            {
                point1 = transformedPoints[pointsLength - 1];
                point2 = transformedPoints[0];
            }
            else
            {
                point1 = transformedPoints[i - 1];
                point2 = transformedPoints[i];
            }

            float distance = Utils.DistancePointToLineSegment(point, point1, point2);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint1 = point1;
                closestPoint2 = point2;
                // edge index is point index - 1
                edgeIndex = i - 1;
            }
        }

        // assert index correctness
        Assert.IsTrue(edgeIndex >= (int)EdgeIndex.RightTop && edgeIndex <= (int)EdgeIndex.Top);
        return (closestPoint1, closestPoint2, (EdgeIndex)edgeIndex);
    }

    /// <summary>
    /// Utility struct used during link process.
    /// See LinkWithNeighbours.
    /// </summary>
    protected struct Link
    {
        public EdgeIndex neighbourEdge;
        public GameObject neighbour;
    }
    #endregion

    #region Editor
#if UNITY_EDITOR
    protected void DrawCollider()
    {
        PolygonCollider2D collider = GetComponent<PolygonCollider2D>();
        Vector2[] points = collider.points;
        int pointsLength = points.Length;

        // transform all points once
        Vector2[] transformedPoints = new Vector2[pointsLength];
        for (int i = 0; i < pointsLength; i++)
        {
            transformedPoints[i] = collider.transform.TransformPoint(points[i]);
        }

        Vector2 point1;
        Vector2 point2;
        for (int i = 1; i <= pointsLength; i++)
        {
            if (i == pointsLength)
            {
                point1 = transformedPoints[pointsLength - 1];
                point2 = transformedPoints[0];
                DebugUtils.DrawLine(point1, point2, Color.green);
                break;
            }
            else
            {
                point1 = transformedPoints[i - 1];
                point2 = transformedPoints[i];

                DebugUtils.DrawLine(point1, point2, Color.green);
            }
        }
    }

    /// <summary>
    /// Draws attachment rays from block position to all edge directions of edgeAttachPositionOffset length.
    /// </summary>
    protected void DrawEdgeAttachmentRays()
    {
        foreach (KeyValuePair<EdgeIndex, Vector2> entry in EdgeOffsets)
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(entry.Value).normalized * edgeAttachPositionOffset, Color.yellow);
        }
    }

    /// <summary>
    /// Draws neighbour link rays from block position to all edge directions of neighbourRange length.
    /// </summary>
    protected void DrawNeighbourLinkRays()
    {
        foreach (KeyValuePair<EdgeIndex, Vector2> entry in EdgeOffsets)
        {
            Vector2 start = transform.position + transform.TransformDirection(entry.Value);
            Vector2 dir = ((Vector2)transform.position - start).normalized * neighbourRange;
            Debug.DrawRay(start, dir, Color.red);
        }
    }
#endif // UNITY_EDITOR
    #endregion

    /// <summary>
    /// Gets linked neighbour (if any).
    /// </summary>
    /// <param name="link">Linked joint.</param>
    /// <returns>Null or a neighbour as GameObject.</returns>
    private GameObject GetLinkNeighbour(AnchoredJoint2D link)
    {
        if (link == null)
        {
            return null;
        }

        if (link.gameObject == gameObject)
        {
            return link.connectedBody.gameObject;
        }

        return link.gameObject;
    }

    /// <summary>
    /// Gets gravity direction, constantly applied in UpdateFixed.
    /// </summary>
    /// <returns>Normalized gravity vector.</returns>
    protected Vector2 GetGravityDirection()
    {
        return (gravityPoint - (Vector2)transform.position).normalized;
    }

    /// <summary>
    /// Removes link (joint) from the block links.
    /// </summary>
    /// <param name="linkToUnlink"></param>
    protected void Unlink(AnchoredJoint2D linkToUnlink)
    {
        foreach (EdgeIndex edge in links.Keys.ToArray())
        {
            AnchoredJoint2D link = links[edge];
            if (link == null)
            {
                continue;
            }

            if (link == linkToUnlink)
            {
                links[edge] = null;
                // each edge has individual links
                // break when found and removed
                break;
            }
        }
    }

    protected virtual void Awake()
    {
        blocksLayer = LayerMask.NameToLayer("blocks");
        GameObject obj = GameObject.FindGameObjectWithTag("obstructions_tilemap");
        obstructionsTilemap = obj?.GetComponent<Tilemap>();
    }

    protected virtual void Update()
    {
#if UNITY_EDITOR
        DrawCollider();

        DrawEdgeAttachmentRays();

        DrawNeighbourLinkRays();
#endif

        // attached blocks check if they collide with obstruction
        // that can't be done in OnCollisionEnter2D as attached
        // objects are static
        if ((obstructionsTilemap != null) && attached)
        {
            // Convert world position to tile coordinates
            Vector3Int tilePos = obstructionsTilemap.WorldToCell(transform.position);
            // Get the tile at that position
            TileBase tile = obstructionsTilemap.GetTile(tilePos);
            // any non null tile is an obstruction
            if (tile != null)
            {
                // a more precise approach would be to check
                // tile collider against our collider
                // but to save on perf. we go simple way
                // and consider this block as collided
                LevelManager.Instance.OnBlocksObstructionCollision(gameObject);
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        // as long as the block is not attached apply gravitation force
        // object's physics mass is not discarded
        if (!attached)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            rb.AddForce(GetGravityDirection() * GravityStrength, ForceMode2D.Force);
        }
    }

    /// <summary>
    /// Processes collisions with other non static/kinematic rigidbodies.
    /// </summary>
    /// <param name="other"></param>
    protected virtual void OnCollisionEnter2D(Collision2D other)
    {
        // store origin parent for possible restoration
        Transform initialParent = transform.parent;
        Vector3 initialPosition = transform.position;
        Quaternion initialRotation = transform.rotation;

        if (attached)
        {
            // don't need to process already attached ones
            return;
        }
        else if (other.gameObject.layer != blocksLayer)
        {
            // don't process if the other block is also a "floating" blocks
            return;
        }

        // add to all blocks layer
        transform.parent = other.transform.parent;

        // find this block closest edge
        (Vector2 point1, Vector2 point2, EdgeIndex edgeIdx) thisEdge = FindClosestColliderEdge(GetComponent<PolygonCollider2D>(), other.collider.ClosestPoint(transform.position));
        DebugUtils.DrawLine(thisEdge.point1, thisEdge.point2, Color.red, 3);

        (Vector2 point1, Vector2 point2, EdgeIndex edgeIdx) otherEdge = FindClosestColliderEdge(other.gameObject.GetComponent<PolygonCollider2D>(), other.collider.ClosestPoint(transform.position));
        DebugUtils.DrawLine(otherEdge.point2, otherEdge.point2, Color.green, 3);

        // check if other block's edge is free
        // skip collision processing if it is not
        if (other.gameObject.GetComponent<BasicBlock>().GetNeighbour(otherEdge.edgeIdx) != null)
        {
            transform.parent = initialParent;
            return;
        }

        // get midpoints
        Vector2 thisEdgeMidpoint = (thisEdge.point1 + thisEdge.point2) / 2;
        Vector2 otherEdgeMidpoint = (otherEdge.Item1 + otherEdge.Item2) / 2;

#if DEBUG && UNITY_EDITOR
        (_, _, EdgeIndex idx) = FindClosestColliderEdge(other.gameObject.GetComponent<PolygonCollider2D>(), otherEdgeMidpoint);
        Logger.Debug("side (our) " + thisEdge.edgeIdx + " their " + otherEdge.edgeIdx + " computed " + idx);
#endif // DEBUG && UNITY_EDITOR

        // compute and apply rotation between 2 edge midpoints
        Vector2 dir1 = ((Vector2)transform.position - thisEdgeMidpoint).normalized;
        Vector2 dir2 = (otherEdgeMidpoint - (Vector2)other.gameObject.transform.position).normalized;
        Quaternion rotation = Quaternion.FromToRotation(dir1, dir2);
        transform.rotation *= rotation;

        // float angle11 = Mathf.Atan2(dir1.y, dir1.x) * Mathf.Rad2Deg;
        // float angle22 = Mathf.Atan2(dir2.y, dir2.x) * Mathf.Rad2Deg;
        // float rotationDifference = angle22 - angle11;
        // Logger.Debug("rotationDifference " + rotationDifference);
        // transform.Rotate(0, 0, rotationDifference);

        // set correct position
        transform.position = (Vector2)other.gameObject.transform.position + dir2 * edgeAttachPositionOffset;

        if (LinkWithNeighbours(blocksLayer) == 0)
        {
            // not able to attach, revert block state
            transform.parent = initialParent;
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            return;
        }

        gameObject.layer = blocksLayer;

        // prevent physics to have an effect on the object
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        GetComponent<Rigidbody2D>().totalForce = Vector2.zero;

        // mark as attached
        attached = true;

        Light2D light = GetComponent<Light2D>();
        if (light != null)
        {
            Destroy(light);
        }

        ParticleSystem efx = NewAttachEfx();
        efx.Play();

        // run match check and scoring
        LevelManager.Instance.OnBlocksAttach(gameObject);
    }

    /// <summary>
    /// Gets transformed edge offset point.
    /// </summary>
    /// <param name="edge">Edge index.</param>
    /// <returns></returns>
    public Vector2 GetEdgeOffset(EdgeIndex edge)
    {
        return transform.TransformDirection(EdgeOffsets[edge]).normalized * edgeAttachPositionOffset;
    }

    /// <summary>
    /// Gets neighbour attached to provided edge.
    /// </summary>
    /// <param name="edge">Edge index.</param>
    /// <returns>Null or a neighbour as GameObject.</returns>
    public GameObject GetNeighbour(EdgeIndex edge)
    {
        return GetLinkNeighbour(links[edge]);
    }

    public void Destroy()
    {
        foreach (EdgeIndex edge in links.Keys.ToArray())
        {
            AnchoredJoint2D link = links[edge];
            if (link == null)
            {
                continue;
            }

            // remove the link from connected block
            GameObject other = GetLinkNeighbour(link);
            other?.GetComponent<BasicBlock>().Unlink(link);

            // remove it from this block
            links[edge] = null;

            // destroy the link object
            Destroy(link);
        }

        destroyed = true;

        ParticleSystem efx = NewDestroyEfx();
        if (efx != null)
        {
            efx.Play();
        }
    }

    #region General Overrides
    public virtual ParticleSystem NewDestroyEfx()
    {
        return null;
    }

    public virtual ParticleSystem NewAttachEfx()
    {
        return null;
    }

    public virtual AudioClip SfxOnAttach()
    {
        return null;
    }

    public virtual bool MatchesWith(GameObject obj)
    {
        // intentionally blank
        return false;
    }
    #endregion

    /// <summary>
    /// Links the block against other blocks within provided layer.
    ///
    /// Performs inverse raycast to all edge directions.
    /// If ray hits another block, then two blocks are jointed/linked.
    ///
    /// However, if the edge is not free or it is not a block, nothing
    /// is returned.
    ///
    /// Shall be done only after the correct block placement.
    ///
    /// </summary>
    /// <param name="layer">Layer tused for raycasting, shall contain only blocks.</param>
    /// <returns>Number of linked neighbours.</returns>
    public int LinkWithNeighbours(int layer)
    {
        // as the block is now placed, check neighbours
        // add ourselves (this block) to all of them
        // however, when something prevents us from doing so
        // don't collide...
        // that prevents possible attachment when block collides
        // with antagonistic neighbour side that doesn't have a neighbour set
        // draw directions
        Dictionary<EdgeIndex, Link> neighbours = new Dictionary<EdgeIndex, Link>(EdgeOffsets.Count);
        foreach (KeyValuePair<EdgeIndex, Vector2> entry in EdgeOffsets)
        {
            // perform inverse raycast, that avoids hit & stop due to our collider boundary
            Vector2 start = transform.position + transform.TransformDirection((Vector3)entry.Value);
            Vector2 dir = ((Vector2)transform.position - start).normalized * neighbourRange;
            Debug.DrawRay(start, dir, Color.yellow, 2);

            RaycastHit2D hit = Physics2D.Raycast(start, dir, neighbourRange, 1 << layer);
            Logger.Debug($"raycast from {start} dir {dir} len {neighbourRange}");

            if (!hit.collider)
            {
                continue;
            }

            Logger.Debug($"hit {hit.collider.gameObject.name}");
            DebugUtils.DrawLine(start, hit.point, Color.red, 4);

            GameObject hitObj = hit.collider.gameObject;
            BasicBlock neighbour = hitObj.GetComponent<BasicBlock>();
#if DEBUG
            if (!neighbour)
            {
                // not a block
                Logger.Debug("unexpected object in blocks layer");
            }
#endif
            PolygonCollider2D neighbourCollider = hitObj.GetComponent<PolygonCollider2D>();

            (Vector2 closestPoint1, Vector2 closestPoint2, EdgeIndex edgeIdx) neighbourEdge = FindClosestColliderEdge(neighbourCollider, hit.collider.ClosestPoint(transform.position));

            if (neighbour.links[neighbourEdge.edgeIdx] != null)
            {
                // edge is occupied, stop processing to prevent invalid state
                neighbours.Clear();
                break;
            }

            if (neighbours.ContainsKey(entry.Key))
            {
#if DEBUG
                // shall not happen
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }
#endif
                Logger.Debug("unexpected edge conflict on collision");
                neighbours.Clear();
                break;
            }

            neighbours.Add(entry.Key, new Link
            {
                neighbourEdge = neighbourEdge.edgeIdx,
                neighbour = hit.collider.gameObject
            });
        }

        // attach / link
        foreach (KeyValuePair<EdgeIndex, Link> entry in neighbours)
        {
            GameObject neighbour = entry.Value.neighbour;
            FixedJoint2D joint = neighbour.AddComponent<FixedJoint2D>();

            joint.connectedBody = GetComponent<Rigidbody2D>();
            joint.breakAction = JointBreakAction2D.Ignore;
            // joint.breakAction = JointBreakAction2D.Destroy;
            // joint.breakForce = 100;
            // joint.breakTorque = 50;
            joint.dampingRatio = 1.0f;
            joint.frequency = 1;

            neighbour.GetComponent<BasicBlock>().links[entry.Value.neighbourEdge] = joint;
            links[entry.Key] = joint;
        }

        return neighbours.Count;
    }
}