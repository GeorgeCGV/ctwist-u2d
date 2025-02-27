using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using Utils;
using static Model.BlockType;
using Debug = UnityEngine.Debug;

namespace Blocks
{
    /// <summary>
    /// Base game element - Block.
    /// </summary>
    /// <remarks>
    /// All other block types extend this one.
    /// The class provides collision, linkage handling,
    /// provides general block behaviour, and
    ///  defines methods for overrides.
    /// </remarks>
    [ExecuteInEditMode]
    [RequireComponent(typeof(PolygonCollider2D))]
    public class BasicBlock : MonoBehaviour
    {
        public enum EdgeIndex
        {
            RightTop = 0,
            RightBottom = 1,
            Bottom = 2,
            LeftBottom = 3,
            LeftTop = 4,
            Top = 5,
        };

        private static readonly Dictionary<EdgeIndex, Vector2> EdgeOffsets = new(6)
        {
            { EdgeIndex.RightTop, new Vector2(0.35f, 0.2f) },
            { EdgeIndex.RightBottom, new Vector2(0.35f, -0.2f) },
            { EdgeIndex.Bottom, new Vector2(0.0f, -0.4f) },
            { EdgeIndex.LeftBottom, new Vector2(-.35f, -0.2f) },
            { EdgeIndex.LeftTop, new Vector2(-0.35f, 0.2f) },
            { EdgeIndex.Top, new Vector2(0.0f, 0.4f) },
        };

        [SerializeField] protected float neighbourRange = 0.36f;

        [SerializeField] protected float edgeAttachPositionOffset = 0.85f;

        [SerializeField] protected Vector2 gravityPoint = Vector2.zero;

        [FormerlySerializedAs("GravityStrength")] [SerializeField]
        public float gravityStrength = 1.0f;

        /// <summary>
        /// Maps block's <see cref="EdgeIndex"/> to its corresponding <see cref="AnchoredJoint2D"/>.
        /// </summary>
        /// <remarks>
        /// Edge that is connected to another block won't be <c>null</c>.
        /// </remarks>
        private readonly Dictionary<EdgeIndex, AnchoredJoint2D> _links = new(6)
        {
            { EdgeIndex.RightTop, null },
            { EdgeIndex.RightBottom, null },
            { EdgeIndex.Bottom, null },
            { EdgeIndex.LeftBottom, null },
            { EdgeIndex.LeftTop, null },
            { EdgeIndex.Top, null },
        };

        /// <summary>
        /// Setter and Getter for the block type.
        /// </summary>
        /// <value>EBlockType</value>
        public EBlockType BlockType { get; protected set; }

        /// <summary>
        /// Attached flag.
        /// Is set to True when block is attached to another block
        /// that leads to central block.
        /// </summary>
        public bool attached;

        /// <summary>
        /// Destroyed flag.
        /// Is set to True when block is marked for destruction.
        /// </summary>
        public bool Destroyed { get; private set; }

        /// <summary>
        /// Possible obstruction tilemap present in the level.
        /// Used here to detect collisions against it.
        /// </summary>
        private Tilemap _obstructionsTilemap;

        /// <summary>
        /// Layer where all attached blocks are.
        /// </summary>
        private int _blocksLayer;

        #region Helpers

        /// <summary>
        /// Finds closes collider edge to provided point.
        /// </summary>
        /// <param name="collider">Block's collider.</param>
        /// <param name="point">Point.</param>
        /// <returns>Edge points and index.</returns>
        private static (Vector2 point1, Vector2 point2, EdgeIndex edgeIdx) FindClosestColliderEdge(
            PolygonCollider2D collider, Vector2 point)
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

                float distance = Helpers.DistancePointToLineSegment(point, point1, point2);
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
            Assert.IsTrue(edgeIndex is >= (int)EdgeIndex.RightTop and <= (int)EdgeIndex.Top);
            return (closestPoint1, closestPoint2, (EdgeIndex)edgeIndex);
        }

        /// <summary>
        /// Utility struct used during link process.
        /// See LinkWithNeighbours.
        /// </summary>
        private struct Linkage
        {
            public EdgeIndex NeighbourEdge;
            public GameObject Neighbour;
        }

