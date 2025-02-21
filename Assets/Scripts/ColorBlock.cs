using System;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Animator))]
public class ColorBlock : BasicBlock
{
    private static int animatorColorIntParam = Animator.StringToHash("Color");

    public static Color red = new Color32(255, 165, 163, 255);
    public static Color blue = new Color32(112, 225, 255, 255);
    public static Color white = new Color32(231, 231, 231, 255);
    public static Color black = new Color32(96, 96, 96, 255);
    public static Color yellow = new Color32(239, 227, 115, 255);
    public static Color green = new Color32(99, 236, 124, 255);
    public static Color pink = new Color32(255, 133, 228, 255);
    public static Color purple = new Color32(185, 112, 255, 255);

    public enum EBlockColor
    {
        Blue,
        Red,
        Yellow,
        Green,
        White,
        Purple,
        Pink,
        Black
    }

    public static Color UnityColorFromBlockColor(EBlockColor value)
    {
        switch (value)
        {
            case EBlockColor.Red:
                return red;
            case EBlockColor.Blue:
                return blue;
            case EBlockColor.White:
                return white;
            case EBlockColor.Yellow:
                return yellow;
            case EBlockColor.Green:
                return green;
            case EBlockColor.Purple:
                return purple;
            case EBlockColor.Pink:
                return pink;
            case EBlockColor.Black:
                return black;
            default:
                throw new NotImplementedException("not supported");
        }
    }

    [SerializeField]
    private EBlockColor color;

    public EBlockColor ColorType
    {
        get
        {
            return color;
        }
        set
        {
            int animatorTriggerValue;
            switch (value)
            {
                case EBlockColor.Red:
                    animatorTriggerValue = 0;
                    break;
                case EBlockColor.Blue:
                    animatorTriggerValue = 1;
                    break;
                case EBlockColor.White:
                    animatorTriggerValue = 2;
                    break;
                case EBlockColor.Black:
                    animatorTriggerValue = 3;
                    break;
                case EBlockColor.Green:
                    animatorTriggerValue = 4;
                    break;
                case EBlockColor.Yellow:
                    animatorTriggerValue = 5;
                    break;
                case EBlockColor.Pink:
                    animatorTriggerValue = 6;
                    break;
                case EBlockColor.Purple:
                    animatorTriggerValue = 7;
                    break;
                default:
                    throw new NotImplementedException("not supported");
            }

            GetComponent<Animator>().SetInteger(animatorColorIntParam, animatorTriggerValue);
            color = value;
        }
    }

    [SerializeField]
    private GameObject EfxOnDestroy;
    [SerializeField]
    private GameObject EfxOnAttach;
    [SerializeField]
    private AudioClip SfxOnAttach;

    public override bool MatchesWith(GameObject obj)
    {
        if (base.MatchesWith(obj))
        {
            return true;
        }

        ColorBlock other = obj.GetComponent<ColorBlock>();
        if (other == null)
        {
            return false;
        }

        return other.color == color;
    }

    public override void Destroy()
    {
        base.Destroy();

        GameObject efx = Instantiate(EfxOnDestroy, transform.position, Quaternion.identity);
        ParticleSystem.MainModule mainModule = efx.GetComponent<ParticleSystem>().main;
        mainModule.startColor = UnityColorFromBlockColor(color);
        efx.GetComponent<ParticleSystem>().Play();
    }

    protected override void Update()
    {
        base.Update();

        // draw collision edges
        PolygonCollider2D collider = GetComponent<PolygonCollider2D>();
        Vector2[] points = collider.points;
        (Vector2, Vector2) line;
        for (int i = 1; i <= points.Length; i++)
        {
            if (i == points.Length)
            {
                line = ((Vector2)transform.TransformPoint(points[points.Length - 1]), (Vector2)transform.TransformPoint(points[0]));
                Debug.DrawLine(line.Item1, line.Item2, Color.green);
                break;
            }
            else
            {
                line = ((Vector2)transform.TransformPoint(points[i - 1]), (Vector2)transform.TransformPoint(points[i]));
                Debug.DrawLine(line.Item1, line.Item2, Color.green);
            }
        }
    }

    public static EdgeIndex FindEdgeIndex(PolygonCollider2D collider, Vector2 midPoint)
    {
        float minDistance = float.MaxValue;

        Vector2[] points = collider.points;
        (Vector2, Vector2) edge;
        int edgeIndex = -1;

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

            // float distance = HandleUtility.DistancePointLine(midPoint, edge.Item1, edge.Item2);
            float distance = DistancePointToLineSegment(midPoint, edge.Item1, edge.Item2);
            // Logger.Debug($"DST {distance}, {distance2}");
            // if (distance != distance2) {
            //     Debug.DebugBreak();
            // }

            if (distance < minDistance)
            {
                minDistance = distance;
                // edge index is point index - 1
                edgeIndex = i - 1;
            }
        }

        Assert.IsTrue(edgeIndex >= (int)EdgeIndex.RightTop && edgeIndex <= (int)EdgeIndex.Top);

