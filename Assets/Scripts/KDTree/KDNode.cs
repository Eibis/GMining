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
    public static int MaxDepth = 50;
    public static float SplitCost = 0.5f;

    public Bounds Box;

    public KDNode Left;
    public KDNode Right;
    public List<Triangle> Triangles;
    public Axis SelectionAxis;
    private Vector3 MidPoint;

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

    internal static KDNode Build(List<Triangle> pTriangles, int pDepth, Axis pPreviousAxis)
    {
        KDNode node = new KDNode();
        node.Triangles = pTriangles;

        float tris_count = node.Triangles.Count;

        if (tris_count == 0)
            return node;

        node.Box = new Bounds(node.Triangles[0].Box.center, node.Triangles[0].Box.size);

        if (tris_count == 1 || pDepth >= MaxDepth)
        {
            node.InitEmptyLeaf();

            return node;
        }

        Vector3 mid_point = Vector3.zero;

        for (int i = 1; i < tris_count; ++i)
        {
            node.Box.Encapsulate(node.Triangles[i].Box);
        }

        for (int i = 0; i < tris_count; ++i)
        {
            mid_point += node.Triangles[0].GetMidPoint() * (1 / tris_count);
        }

        node.MidPoint = mid_point;

        Axis long_axis = node.GetLongestAxis(pPreviousAxis);

        node.SelectionAxis = long_axis;

        List<Triangle> left_triangles = new List<Triangle>();
        List<Triangle> right_triangles = new List<Triangle>();

        bool should_split = node.DivideLeftRightTriangles(ref left_triangles, ref right_triangles);

        if (should_split)
        {
            node.Left = Build(left_triangles, pDepth + 1, long_axis);
            node.Right = Build(right_triangles, pDepth + 1, long_axis);
        }
        else
        {
            node.InitEmptyLeaf();
        }

        return node;
    }

    private bool DivideLeftRightTriangles(ref List<Triangle> pLeftTriangles, ref List<Triangle> pRightTriangles)
    {
        Bounds left_box = new Bounds();
        bool left_box_initialized = false;
        Bounds right_box = new Bounds();
        bool right_box_initialized = false;

        for (int i = 0; i < Triangles.Count; ++i)
        {
            bool add_to_right = ShouldAddToRight(Triangles[i]);

            if (add_to_right)
            {
                if (!right_box_initialized)
                {
                    right_box = new Bounds(Triangles[i].Box.center, Triangles[i].Box.size);
                    right_box_initialized = true;
                }
                else
                {
                    right_box.Encapsulate(Triangles[i].Box);
                }

                pRightTriangles.Add(Triangles[i]);
            }
            else
            {
                if (!left_box_initialized)
                {
                    left_box = new Bounds(Triangles[i].Box.center, Triangles[i].Box.size);
                    left_box_initialized = true;
                }
                else
                {
                    left_box.Encapsulate(Triangles[i].Box);
                }

                pLeftTriangles.Add(Triangles[i]);
            }
        }

        float SAH_initial = Triangles.Count * GetBoxArea(Box);
        float SAH_optimal = Mathf.Min(GetBoxArea(left_box) * pLeftTriangles.Count, GetBoxArea(right_box) * pRightTriangles.Count);

        return SAH_optimal + SplitCost < SAH_initial;
    }

    private bool ShouldAddToRight(Triangle pTriangle)
    {
        Vector3 triangle_mid_point = pTriangle.GetMidPoint();

        bool add_to_right = false;

        switch (SelectionAxis)
        {
            case Axis.X_AXIS:

                add_to_right = MidPoint.x >= triangle_mid_point.x;

                break;
            case Axis.Y_AXIS:

                add_to_right = MidPoint.y >= triangle_mid_point.y;

                break;
            case Axis.Z_AXIS:

                add_to_right = MidPoint.z >= triangle_mid_point.z;

                break;
            default:
                break;
        }

        return add_to_right;
    }

    public static float GetBoxArea(Bounds pBox)
    {
        return (pBox.size.x + pBox.size.y + pBox.size.z) * 2;
    }

    public bool Raycast(Ray pRay, ref Triangle pHitTriangle, ref Vector3 pPoint)
    {
        if (Box.IntersectRay(pRay))
        {
            if (!IsLeaf())
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

    internal void RemoveTriangle(Triangle pTriangle, Vector3 pTriangleMidPoint)
    {
        if (Box.Contains(pTriangleMidPoint))
        {
            int index = Triangles.IndexOf(pTriangle);
            if (index >= 0)
            {
                Triangles.RemoveAt(index);

                if(Triangles.Count > 0)
                { 
                    Box = new Bounds(Triangles[0].Box.center, Triangles[0].Box.size);

                    for (int i = 1; i < Triangles.Count; ++i)
                    {
                        Box.Encapsulate(Triangles[i].Box);
                    }
                }
            }

            if (!IsLeaf())
            {
                if (Left.Triangles.Count > 0)
                    Left.RemoveTriangle(pTriangle, pTriangleMidPoint);

                if (Right.Triangles.Count > 0)
                    Right.RemoveTriangle(pTriangle, pTriangleMidPoint);

                if (Left.Triangles.Count == 0 && Right.Triangles.Count == 0)
                {
                    InitEmptyLeaf();
                }
            }
        }
    }

    internal void AddTriangle(Triangle pTriangle)
    {
        Triangles.Add(pTriangle);

        if(Triangles.Count == 1)
            Box = new Bounds(pTriangle.Box.center, pTriangle.Box.size);
        else
            Box.Encapsulate(pTriangle.Box);

        if (!IsLeaf())
        {
            bool add_to_right = ShouldAddToRight(pTriangle);

            if (add_to_right)
                Right.AddTriangle(pTriangle);
            else
                Left.AddTriangle(pTriangle);
        }
        else
        {
            List<Triangle> left_triangles = new List<Triangle>();
            List<Triangle> right_triangles = new List<Triangle>();

            bool should_split = DivideLeftRightTriangles(ref left_triangles, ref right_triangles);

            if (should_split)
            {
                //split
            }
        }
    }

    public bool IsLeaf()
    {
        return Left == null || Right == null || Left.Triangles.Count == 0 || Right.Triangles.Count == 0;
    }
}