        #endregion Helpers

        #region Editor
        
#if UNITY_EDITOR
        
        protected void DrawCollider()
        {
            PolygonCollider2D polyCollider = GetComponent<PolygonCollider2D>();
            Vector2[] points = polyCollider.points;
            int pointsLength = points.Length;

            // transform all points once
            Vector2[] transformedPoints = new Vector2[pointsLength];
            for (int i = 0; i < pointsLength; i++)
            {
                transformedPoints[i] = polyCollider.transform.TransformPoint(points[i]);
            }

            Vector2 point1;
            Vector2 point2;
            for (int i = 1; i <= pointsLength; i++)
            {
                if (i == pointsLength)
                {
                    point1 = transformedPoints[pointsLength - 1];
                    point2 = transformedPoints[0];
                    Debug.DrawLine(point1, point2, Color.green);
                    break;
                }

                point1 = transformedPoints[i - 1];
                point2 = transformedPoints[i];

                Debug.DrawLine(point1, point2, Color.green);
            }
        }

        /// <summary>
        /// Draws attachment rays from block position to all edge directions of edgeAttachPositionOffset length.
        /// </summary>
        protected void DrawEdgeAttachmentRays()
        {
            foreach (KeyValuePair<EdgeIndex, Vector2> entry in EdgeOffsets)
            {
                Debug.DrawRay(transform.position,
                    transform.TransformDirection(entry.Value).normalized * edgeAttachPositionOffset, Color.yellow);
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
        
        #endregion Editor

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
        private Vector2 GetGravityDirection()
        {
            return (gravityPoint - (Vector2)transform.position).normalized;
        }

        /// <summary>
        /// Removes link (joint) from the block links.
        /// </summary>
        /// <param name="linkToUnlink"></param>
        private void Unlink(AnchoredJoint2D linkToUnlink)
        {
            EdgeIndex[] edges = _links.Keys.ToArray();
            foreach (EdgeIndex edge in edges)
            {
                AnchoredJoint2D link = _links[edge];
                if (link == null)
                {
                    continue;
                }

                if (link == linkToUnlink)
                {
                    _links[edge] = null;
                    // each edge has individual links
                    // break when found and removed
                    break;
                }
            }
        }

        /// <summary>
        /// Sets provided joint as edge link.
        /// </summary>
        /// <param name="edgeIdx">Edge index.</param>
        /// <param name="joint">AnchoredJoint2D</param>
        private void Link(EdgeIndex edgeIdx, AnchoredJoint2D joint)
        {
            _links[edgeIdx] = joint;
        }

        /// <summary>
        /// Gets a link at provided edge.
        /// </summary>
        /// <param name="edgeIdx">Edge index.</param>
        /// <returns>AnchoredJoint2D or null.</returns>
        private AnchoredJoint2D Link(EdgeIndex edgeIdx)
        {
            return _links[edgeIdx];
        }

        /// <summary>
        /// Returns the amount of links this block has.
        /// </summary>
        /// <returns>Amount of links.</returns>
        public int LinksCount()
        {
            int ret = 0;
            foreach (AnchoredJoint2D joint in _links.Values)
            {
                if (joint != null)
                {
                    ret++;
                }
            }

            return ret;
        }
        
        #region Block Overrides

        /// <summary>
        /// Used to instantiate new EFX when block is destroyed.
        /// </summary>
        /// <remarks>
        /// Different type of blocks may have different destroy EFX.
        /// </remarks>
        /// <returns>ParticleSystem.</returns>
        protected virtual ParticleSystem NewDestroyEfx()
        {
            return null;
        }

        /// <summary>
        /// Used to instantiate new EFX when block attaches.
        /// </summary>
        /// <remarks>
        /// Different type of blocks may have different attach EFX.
        /// </remarks>
        /// <returns>ParticleSystem.</returns>
        protected virtual ParticleSystem NewAttachEfx()
        {
            return null;
        }

        /// <summary>
        /// Used to play SFX when block attaches.
        /// </summary>
        /// <remarks>
        /// Different type of blocks may have different attach SFX.
        /// </remarks>
        /// <returns>AudioClip.</returns>
        public virtual AudioClip SfxOnAttach()
        {
            return null;
        }

        /// <summary>
        /// Check if the block matches with another block.
        /// </summary>
        /// <remarks>
        /// Different type of blocks may have different match rules.
        /// </remarks>
        /// <param name="block">Another block.</param>
        /// <returns>True if matches, otherwise False.</returns>
        public virtual bool MatchesWith(GameObject block)
        {
            // intentionally blank
            return false;
        }

        #endregion Block Overrides

        #region Unity
        
        /// <summary>
        /// Basic setup to get required level references.
        /// </summary>
        /// <remarks>
        /// Requires "block" layer for collisions and
        /// obstructions tilemap that must have "obstructions_tilemap" tag.
        /// The tilemap is optional, as not all levels have it.
        /// </remarks>
        protected virtual void Awake()
        {
            _blocksLayer = LayerMask.NameToLayer("blocks");
            GameObject obj = GameObject.FindGameObjectWithTag("obstructions_tilemap");
            _obstructionsTilemap = obj?.GetComponent<Tilemap>();
        }

        /// <summary>
        /// Performs obstruction tilemap collision checks.
        /// </summary>
        /// <remarks>
        /// Only if <c>obstructionTilemap</c> is not <c>null</c>
        /// and the block is attached.
        /// Also draws helpful debug information (collider, attachment and link rays).
        /// </remarks>
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
            if ((_obstructionsTilemap != null) && attached)
            {
                // Convert world position to tile coordinates
                Vector3Int tilePos = _obstructionsTilemap.WorldToCell(transform.position);
                // Get the tile at that position
                TileBase tile = _obstructionsTilemap.GetTile(tilePos);
                // any non-null tile is an obstruction
                if (tile != null)
                {
                    // a more precise approach would be to check
                    // tile collider against our collider
                    // but to save on perf. we go simple way
                    // and consider this block as collided
                    LevelManager.Instance.OnBlocksObstructionCollision(this);
                }
            }
        }

        /// <summary>
        /// Applies world gravity.
        /// </summary>
        /// <remarks>
        /// As long as the block is not <c>attached</c> it
        /// gets gravitational force <c>GravityStrength</c>
        /// towards <c>gravityPoint</c>.
        /// </remarks>
        protected virtual void FixedUpdate()
        {
            // as long as the block is not attached apply gravitation force
            // object's physics mass is not discarded
            if (!attached)
            {
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                rb.AddForce(GetGravityDirection() * gravityStrength, ForceMode2D.Force);
            }
        }

        /// <summary>
        /// Processes collisions with other non-static/kinematic rigidbodies.
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
            else if (other.gameObject.layer != _blocksLayer)
            {
                // don't process if the other block is also a "floating" blocks
                return;
            }

            // add to all blocks layer
            transform.parent = other.transform.parent;

            // find this block closest edge
            (Vector2 point1, Vector2 point2, EdgeIndex edgeIdx) thisEdge =
                FindClosestColliderEdge(GetComponent<PolygonCollider2D>(),
                    other.collider.ClosestPoint(transform.position));
            Debug.DrawLine(thisEdge.point1, thisEdge.point2, Color.red, 3);

            (Vector2 point1, Vector2 point2, EdgeIndex edgeIdx) otherEdge =
                FindClosestColliderEdge(other.gameObject.GetComponent<PolygonCollider2D>(),
                    other.collider.ClosestPoint(transform.position));
            Debug.DrawLine(otherEdge.point2, otherEdge.point2, Color.green, 3);

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

#if ENABLE_LOGS
            (_, _, EdgeIndex idx) =
                FindClosestColliderEdge(other.gameObject.GetComponent<PolygonCollider2D>(), otherEdgeMidpoint);
            Logger.Debug("side (our) " + thisEdge.edgeIdx + " their " + otherEdge.edgeIdx + " computed " + idx);
#endif // ENABLE_LOGS

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

            if (LinkWithNeighbours(_blocksLayer) == 0)
            {
                // not able to attach, revert block state
                transform.parent = initialParent;
                transform.position = initialPosition;
                transform.rotation = initialRotation;
                return;
            }

            gameObject.layer = _blocksLayer;

            // prevent physics to have an effect on the object
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            GetComponent<Rigidbody2D>().totalForce = Vector2.zero;

            // mark as attached
            attached = true;

            Light2D lightComponent = GetComponent<Light2D>();
            if (lightComponent != null)
            {
                Destroy(lightComponent);
            }

            ParticleSystem efx = NewAttachEfx();
            efx.Play();

            // run match check and scoring
            LevelManager.Instance.OnBlocksAttach(this);
        }
        
