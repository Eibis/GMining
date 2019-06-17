using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vertex
{
    public int Index;

    public Vector3 Position;

    public List<Triangle> Triangles = new List<Triangle>();

    public Vertex(int pIndex, Vector3 pPosition)
    {
        Index = pIndex;
        Position = pPosition;
    }
}

public class Triangle
{
    public int Index;

    public Vertex V0;
    public Vertex V1;
    public Vertex V2;

    public Bounds Box;

    internal Vector3 GetMidPoint()
    {
        return (V0.Position + V1.Position + V2.Position) / 3.0f;
    }

    internal bool IntersectRay(Ray pRay, ref Vector3 pPoint)
    {
        /*triangle vectors*/
        Vector3 v, u;
        u = V1.Position - V0.Position;
        v = V2.Position - V0.Position;

        Vector3 n = Vector3.Cross(u, v);

        /*degenerate triangle*/
        if (n.magnitude == 0)
            return false;

        Vector3 w0 = pRay.origin - V0.Position;

        float a = -Vector3.Dot(n, w0);

        /*the ray comes from behind the triangle*/
        if (a > 0.0f)
            return false;

        float b = Vector3.Dot(n, pRay.direction);

        /*parallel to the triangle or plane*/
        if (Mathf.Approximately(b, 0.0f))
            return false;

        float r = a / b;

        /*ray starts after the triangle so no intersection*/
        if (r < 0.0f)
            return false;

        Vector3 point = pRay.origin + r * pRay.direction;

        bool is_point_in_triangle = CheckPointInTriangle(point);

        if (!is_point_in_triangle)
            return false;

        pPoint = point;
        return true;
    }

    private bool CheckPointInTriangle(Vector3 pI)
    {
        Vector3 v, u;
        u = V1.Position - V0.Position;
        v = V2.Position - V0.Position;

        float uu = Vector3.Dot(u, u);
        float uv = Vector3.Dot(u, v);
        float vv = Vector3.Dot(v, v);

        Vector3 w = pI - V0.Position;

        float wu = Vector3.Dot(w, u);
        float wv = Vector3.Dot(w, v);

        float D = uv * uv - uu * vv;

        float s = (uv * wv - vv * wu) / D;

        /* I is outside T*/
        if (s < 0.0 || s > 1.0)
            return false;

        float t = (uv * wu - uu * wv) / D;

        /* I is outside T*/
        if (t < 0.0 || (s + t) > 1.0)
            return false;

        return true;
    }
}
