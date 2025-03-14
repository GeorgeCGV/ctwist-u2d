using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Blocks.SpecialProperties;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
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
    [RequireComponent(typeof(PolygonCollider2D), typeof(Rigidbody2D))]
    public abstract class BasicBlock : MonoBehaviour
    {
        public enum EdgeIndex
        {
            RightTop = 0,
            RightBottom = 1,
            Bottom = 2,
            LeftBottom = 3,
            LeftTop = 4,
            Top = 5,
        }
        
        public static readonly EdgeIndex[] EdgeIndexes = (EdgeIndex[])Enum.GetValues(typeof(EdgeIndex));

        private static readonly Dictionary<EdgeIndex, Vector2> EdgeOffsets = new(6)
        {
            { EdgeIndex.RightTop, new Vector2(0.35f, 0.2f) },
            { EdgeIndex.RightBottom, new Vector2(0.35f, -0.2f) },
            { EdgeIndex.Bottom, new Vector2(0.0f, -0.4f) },
            { EdgeIndex.LeftBottom, new Vector2(-.35f, -0.2f) },
            { EdgeIndex.LeftTop, new Vector2(-0.35f, 0.2f) },
            { EdgeIndex.Top, new Vector2(0.0f, 0.4f) },
        };

        [SerializeField]
        protected float neighbourRange = 0.35f;

        [SerializeField]
        protected float edgeAttachPositionOffset = 0.4f;

        [SerializeField]
        protected Vector2 gravityPoint = Vector2.zero;

        [FormerlySerializedAs("GravityStrength")] [SerializeField]
        public float gravityStrength = 1.0f;

        private Rigidbody2D _rigidBody;

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

        private EBlockType _blockType;

        /// <summary>
        /// Setter and Getter for the block type.
        /// </summary>
        /// <value>EBlockType</value>
        public EBlockType BlockType
        {
            get => _blockType;
            protected set
            {
                Light2D lightComponent = GetComponent<Light2D>();
                if (lightComponent != null)
                {
                    lightComponent.color = UnityColorFromType(value);
                }
                _blockType = value;
            }
        }

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
        /// Layer where all attached blocks are.
        /// </summary>
        private int _blocksLayer;

        private IMatchProperty _matchProperty;

        public IMatchProperty MatchProperty
        {
            get => _matchProperty;
            set
            {
                Assert.IsNotNull(value, "matchProperty is null");
                value.Activate(this);
                _matchProperty = value;
            }
        }

        /// <summary>
        /// Processes block's <see cref="IMatchProperty"/> when matched.
        /// </summary>
        /// <returns>Match processing modification.</returns>
        public EMatchPropertyOutcome CheckMatchProperty()
        {
            EMatchPropertyOutcome outcome = EMatchPropertyOutcome.ContinueNormalMatching;
            
            if (_matchProperty != null)
            {
                outcome = _matchProperty.Execute(out bool removeProperty);
                if (removeProperty)
                {
                    Assert.IsFalse(outcome == EMatchPropertyOutcome.SpecialMatchRule, 
                        "Special match rule requires property to be present for ExecuteSpecial");
                    _matchProperty = null;
                }
            }

            return outcome;
        }

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
        private BasicBlock GetLinkNeighbour(AnchoredJoint2D link)
        {
            if (link is null)
            {
                return null;
            }

            if (link.gameObject == gameObject)
            {
                return link.connectedBody.gameObject.GetComponent<BasicBlock>();
            }

            return link.gameObject.GetComponent<BasicBlock>();
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
        /// <remarks>
        /// Doesn't destroy the joint object.
        /// </remarks>
        /// <param name="linkToUnlink"></param>
        private void Unlink(AnchoredJoint2D linkToUnlink)
        {
            EdgeIndex edge;
            for (int i = 0; i < EdgeIndexes.Length; i++)
            {
                edge = EdgeIndexes[i];
                AnchoredJoint2D link = _links[EdgeIndexes[i]];
                if (link is null)
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
                if (joint is not null)
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
        /// <param name="other">Another block.</param>
        /// <returns>True if matches, otherwise False.</returns>
        public virtual bool MatchesWith(BasicBlock other)
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
            _rigidBody = GetComponent<Rigidbody2D>();
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
#if UNITY_EDITOR && DEBUG_BLOCKS
            DrawCollider();
            DrawEdgeAttachmentRays();
            DrawNeighbourLinkRays();
#endif
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
                _rigidBody.AddForce(GetGravityDirection() * gravityStrength, ForceMode2D.Force);
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

            if (other.gameObject.layer != _blocksLayer)
            {
                // don't process if the other block is also a "floating" blocks
                return;
            }

            // add to all blocks layer
            transform.parent = other.transform.parent;

            // find other block closes edge to this block
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
            
            // find this block closest edge
            (Vector2 point1, Vector2 point2, EdgeIndex edgeIdx) thisEdge =
                FindClosestColliderEdge(GetComponent<PolygonCollider2D>(),
                    other.collider.ClosestPoint(transform.position));
            Debug.DrawLine(thisEdge.point1, thisEdge.point2, Color.red, 3);

            // get midpoints
            Vector2 thisEdgeMidpoint = (thisEdge.point1 + thisEdge.point2) / 2;
            Vector2 otherEdgeMidpoint = (otherEdge.point1 + otherEdge.point2) / 2;

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
            _rigidBody.bodyType = RigidbodyType2D.Static;
            _rigidBody.totalForce = Vector2.zero;

            // mark as attached
            attached = true;
            
            ParticleSystem efx = NewAttachEfx();
            efx.transform.position = otherEdgeMidpoint;
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
        /// <returns>Null or a neighbour as <see cref="BasicBlock"/>.</returns>
        public BasicBlock GetNeighbour(EdgeIndex edge)
        {
            return GetLinkNeighbour(_links[edge]);
        }

        /// <summary>
        /// Destroys the block (if not destroyed yet).
        /// </summary>
        /// <remarks>
        /// Unlinks and destroys the joint object between blocks (if present).
        /// </remarks>
        /// <param name="withEfx">To play or not to play the OnDestroy VFX.</param>
        public void DestroyBlock(bool withEfx = true)
        {
            if (Destroyed)
            {
                return;
            }
            
            Destroyed = true;

            EdgeIndex edge;
            for (int i = 0; i < EdgeIndexes.Length; i++)
            {
                edge = EdgeIndexes[i];
                AnchoredJoint2D link = _links[edge];
                if (link is null)
                {
                    continue;
                }

                // remove the link from connected block
                BasicBlock other = GetLinkNeighbour(link);
                other?.Unlink(link);

                // remove it from this block
                _links[edge] = null;

                // destroy the joint object
                Destroy(link);
            }
            
            if (withEfx)
            {
                ParticleSystem efx = NewDestroyEfx();
                if (efx is not null)
                {
                    efx.Play();
                }
            }
            
            Destroy(gameObject);
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
                    continue;
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
                    Neighbour = hitObj
                });
            }

            // attach / link
            foreach (KeyValuePair<EdgeIndex, Linkage> entry in neighbours)
            {
                GameObject neighbour = entry.Value.Neighbour;
                FixedJoint2D joint = neighbour.AddComponent<FixedJoint2D>();

                joint.connectedBody = _rigidBody;
                joint.breakAction = JointBreakAction2D.Ignore;
                joint.dampingRatio = 1.0f;
                joint.frequency = 1;

                neighbour.GetComponent<BasicBlock>().Link(entry.Value.NeighbourEdge, joint);
                _links[entry.Key] = joint;
            }
            
            // disable light
            Light2D lightComponent = GetComponent<Light2D>();
            if (lightComponent != null)
            {
                lightComponent.enabled = false;
            }

            return neighbours.Count;
        }
        
#if UNITY_EDITOR // simple way to extend editor without adding a ton of extra code
    
        public bool toggleProperty;

        public MatchPropertyFactory.EMatchProperty toggleMatchProperty;
        private IMatchProperty _activeProperty;
        
        private void OnValidate()
        {
            if (!toggleProperty)
            {
                return;
            }

            if (_activeProperty == null)
            {
                _activeProperty = MatchPropertyFactory.Instance.NewProperty(toggleMatchProperty);
                _activeProperty.Activate(this);
            }
            else
            {
                EMatchPropertyOutcome outcome = _activeProperty.Execute(out bool removeProperty);
                if (outcome == EMatchPropertyOutcome.SpecialMatchRule)
                {
                    _activeProperty.ExecuteSpecial(this, new ());
                }
                
                if (removeProperty)
                {
                    _activeProperty = null;
                }
            }

            toggleProperty = false;
        }
#endif // UNITY_EDITOR
    }
}