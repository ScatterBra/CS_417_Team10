using UnityEngine;

// Integrated-scene walk-in access and physical delivery presentation only.
// Never changes base footprint, path occupancy, waves, resources or authenticated receipts.
[DisallowMultipleComponent]
public sealed class DEF_IntegratedOutpostPresentation : MonoBehaviour
{
    const float DoorWidth = 1.8f;
    float nextScan;
    Deadzone.Power.PWR_DefenseCellDock positionedDock;
    void Start()
    {
        if (!DEF_IntegratedXRPresentation.IsIntegratedScene(gameObject.scene)) { enabled = false; return; }
        var structure = GetComponent<DEF_BaseStructure>();
        if (structure == null) { enabled = false; return; }
        OpenNorthEntrance(structure);
        foreach (var intake in FindObjectsByType<DEF_WorkshopAmmoReceiver>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (intake.gameObject.scene != gameObject.scene) continue;
            // Keep the same receiver, trigger, ledger and destination; only move its presentation.
            intake.transform.position = transform.TransformPoint(new Vector3(0, 1.35f, -1));
            intake.transform.rotation = transform.rotation;
            var oldPlatform = intake.transform.Find("Intake platform");
            if (oldPlatform != null) oldPlatform.gameObject.SetActive(false);
            if (intake.feedbackText != null) intake.feedbackText.gameObject.SetActive(false);
            if (intake.GetComponent<DEF_PhysicalReceiverPresentation>() == null)
                intake.gameObject.AddComponent<DEF_PhysicalReceiverPresentation>();
        }
    }
    void OpenNorthEntrance(DEF_BaseStructure structure)
    {
        if (transform.Find("Integrated outpost access ramp") != null) return;
        Transform wall = null, sill = null;
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            var position = transform.InverseTransformPoint(child.position);
            if (Mathf.Abs(position.x) > .05f || Mathf.Abs(position.z - structure.halfSize) > .05f) continue;
            if (child.name == "Armored lower wall") wall = child;
            if (child.name == "Window sill") sill = child;
        }
        if (!CanSplit(wall) || !CanSplit(sill))
        { Debug.LogError("Integrated outpost requires its authored north wall and sill; entry was not modified.", this); return; }
        Split(wall); Split(sill);
        var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Integrated outpost access ramp";
        ramp.transform.SetParent(transform, false);
        ramp.transform.localPosition = new Vector3(0, .085f, 4.25f);
        ramp.transform.localRotation = Quaternion.Euler(Mathf.Atan(.27f / 2.5f) * Mathf.Rad2Deg, 0, 0);
        ramp.transform.localScale = new Vector3(DoorWidth, .1f, 2.5f);
        var floor = transform.Find("Foundation");
        if (floor != null && floor.TryGetComponent<Renderer>(out var renderer))
            ramp.GetComponent<Renderer>().sharedMaterial = renderer.sharedMaterial;
    }
    static bool CanSplit(Transform source)
    {
        if (source == null || !source.TryGetComponent<BoxCollider>(out var box)) return false;
        return source.TransformVector(Vector3.right * box.size.x).magnitude > DoorWidth;
    }
    static void Split(Transform source)
    {
        var box = source.GetComponent<BoxCollider>();
        // Native lower-wall collider is 2m wide: localScale.x alone is NOT its physical width.
        float width = source.TransformVector(Vector3.right * box.size.x).magnitude;
        float segmentWidth = (width - DoorWidth) * .5f;
        var direction = source.TransformVector(Vector3.right).normalized;
        source.gameObject.SetActive(false);
        foreach (int side in new[] { -1, 1 })
        {
            var segment = Instantiate(source.gameObject, source.parent);
            segment.name = "Integrated doorway segment " + source.name + " " + side;
            var scale = source.localScale;
            scale.x *= segmentWidth / width;
            segment.transform.localScale = scale;
            segment.transform.position = source.position + direction * (side * (DoorWidth + segmentWidth) * .5f);
            segment.SetActive(true);
        }
    }
    void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + .5f;
        // The optional auto-spawned dock follows the moved intake, not its obsolete outside location.
        var dock = Deadzone.Power.PWR_DefenseCellDock.Instance;
        if (dock != null && dock != positionedDock && dock.gameObject.scene == gameObject.scene &&
            dock.name == "PWR Cell Dock (Power → Defense)" && !dock.IsDocked)
        {
            dock.transform.position = transform.TransformPoint(new Vector3(1.6f, .27f, -1));
            dock.transform.rotation = transform.rotation;
            positionedDock = dock;
        }
        foreach (var receiver in FindObjectsByType<DEF_WorkshopRepairReceiver>(FindObjectsSortMode.None))
            if (receiver.gameObject.scene == gameObject.scene && receiver.GetComponent<DEF_PhysicalReceiverPresentation>() == null)
                receiver.gameObject.AddComponent<DEF_PhysicalReceiverPresentation>();
    }
}
