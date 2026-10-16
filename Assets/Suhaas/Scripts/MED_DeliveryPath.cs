using System;
using UnityEngine;

// A smooth designer-defined route through child waypoint transforms (Catmull-Rom spline). An arc-length table lets a
// follower move at constant speed: GetPointAtDistance / GetTangentAtDistance take a distance in meters along the curve.
// Everything is computed in this object's local space, so moving or rotating the station moves the path with it.
public class MED_DeliveryPath : MonoBehaviour
{
    [Tooltip("Waypoints in route order. If empty, the child transforms are used.")]
    [SerializeField] Transform[] waypoints;
    [SerializeField, Range(4, 64)] int samplesPerSegment = 24;
    [SerializeField] Color gizmoColor = new Color(1f, 0.85f, 0.2f);

    Vector3[] localPoints = Array.Empty<Vector3>();
    float[] cumulative = Array.Empty<float>();   // distance (meters) from the start to each sample
    bool built;

    public float TotalLength
    {
        get { Ensure(); return cumulative.Length > 0 ? cumulative[cumulative.Length - 1] : 0f; }
    }

    void OnValidate() => built = false;

    void Ensure()
    {
        if (!built) Build();
    }

    Vector3[] LocalWaypoints()
    {
        var list = new System.Collections.Generic.List<Vector3>();
        if (waypoints != null && waypoints.Length > 0)
        {
            foreach (var w in waypoints)
                if (w != null) list.Add(transform.InverseTransformPoint(w.position));
        }
        else
        {
            foreach (Transform c in transform) list.Add(c.localPosition);
        }
        return list.ToArray();
    }

    [ContextMenu("Rebuild Path")]
    public void Build()
    {
        var wp = LocalWaypoints();
        built = true;
        if (wp.Length < 2)
        {
            localPoints = wp;
            cumulative = new float[wp.Length];
            return;
        }

        int segs = wp.Length - 1;
        var pts = new Vector3[segs * samplesPerSegment + 1];
        int n = 0;
        for (int i = 0; i < segs; i++)
        {
            // Phantom end points (mirrored) give the curve natural tangents at the start and end.
            var p0 = i > 0 ? wp[i - 1] : 2f * wp[0] - wp[1];
            var p1 = wp[i];
            var p2 = wp[i + 1];
            var p3 = i + 2 < wp.Length ? wp[i + 2] : 2f * wp[wp.Length - 1] - wp[wp.Length - 2];
            for (int s = 0; s < samplesPerSegment; s++)
                pts[n++] = CatmullRom(p0, p1, p2, p3, s / (float)samplesPerSegment);
        }
        pts[n++] = wp[wp.Length - 1];

        float scale = Mathf.Max(0.0001f, transform.lossyScale.x);
        var cum = new float[pts.Length];
        for (int i = 1; i < pts.Length; i++)
            cum[i] = cum[i - 1] + Vector3.Distance(pts[i - 1], pts[i]) * scale;
        localPoints = pts;
        cumulative = cum;
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    // Finds the sample index i whose cumulative distance range contains d, plus the blend factor inside that sample.
    void Locate(float d, out int i, out float f)
    {
        Ensure();
        if (localPoints.Length < 2) { i = 0; f = 0f; return; }
        d = Mathf.Clamp(d, 0f, cumulative[cumulative.Length - 1]);
        int idx = Array.BinarySearch(cumulative, d);
        if (idx < 0) idx = ~idx - 1;
        i = Mathf.Clamp(idx, 0, localPoints.Length - 2);
        float span = cumulative[i + 1] - cumulative[i];
        f = span > 0.00001f ? (d - cumulative[i]) / span : 0f;
    }

    /// <summary>World position at a distance (meters) along the path, clamped to the ends.</summary>
    public Vector3 GetPointAtDistance(float d)
    {
        Ensure();
        if (localPoints.Length == 0) return transform.position;
        if (localPoints.Length == 1) return transform.TransformPoint(localPoints[0]);
        Locate(d, out int i, out float f);
        return transform.TransformPoint(Vector3.Lerp(localPoints[i], localPoints[i + 1], f));
    }

    /// <summary>Normalized world direction of travel at a distance along the path.</summary>
    public Vector3 GetTangentAtDistance(float d)
    {
        Ensure();
        if (localPoints.Length < 2) return transform.forward;
        Locate(d, out int i, out _);
        var dir = transform.TransformDirection(localPoints[i + 1] - localPoints[i]);
        return dir.sqrMagnitude > 0.000001f ? dir.normalized : transform.forward;
    }

    void OnDrawGizmos()
    {
        Build();
        Gizmos.color = gizmoColor;
        for (int i = 1; i < localPoints.Length; i++)
            Gizmos.DrawLine(transform.TransformPoint(localPoints[i - 1]), transform.TransformPoint(localPoints[i]));
        Gizmos.color = new Color(1f, 0.4f, 0.1f);
        foreach (var w in LocalWaypoints())
            Gizmos.DrawSphere(transform.TransformPoint(w), 0.15f);
    }
}
