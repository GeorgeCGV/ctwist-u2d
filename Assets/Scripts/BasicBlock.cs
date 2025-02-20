using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

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

    public static EdgeIndex GetRandomEdge()
    {
        Array edges = Enum.GetValues(typeof(EdgeIndex));
        return (EdgeIndex)edges.GetValue(UnityEngine.Random.Range(0, edges.Length));
    }

    public static Dictionary<EdgeIndex, Vector2> EdgeDirections = new Dictionary<EdgeIndex, Vector2>()
    {
        {EdgeIndex.RightTop, new Vector2(.35f, .2f)},
        {EdgeIndex.RightBottom, new Vector2(.35f, -.2f)},
        {EdgeIndex.Bottom, new Vector2(.0f, -.4f)},
        {EdgeIndex.LeftBottom, new Vector2(-.35f, -.2f)},
        {EdgeIndex.LeftTop, new Vector2(-.35f, .2f)},
        {EdgeIndex.Top, new Vector2(.0f, .4f)},
    };

    public float neighbourRange = .36f;

    public float edgeAttachPositionOffset = .85f;

    public Vector2 targetPoint = Vector2.zero;

    public float startForce = 1.0f;
    public float stepForce = float.NaN;

    public bool attached = false;
    public bool destroyed = false;

    private Tilemap obstructionsTilemap;

    void Awake()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("obstructions_tilemap");
        Assert.IsNotNull(obj);
        obstructionsTilemap = obj.GetComponent<Tilemap>();
        Assert.IsNotNull(obstructionsTilemap);
    }

    public static float DistancePointToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 lineDirection = lineEnd - lineStart;
        float lineLengthSquared = lineDirection.sqrMagnitude;

        // if line segment is a point
        if (lineLengthSquared == 0.0f)
        {
            return Vector2.Distance(point, lineStart);
        }

        // project point onto the line segment, clamped to [0, 1]
        float t = Mathf.Clamp01(Vector2.Dot(point - lineStart, lineDirection) / lineLengthSquared);
        Vector2 projection = lineStart + t * lineDirection;

        // distance from the point to the projection
        return Vector2.Distance(point, projection);
    }

    public static (Vector2, Vector2, EdgeIndex) FindClosestEdge(PolygonCollider2D collider, Vector2 point)
    {
        float minDistance = float.MaxValue;
        (Vector2, Vector2) closestEdge = (Vector2.zero, Vector2.zero);

        int edgeIndex = -1;
        Vector2[] points = collider.points;
        (Vector2, Vector2) edge;
        for (int i = 1; i <= points.Length; i++)
        {
            if (i == points.Length)
            {
                edge = (collider.transform.TransformPoint(points[points.Length - 1]), collider.transform.TransformPoint(points[0]));
            }
            else
            {
                edge = (collider.transform.TransformPoint(points[i - 1]), collider.transform.TransformPoint(points[i]));
            }

            // float distance = HandleUtility.DistancePointLine(point, edge.Item1, edge.Item2);
            float distance = DistancePointToLineSegment(point, edge.Item1, edge.Item2);
            // Debug.Log($"DST {distance}, {distance2}");
            // if (distance != distance2) {
            //     Debug.DebugBreak();
            // }

            if (distance < minDistance)
            {
                minDistance = distance;
                closestEdge = edge;
                // edge index is point index - 1
                edgeIndex = i - 1;
            }
        }

        // assert index correctness
        Assert.IsTrue(edgeIndex >= (int)EdgeIndex.RightTop && edgeIndex <= (int)EdgeIndex.Top);

        return (new Vector2(closestEdge.Item1.x, closestEdge.Item1.y), new Vector2(closestEdge.Item2.x, closestEdge.Item2.y), (EdgeIndex)edgeIndex);
    }

    public Vector3 GetEdgeOffset(EdgeIndex edge)
    {
        Vector2 dir = EdgeDirections[edge].normalized;
        return dir * edgeAttachPositionOffset;
    }

    protected Vector2 GetTargetPointDirection()
    {
        // Vector3 AB = B - A. Destination - Origin.
        return (targetPoint - (Vector2)transform.position).normalized;
    }

    public SerializedDictionary<EdgeIndex, AnchoredJoint2D> links = new SerializedDictionary<EdgeIndex, AnchoredJoint2D>()
    {
        {EdgeIndex.RightTop, null},
        {EdgeIndex.RightBottom, null},
        {EdgeIndex.Bottom, null},
        {EdgeIndex.LeftBottom, null},
        {EdgeIndex.LeftTop, null},
        {EdgeIndex.Top, null},
    };

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

    public virtual void Unlink(AnchoredJoint2D linkToUnlink)
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

    public virtual void Destroy()
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
    }

    public virtual bool MatchesWith(GameObject obj)
    {
        // intentionally blank
        return false;
    }

    public virtual GameObject GetNeighbour(EdgeIndex edge)
    {
        return GetLinkNeighbour(links[edge]);
    }

    protected virtual void Start()
    {
        if (!float.IsNaN(startForce))
        {
            GetComponent<Rigidbody2D>().AddForce(GetTargetPointDirection() * startForce);
            GetComponent<Rigidbody2D>().AddTorque(UnityEngine.Random.Range(-10, 11), ForceMode2D.Force);
        }
    }

    protected virtual void Update()
    {

        // attached blocks shall check if they collide with obstruction
        if (attached)
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

        // intentionally blank
        foreach (KeyValuePair<EdgeIndex, Vector2> entry in EdgeDirections)
        {
            Vector2 start = transform.position + transform.TransformDirection((Vector3)entry.Value);
            Vector2 dir = ((Vector2)transform.position - start).normalized * neighbourRange;
            Debug.DrawRay(start, dir, Color.red);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!attached)
        {
            if (!float.IsNaN(stepForce))
            {
                GetComponent<Rigidbody2D>().AddForce(GetTargetPointDirection() * stepForce);
            }
        }
    }

    public int LinkWithNeighbours(int layer)
    {
        // as the block is now placed, check neighbours
        // add ourselves (this block) to all of them
        // however, when something prevents us from doing so
        // don't collide...
        // that prevents possible attachment when block collides
        // with antagonistic neighbour side that doesn't have a neighbour set
        // draw directions

        Dictionary<EdgeIndex, Link> neighbours = new Dictionary<EdgeIndex, Link>(EdgeDirections.Count);
        foreach (KeyValuePair<EdgeIndex, Vector2> entry in EdgeDirections)
        {
            // perform inverse raycast, that avoids hit & stop due to our collider boundary
            // Vector2 start = transform.position + (transform.TransformDirection((Vector3)entry.Value) * neighbourRange);
            // Vector2 dir = ((Vector2)transform.position - start).normalized;

            Vector2 start = transform.position + transform.TransformDirection((Vector3)entry.Value);
            Vector2 dir = ((Vector2)transform.position - start).normalized * neighbourRange;
            Debug.DrawRay(start, dir, Color.yellow, 2);

            RaycastHit2D hit = Physics2D.Raycast(start, dir, neighbourRange, 1 << layer);
            Debug.Log("raycast from: " + start.ToString() + " dir: " + dir.ToString() + " len: " + neighbourRange);
            if (hit.collider != null)
            {
                Debug.Log("hit: " + hit.collider.gameObject.name);
                Debug.DrawLine(start, hit.point, Color.red, 4);

                var neighbourClosestEdge = FindClosestEdge(hit.collider.gameObject.GetComponent<PolygonCollider2D>(),
                                                    hit.collider.ClosestPoint(transform.position));
                var neighbourEdge = neighbourClosestEdge.Item3;
                var neighbourBlock = hit.collider.gameObject.GetComponent<BasicBlock>();
                if (neighbourBlock.links[neighbourEdge] != null)
                {
                    // occupied
                    // can't attack, something else takes the place, revert
                    neighbours.Clear();
                    break;
                }

                if (neighbours.ContainsKey(entry.Key))
                {
                    // shall not happen
#if DEBUG
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Diagnostics.Debugger.Break();
                    }
#endif
                    Debug.Log("unexpected edge conflic on collision");
                    neighbours.Clear();
                    break;
                }

                neighbours.Add(entry.Key, new Link
                {
                    neighbourEdge = neighbourEdge,
                    neighbour = hit.collider.gameObject
                });
            }
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

    /// <summary>
    /// Utility struct used during link process.
    /// </summary>
    protected struct Link
    {
        public EdgeIndex neighbourEdge;
        public GameObject neighbour;
    }
}