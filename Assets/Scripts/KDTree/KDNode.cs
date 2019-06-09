using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Axis
{
    NO_AXIS,
    X_AXIS,
    Y_AXIS,
    Z_AXIS
}

public class KDNode
{
    public Bounds Box;

    public KDNode Left;
    public KDNode Right;
    public List<Triangle> Triangles;

    public Axis GetLongestAxis(Axis pPreviousAxis)
    {
        if (Box.extents.x >= Box.extents.y && Box.extents.x >= Box.extents.z && pPreviousAxis != Axis.X_AXIS)
            return Axis.X_AXIS;
        if (Box.extents.y >= Box.extents.x && Box.extents.y >= Box.extents.z && pPreviousAxis != Axis.Y_AXIS)
            return Axis.Y_AXIS;
        if (Box.extents.z >= Box.extents.y && Box.extents.z >= Box.extents.x && pPreviousAxis != Axis.Z_AXIS)
            return Axis.Z_AXIS;
        return Axis.X_AXIS;
    }

    internal void InitEmptyLeaf()
    {
        Left = new KDNode();
        Right = new KDNode();
        Left.Triangles = new List<Triangle>();
        Right.Triangles = new List<Triangle>();
    }

    public bool Raycast(Ray pRay, ref Triangle pHitTriangle, ref Vector3 pPoint)
    {
        if (Box.IntersectRay(pRay))
        {
            if (Left.Triangles.Count > 0 || Right.Triangles.Count > 0)
            {
                bool left_raycast = Left.Triangles.Count > 0 ? Left.Raycast(pRay, ref pHitTriangle, ref pPoint) : false;
                bool right_raycast = Right.Triangles.Count > 0 ? Right.Raycast(pRay, ref pHitTriangle, ref pPoint) : false;

                return left_raycast || right_raycast;
            }
            else
            {
                bool found = false;
                foreach(var triangle in Triangles)
                {
                    if (triangle.IntersectRay(pRay, ref pPoint))
                    {
                        pHitTriangle = triangle;
                        found = true;
                        break;
                    }
                }

                return found;
            }
        }

        return false;
    }
}