        #endregion Unity

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
            return GetLinkNeighbour(_links[edge]);
        }

        public void DestroyBlock(bool withEfx = true)
        {
            EdgeIndex[] edges = _links.Keys.ToArray();
            foreach (EdgeIndex edge in edges)
            {
                AnchoredJoint2D link = _links[edge];
                if (link == null)
                {
                    continue;
                }

                // remove the link from connected block
                GameObject other = GetLinkNeighbour(link);
                other?.GetComponent<BasicBlock>().Unlink(link);

                // remove it from this block
                _links[edge] = null;

                // destroy the link object
                Destroy(link);
            }

            Destroyed = true;

            if (withEfx)
            {
                ParticleSystem efx = NewDestroyEfx();
                if (efx != null)
                {
                    efx.Play();
                }
            }
        }

        /// <summary>
        /// Links the block against other blocks within provided layer.
        ///
        /// Performs inverse raycast to all edge directions.
        /// If ray hits another block, then two blocks are jointed/linked.
        ///
        /// However, if the edge is not free, or it is not a block, nothing
        /// is returned.
        ///
        /// Shall be done only after the correct block placement.
        ///
        /// </summary>
        /// <param name="layer">Layer used for ray-casting, shall contain only blocks.</param>
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
            Dictionary<EdgeIndex, Linkage> neighbours = new Dictionary<EdgeIndex, Linkage>(EdgeOffsets.Count);
            foreach (KeyValuePair<EdgeIndex, Vector2> entry in EdgeOffsets)
            {
                // perform inverse raycast, that avoids hit & stop due to our collider boundary
                Vector2 start = transform.position + transform.TransformDirection(entry.Value);
                Vector2 dir = ((Vector2)transform.position - start).normalized * neighbourRange;
                Debug.DrawRay(start, dir, Color.yellow, 2);

                RaycastHit2D hit = Physics2D.Raycast(start, dir, neighbourRange, 1 << layer);
                Logger.Debug($"raycast from {start} dir {dir} len {neighbourRange}");

                if (!hit.collider)
                {
                    continue;
                }

                Logger.Debug($"hit {hit.collider.gameObject.name}");
                Debug.DrawLine(start, hit.point, Color.red, 4);

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

                (Vector2 closestPoint1, Vector2 closestPoint2, EdgeIndex edgeIdx) neighbourEdge =
                    FindClosestColliderEdge(neighbourCollider, hit.collider.ClosestPoint(transform.position));

                if (neighbour.Link(neighbourEdge.edgeIdx) != null)
                {
                    // edge is occupied, stop processing to prevent invalid state
                    neighbours.Clear();
                    break;
                }

                if (neighbours.ContainsKey(entry.Key))
                {
#if DEBUG
                    // shall not happen
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }
#endif
                    Logger.Debug("unexpected edge conflict on collision");
                    neighbours.Clear();
                    break;
                }

                neighbours.Add(entry.Key, new Linkage
                {
                    NeighbourEdge = neighbourEdge.edgeIdx,
                    Neighbour = hit.collider.gameObject
                });
            }

            // attach / link
            foreach (KeyValuePair<EdgeIndex, Linkage> entry in neighbours)
            {
                GameObject neighbour = entry.Value.Neighbour;
                FixedJoint2D joint = neighbour.AddComponent<FixedJoint2D>();

                joint.connectedBody = GetComponent<Rigidbody2D>();
                joint.breakAction = JointBreakAction2D.Ignore;
                joint.dampingRatio = 1.0f;
                joint.frequency = 1;

                neighbour.GetComponent<BasicBlock>().Link(entry.Value.NeighbourEdge, joint);
                _links[entry.Key] = joint;
            }

            return neighbours.Count;
        }
    }
}