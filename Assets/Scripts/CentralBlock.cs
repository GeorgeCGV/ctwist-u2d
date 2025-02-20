using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CentralBlock : BasicBlock
{
    protected override void Start()
    {
        // override, do nothing
    }

    protected override void Update()
    {
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

        // foreach (KeyValuePair<EdgeIndex, Vector2> entry in EdgeDirections)
        // {

        //     Vector2 start = transform.position;
        //     Vector2 end =  transform.position + transform.TransformDirection((Vector3)entry.Value).normalized * edgeAttachPositionOffset;
        //     Debug.DrawRay(start, end, Color.yellow);
        // }

        foreach (KeyValuePair<EdgeIndex, Vector2> entry in EdgeDirections)
        {
            Vector2 start = transform.position + transform.TransformDirection((Vector3)entry.Value);
            Vector2 dir = ((Vector2)transform.position - start).normalized * neighbourRange;
            Debug.DrawRay(start, dir, Color.red);
        }


    }

    protected override void FixedUpdate()
    {
        // override, do nothing
    }
}
