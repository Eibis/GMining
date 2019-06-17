using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshManager : MonoBehaviour
{
    public MeshFilter MainMesh;
    private MeshData MData;

    List<Vector3> DebugFrom = new List<Vector3>();
    List<Vector3> DebugTo = new List<Vector3>();
    List<Vector3> DebugPoints = new List<Vector3>();

    public void Start()
    {
        MData = new MeshData(MainMesh.mesh);
    }

    public void LateUpdate()
    {
        if (MData.UpdateMeshNeeded)
        {
            MData.UpdateMesh();
        }
    }

    public void RaycastQuery(Ray pRay)
    {
        Vector3 from = pRay.origin;
        DebugFrom.Add(from);

        Vector3 to = pRay.origin + pRay.direction * 10.0f;
        DebugTo.Add(to);

        Triangle hit = new Triangle();
        Vector3 point = Vector3.zero;
        bool result = MData.Raycast(pRay, ref hit, ref point);

        if (result)
        {
            Debug.Log("HIT " + hit.Index + " " + point);
            DebugPoints.Add(point);
            MData.AddVertex(point, hit);
        }
        else
            Debug.Log("MISS :-(");
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < DebugFrom.Count; ++i)
        {
            Vector3 from = DebugFrom[i];
            Vector3 to = DebugTo[i];
            Vector3 diff = to - from;

            Gizmos.color = Color.HSVToRGB(i / DebugFrom.Count, 1.0f, 1.0f);
            Gizmos.DrawRay(from, diff);
        }

        foreach (var point in DebugPoints)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(point, 0.01f);
        }
    }
}
