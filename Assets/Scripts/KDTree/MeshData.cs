using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshData
{
    public int MaxDepth = 50;
    public float SplitCost = 0.5f;

    public KDNode Root;
    public Mesh TriMesh;

    private List<Vector3> RawVertices;
    private List<Vector3> RawNormals;
    private List<int> RawTriangles;

    public Dictionary<int, Vertex> Vertices = new Dictionary<int, Vertex>();
    public List<Triangle> Triangles = new List<Triangle>();

    public Dictionary<Vertex, List<Vertex>> DupVertices = new Dictionary<Vertex, List<Vertex>>();
    public bool UpdateMeshNeeded { get; private set; }

    public MeshData(Mesh pMesh)
    {
        TriMesh = pMesh;
        RawVertices = TriMesh.vertices.ToList<Vector3>();
        RawNormals = TriMesh.normals.ToList<Vector3>(); 
        RawTriangles = TriMesh.triangles.ToList<int>();

        GenerateTriangleList();
        Root = Build(Triangles, 0, Axis.NO_AXIS);
    }

    private void GenerateTriangleList()
    {
        for (int i = 0; i < RawTriangles.Count; i += 3)
        {
            Triangle t = new Triangle();

            t.Index = i;

            int v_index = RawTriangles[t.Index + 0];
            if (!Vertices.ContainsKey(v_index))
            {
                Vertices.Add(v_index, new Vertex(v_index, RawVertices[v_index]));
            }

            t.V0 = Vertices[v_index];
            Vertices[v_index].Triangles.Add(t);

            v_index = RawTriangles[t.Index + 1];
            if (!Vertices.ContainsKey(v_index))
            {
                Vertices.Add(v_index, new Vertex(v_index, RawVertices[v_index]));
            }

            t.V1 = Vertices[v_index];
            Vertices[v_index].Triangles.Add(t);

            v_index = RawTriangles[t.Index + 2];
            if (!Vertices.ContainsKey(v_index))
            {
                Vertices.Add(v_index, new Vertex(v_index, RawVertices[v_index]));
            }

            t.V2 = Vertices[v_index];
            Vertices[v_index].Triangles.Add(t);

            t.Box = new Bounds(t.GetMidPoint(), Vector3.zero);
            t.Box.Encapsulate(t.V0.Position);
            t.Box.Encapsulate(t.V1.Position);
            t.Box.Encapsulate(t.V2.Position);
            
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

    internal void AddVertex(Vector3 pPoint, Triangle pTriangle)
    {
        RawVertices.Add(pPoint);
        RawNormals.Add(RawNormals[pTriangle.V0.Index]);

        int v_index = RawVertices.Count - 1;

        if (!Vertices.ContainsKey(v_index))
        {
            Vertices.Add(v_index, new Vertex(v_index, pPoint));
        }

        RemoveTriangle(pTriangle);

        AddTriangle(v_index, pTriangle.V0.Index, pTriangle.V1.Index, pTriangle.Index);
        AddTriangle(v_index, pTriangle.V1.Index, pTriangle.V2.Index, pTriangle.Index);
        AddTriangle(v_index, pTriangle.V2.Index, pTriangle.V0.Index, pTriangle.Index);

        UpdateMeshNeeded = true;
    }

    private void AddTriangle(int pI0, int pI1, int pI2, int pTriangleIndex)
    {
        RawTriangles.Add(pI0);

        int t_index = RawTriangles.Count - 1;

        RawTriangles.Add(pI1);
        RawTriangles.Add(pI2);

        Triangle t = new Triangle();

        t.Index = t_index;

        t.V0 = Vertices[pI0];
        t.V1 = Vertices[pI1];
        t.V2 = Vertices[pI2];

        t.Box = new Bounds(t.GetMidPoint(), Vector3.zero);
        t.Box.Encapsulate(t.V0.Position);
        t.Box.Encapsulate(t.V1.Position);
        t.Box.Encapsulate(t.V2.Position);
        
        Root.AddTriangle(t);
    }

    private void RemoveTriangle(Triangle pTriangle)
    {
        RawTriangles.RemoveRange(pTriangle.Index, 3);

        Root.RemoveTriangle(pTriangle, pTriangle.GetMidPoint());

        foreach (Triangle t in Triangles)
        {
            if (t.Index > pTriangle.Index)
                t.Index -= 3;
        }
    }

    internal void UpdateMesh()
    {
        TriMesh.SetVertices(RawVertices);
        TriMesh.SetNormals(RawNormals);
        TriMesh.SetTriangles(RawTriangles, 0);

        UpdateMeshNeeded = false;
    }
}
