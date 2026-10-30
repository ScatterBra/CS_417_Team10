using TMPro;
using UnityEngine;
using Unity.XR.CoreUtils;
using Deadzone.Power;

public class SCV_ResourceDisplay : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;
    [SerializeField] private TMP_Text resourceText;
    bool integrated, wrist, visible = true;
    Canvas wristCanvas;
    bool canvasWasEnabled;
    DEF_GridManager defenseGrid;
    PWR_GridManager powerGrid;
    MED_GridManager clinicGrid;

    void Start()
    {
        integrated = DEF_IntegratedXRPresentation.IsIntegratedScene(gameObject.scene);
        wrist = integrated && GetComponentInParent<XROrigin>() != null;
        if (!wrist || resourceText == null) return;
        wristCanvas = resourceText.GetComponentInParent<Canvas>();
        if (wristCanvas != null) canvasWasEnabled = wristCanvas.enabled;
        foreach (var grid in FindObjectsByType<DEF_GridManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (grid.gameObject.scene == gameObject.scene) { defenseGrid = grid; break; }
        foreach (var grid in FindObjectsByType<PWR_GridManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (grid.gameObject.scene == gameObject.scene) { powerGrid = grid; break; }
        foreach (var grid in FindObjectsByType<MED_GridManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (grid.gameObject.scene == gameObject.scene) { clinicGrid = grid; break; }
    }

    bool InWorkshopContext()
    {
        var input = DEF_VRInput.Instance;
        var camera = input != null && input.gameObject.scene == gameObject.scene && DEF_VRInput.Active && input.xrCamera != null
            ? input.xrCamera : Camera.main;
        // Do not hide supplies when the camera or authored ownership anchors are unresolved.
        if (camera == null || resourceManager == null || defenseGrid == null || powerGrid == null || clinicGrid == null) return true;
        if (PWR_AreaGate.PlayerInside || MED_AreaGate.PlayerInside || defenseGrid.WorldToTile(camera.transform.position, out _, out _)) return false;
        var head = camera.transform.position;
        float workshopDistance = FlatDistance(head, resourceManager.transform.position);
        // Workshop has no gameplay grid. Use its configured inventory owner, not hardcoded scene coordinates.
        // One metre of hysteresis prevents flicker in the unmarked connecting walkways.
        float margin = visible ? 1f : 0f;
        var powerCenter = powerGrid.transform.TransformPoint(new Vector3(powerGrid.gridSize.x * powerGrid.cellSize * .5f, 0, powerGrid.gridSize.y * powerGrid.cellSize * .5f));
        return workshopDistance <= FlatDistance(head, defenseGrid.transform.position) + margin &&
            workshopDistance <= FlatDistance(head, powerCenter) + margin &&
            workshopDistance <= FlatDistance(head, clinicGrid.transform.position) + margin;
    }
    static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
    private void Update()
    {
        if (resourceText == null || resourceManager == null) return;
        if (wrist && wristCanvas != null)
        {
            visible = InWorkshopContext();
            wristCanvas.enabled = canvasWasEnabled && visible;
        }
        resourceText.text =
            (integrated ? "WORKSHOP SCRAP: " : "SCRAP: ") + resourceManager.scrap +
            "\nPARTS: " + resourceManager.parts +
            (integrated ? "\nAMMO BOXES: " : "\nAMMO: ") + resourceManager.ammo +
            "\nREPAIR KITS: " + resourceManager.repairKits;
    }
    void OnDisable() { if (wristCanvas != null) wristCanvas.enabled = canvasWasEnabled; }
}
