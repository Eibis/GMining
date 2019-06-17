using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    public int MaxDepth = 50;
    public float SplitCost = 0.5f;

    public KDNode Root;
    public List<Triangle> Triangles = new List<Triangle>();
    //TODO list of duplicate points
    //TODO dictionary of edges per point
    public Dictionary<int, Vertex> Vertices = new Dictionary<int, Vertex>();

    public MeshData(Mesh pMesh)
    {
        GenerateTriangleList(pMesh);
        Root = Build(Triangles, 0, Axis.NO_AXIS);
    }

    private void GenerateTriangleList(Mesh pMesh)
    {
        for (int i = 0; i < pMesh.triangles.Length; i += 3)
        {
            Triangle t = new Triangle();

            t.Index = i;

            int v_index = pMesh.triangles[t.Index + 0];
            if (!Vertices.ContainsKey(v_index))
            {
                Vertices.Add(v_index, new Vertex(v_index, pMesh.vertices[v_index]));
            }

            t.V0 = Vertices[v_index];

            v_index = pMesh.triangles[t.Index + 1];
            if (!Vertices.ContainsKey(v_index))
            {
                Vertices.Add(v_index, new Vertex(v_index, pMesh.vertices[v_index]));
            }

            t.V1 = Vertices[v_index];

            v_index = pMesh.triangles[t.Index + 2];
            if (!Vertices.ContainsKey(v_index))
            {
                Vertices.Add(v_index, new Vertex(v_index, pMesh.vertices[v_index]));
            }

            t.V2 = Vertices[v_index];

            t.Box = new Bounds(t.GetMidPoint(), Vector3.zero);
            t.Box.Encapsulate(t.V0.Position);
            t.Box.Encapsulate(t.V1.Position);
            t.Box.Encapsulate(t.V2.Position);
            
            //TODO calc edges

            Triangles.Add(t);
        }
    }

    KDNode Build(List<Triangle> pTriangles, int pDepth, Axis pPreviousAxis)
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

        List<Triangle> left_triangles = new List<Triangle>();
        List<Triangle> right_triangles = new List<Triangle>();

        Bounds left_box = new Bounds();
        bool left_box_initialized = false;
        Bounds right_box = new Bounds();
        bool right_box_initialized = false;

        Axis long_axis = node.GetLongestAxis(pPreviousAxis);

        for (int i = 0; i < tris_count; ++i)
        {
            Vector3 triangle_mid_point = node.Triangles[i].GetMidPoint();

            bool add_to_right = false;

            switch (long_axis)
            {
                case Axis.X_AXIS:

                    add_to_right = mid_point.x >= triangle_mid_point.x;

                    break;
                case Axis.Y_AXIS:

                    add_to_right = mid_point.y >= triangle_mid_point.y;

                    break;
                case Axis.Z_AXIS:

                    add_to_right = mid_point.z >= triangle_mid_point.z;

                    break;
                default:
                    break;
            }

            if (add_to_right)
            {
                if (!right_box_initialized)
                {
                    right_box = new Bounds(node.Triangles[i].Box.center, node.Triangles[i].Box.size);
                    right_box_initialized = true;
                }
                else
                {
                    right_box.Encapsulate(node.Triangles[i].Box);
                }

                right_triangles.Add(node.Triangles[i]);
            }
            else
            {
                if (!left_box_initialized)
                {
                    left_box = new Bounds(node.Triangles[i].Box.center, node.Triangles[i].Box.size);
                    left_box_initialized = true;
                }
                else
                {
                    left_box.Encapsulate(node.Triangles[i].Box);
                }

                left_triangles.Add(node.Triangles[i]);
            }
        }

        float SAH_initial = tris_count * GetBoxArea(node.Box);
        float SAH_optimal = Mathf.Min(GetBoxArea(left_box) * left_triangles.Count, GetBoxArea(right_box) * right_triangles.Count);

            
        if (SAH_optimal + SplitCost < SAH_initial)
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

    public float GetBoxArea(Bounds pBox)
    {
        return (pBox.size.x + pBox.size.y + pBox.size.z) * 2;
    }

    public bool Raycast(Ray pRay, ref Triangle pHitTriangle, ref Vector3 pPoint)
    {
        return Root.Raycast(pRay, ref pHitTriangle, ref pPoint);
    }
}
