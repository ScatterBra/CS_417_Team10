using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Scene-owned, opt-in courier corridor for the integrated final layout. No combat-grid/path changes.
[DisallowMultipleComponent]
public sealed class DEF_ClinicCourierRoute : MonoBehaviour
{
    MED_DeliveryPath route, originalInbound;
    GameObject generated;
    Material paving;
    readonly List<GameObject> hiddenPieces = new();
    public MED_DeliveryPath Route => isActiveAndEnabled ? route : null;
    public Transform DefenseTerminal { get; private set; }
    public string FailureReason { get; private set; } = "Courier route not prepared.";
    static readonly Vector3 Bay = new Vector3(12, 0, 24);

    public static DEF_ClinicCourierRoute Ensure(DEF_ClinicTraining training, MED_DeliveryPath inbound)
    {
        if (training == null) return null;
        var helper = training.GetComponent<DEF_ClinicCourierRoute>();
        if (helper == null) helper = training.gameObject.AddComponent<DEF_ClinicCourierRoute>();
        if (helper.route == null && helper.isActiveAndEnabled) helper.Prepare(inbound);
        else if (helper.originalInbound != inbound) return null;
        return helper;
    }

    bool Reject(string reason) { FailureReason = reason; return false; }
    bool Prepare(MED_DeliveryPath inbound)
    {
        if (inbound == null || !inbound.isActiveAndEnabled || inbound.gameObject.scene != gameObject.scene)
            return Reject("Clinic arrival path is unavailable; no Medicine loaded.");
        float length = inbound.TotalLength;
        if (float.IsNaN(length) || float.IsInfinity(length) || length < 1 || length > 100 ||
            Vector3.Distance(inbound.GetPointAtDistance(0), new Vector3(80, 0, 11.65f)) > .05f ||
            Vector3.Distance(inbound.GetPointAtDistance(length), new Vector3(78, 0, 0)) > .05f)
            return Reject("Clinic route does not match the verified final-scene corridor.");
        // Final scene's existing grounded connector and spine. Fail closed in other layouts.
        var connector = FindBox("Clinic door");
        var spine = FindBox("Station spine");
        if (!Supports(connector, new Vector3(80, 0, 11.65f), .5f) || !Supports(connector, new Vector3(80, 0, 29), .5f) ||
            !Supports(spine, new Vector3(14, 0, 30), .65f) || !Supports(spine, new Vector3(78, 0, 30), .65f))
            return Reject("Courier paving is missing or moved; no Medicine loaded.");
        var pieces = new List<BoxCollider>(); BoxCollider post = null;
        // North group Transform1756081045337926575 in PerimeterFence prefab eea948c2.
        // Five spanning pieces: wall/coping/three rails; only the X12 post is removed.
        Transform fence = null;
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.gameObject.scene == gameObject.scene && t.name == "Perimeter Fence")
            { if (fence != null) return Reject("Ambiguous Defense perimeter."); fence = t; }
        if (fence == null) return Reject("Defense perimeter was not found.");
        foreach (var box in fence.GetComponentsInChildren<BoxCollider>())
        {
            var b = box.bounds;
            if (!box.enabled || box.isTrigger || Mathf.Abs(b.center.z - 20.5f) > .02f) continue;
            if ((box.name == "Wall" || box.name == "Coping" || box.name == "Fence rail") && b.size.x > 40 &&
                Mathf.Abs(b.center.x) < .02f && Quaternion.Angle(box.transform.rotation, Quaternion.identity) < .1f)
                pieces.Add(box);
            if (box.name == "Fence post" && Mathf.Abs(b.center.x - 12) < .02f && b.size.x < .2f)
            { if (post != null) return Reject("Ambiguous north gate post."); post = box; }
        }
        if (pieces.Count != 5 || post == null) return Reject("North service gate geometry does not match the verified layout.");
        foreach (var piece in pieces)
            if (piece.transform.childCount != 0 || Mathf.Abs(piece.center.x) > .001f || piece.size.x <= 0 ||
                piece.transform.localScale.x <= 0 || piece.bounds.min.x >= 11.1f || piece.bounds.max.x <= 12.9f)
                return Reject("Unsupported service gate collider; existing perimeter preserved.");
        var shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return Reject("Courier paving material is unavailable.");