        return (EdgeIndex)edgeIndex;
    }

    public static (Vector2, Vector2, EdgeIndex) FindEdgeByIndex(PolygonCollider2D collider, EdgeIndex idx)
    {
        Vector2[] points = collider.points;
        int edgeIndex = -1;

        for (int i = 1; i <= points.Length; i++)
        {
            edgeIndex = i - 1;
            Assert.IsTrue(edgeIndex >= (int)EdgeIndex.RightTop && edgeIndex <= (int)EdgeIndex.Top);

            if (edgeIndex == (int)idx)
            {
                if (i == points.Length)
                {
                    return (collider.transform.TransformPoint(points[points.Length - 1]), collider.transform.TransformPoint(points[0]), (EdgeIndex)edgeIndex);
                }
                else
                {
                    return (collider.transform.TransformPoint(points[i - 1]), collider.transform.TransformPoint(points[i]), (EdgeIndex)edgeIndex);
                }
            }
        }

        Assert.IsTrue(true);
        return (Vector2.zero, Vector2.zero, EdgeIndex.Bottom);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        int blocksLayer = LayerMask.NameToLayer("blocks");
        // store origin parent for possible restoration
        Transform initialParent = transform.parent;
        Vector3 initialPosition = transform.position;
        Quaternion initialRotation = transform.rotation;

        // ignore central block
        if (gameObject.tag == "central")
        {
            // don't process for central block
            return;
        }
        else if (attached)
        {
            // already attached
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
        var thisBlockEdge = FindClosestEdge(GetComponent<PolygonCollider2D>(), other.collider.ClosestPoint(transform.position));
        Debug.DrawLine(thisBlockEdge.Item1, thisBlockEdge.Item2, Color.red, 10);
        var thisBlockEdgeMidpoint = (thisBlockEdge.Item1 + thisBlockEdge.Item2) / 2;

        var otherBlockEdge = FindClosestEdge(other.gameObject.GetComponent<PolygonCollider2D>(), other.collider.ClosestPoint(transform.position));
        Debug.DrawLine(otherBlockEdge.Item1, otherBlockEdge.Item2, Color.red, 3);

        // check if other block edge is free
        // skip collision processing if it is not
        if (other.gameObject.GetComponent<BasicBlock>().links[otherBlockEdge.Item3] != null)
        {
            transform.parent = initialParent;
            return;
        }

        // List<ContactPoint2D> contacts = new List<ContactPoint2D>();
        // other.GetContacts(contacts);
        // foreach (ContactPoint2D cp in contacts)
        // {
        //     Debug.DrawLine(transform.position, cp.point, Color.blue, 3);
        // }

        Vector2 otherEdgeMidpoint = (otherBlockEdge.Item1 + otherBlockEdge.Item2) / 2;
        EdgeIndex idx = FindEdgeIndex(other.gameObject.GetComponent<PolygonCollider2D>(), otherEdgeMidpoint);
        Logger.Debug("side (our) " + thisBlockEdge.Item3 + " their (from edge): " + otherBlockEdge.Item3 + " their computed: " + idx);

        // Rotation can be calculated by taking vectors
        // (grey edge midpoint - grey hex position) and (blue hex position - blue edge midpoint),
        // the angle between them is the amount the grey hex needs to rotate
        var dir1 = ((Vector2)transform.position - thisBlockEdgeMidpoint).normalized;
        var dir2 = (otherEdgeMidpoint - (Vector2)other.gameObject.transform.position).normalized;

        float angle11 = Mathf.Atan2(dir1.y, dir1.x) * Mathf.Rad2Deg;
        float angle22 = Mathf.Atan2(dir2.y, dir2.x) * Mathf.Rad2Deg;
        float rotationDifference = angle22 - angle11;
        Logger.Debug("rotationDifference " + rotationDifference + " rotationDifference " + rotationDifference);
        transform.Rotate(0, 0, rotationDifference);

        // otherBlockEdge = FindEdgeByIndex(other.gameObject.GetComponent<PolygonCollider2D>(), otherBlockEdge.Item3);// FindClosestEdge(other.gameObject.GetComponent<PolygonCollider2D>(), otherEdgeMidpoint);
        // otherEdgeMidpoint = (otherBlockEdge.Item1 + otherBlockEdge.Item2) / 2;

        // blue hex position + 2 * (blue edge midpoint - blue hex position)
        transform.position = (Vector2)other.gameObject.transform.position + dir2 * edgeAttachPositionOffset;

        int linkedNeighboursCount = LinkWithNeighbours(blocksLayer);

        if (linkedNeighboursCount == 0)
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

        AudioManager.Instance.PlaySfx(SfxOnAttach);

        GameObject efx = Instantiate(EfxOnAttach, transform.position, Quaternion.identity);
        efx.GetComponent<ParticleSystem>().Play();

        // run match check and scoring
        LevelManager.Instance.OnBlocksAttach(gameObject);
    }


}