        generated = new GameObject("Clinic to Defense courier route");
        SceneManager.MoveGameObjectToScene(generated, gameObject.scene);
        paving = new Material(shader); paving.color = new Color(.12f, .15f, .17f);
        // Flush top Y0. These are floors, never walls or combat-grid occupancy.
        Slab("Clinic courier turning apron", new Vector3(79.5f, 0, 29.5f), new Vector2(3, 3));
        Slab("Defense courier bay branch", new Vector3(12, 0, 27), new Vector2(3, 8));
        Slab("Defense courier receiving pad", Bay, new Vector2(4, 4));
        Slab("Defense pedestrian service gate paving", new Vector3(12, 0, 21.75f), new Vector2(1.8f, 4.5f));
        foreach (var piece in pieces) OpenPiece(piece);
        hiddenPieces.Add(post.gameObject); post.gameObject.SetActive(false);

        var points = new List<Vector3>();
        // Reverse the same proven in-Clinic arrival curve, sampled densely; the new outer route is a bounded polyline.
        for (float d = length; d > 0; d -= .25f) AddPoint(points, inbound.GetPointAtDistance(d));
        AddPoint(points, inbound.GetPointAtDistance(0));
        AddPoint(points, new Vector3(80, 0, 29));
        AddPoint(points, new Vector3(78, 0, 30));
        AddPoint(points, new Vector3(14, 0, 30));
        AddPoint(points, new Vector3(12, 0, 30));
        AddPoint(points, Bay);
        var pathObject = new GameObject("Explicit bounded courier path"); pathObject.transform.SetParent(generated.transform, false);
        var builtRoute = pathObject.AddComponent<MED_DeliveryPath>();
        if (!builtRoute.SetWorldPolyline(points)) { Cleanup(); return Reject("Courier path could not be built."); }
        var terminal = new GameObject("Defense Clinic delivery terminal"); terminal.transform.SetParent(generated.transform, false);
        terminal.transform.position = Bay;
        originalInbound = inbound; route = builtRoute; DefenseTerminal = terminal.transform; FailureReason = null;
        return true;
    }

    static void AddPoint(List<Vector3> points, Vector3 p)
    { if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], p) > .0001f) points.Add(p); }
    BoxCollider FindBox(string objectName)
    {
        BoxCollider result = null;
        foreach (var box in FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
            if (box.gameObject.scene == gameObject.scene && box.name == objectName)
            { if (result != null) return null; result = box; }
        return result;
    }
    static bool Supports(BoxCollider box, Vector3 point, float halfWidth)
    {
        if (box == null || !box.enabled || box.isTrigger || Mathf.Abs(box.bounds.max.y) > .02f) return false;
        var b = box.bounds;
        return point.x - halfWidth >= b.min.x && point.x + halfWidth <= b.max.x &&
            point.z - halfWidth >= b.min.z && point.z + halfWidth <= b.max.z;
    }
    void Slab(string title, Vector3 surface, Vector2 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = title;
        go.transform.SetParent(generated.transform, false); go.transform.position = surface + Vector3.down * .05f;
        go.transform.localScale = new Vector3(size.x, .1f, size.y); go.GetComponent<Renderer>().sharedMaterial = paving;
    }
    void OpenPiece(BoxCollider piece)
    {
        var b = piece.bounds;
        Segment(piece, b.min.x, 11.1f, " left of service gate");
        Segment(piece, 12.9f, b.max.x, " right of service gate");
        hiddenPieces.Add(piece.gameObject); piece.gameObject.SetActive(false);
    }
    void Segment(BoxCollider source, float minX, float maxX, string suffix)
    {
        var clone = Instantiate(source.gameObject, source.transform.parent); clone.name = source.name + suffix;
        var scale = clone.transform.localScale; scale.x *= (maxX - minX) / source.bounds.size.x;
        clone.transform.localScale = scale;
        var position = clone.transform.position; position.x = (minX + maxX) * .5f; clone.transform.position = position;
        clone.transform.SetParent(generated.transform, true);
    }
    void Cleanup()
    {
        foreach (var piece in hiddenPieces) if (piece != null) piece.SetActive(true);
        hiddenPieces.Clear(); route = null; DefenseTerminal = null; originalInbound = null;
        if (generated != null) Destroy(generated); generated = null;
        if (paving != null) Destroy(paving); paving = null;
    }
    void OnDestroy() => Cleanup();
}
