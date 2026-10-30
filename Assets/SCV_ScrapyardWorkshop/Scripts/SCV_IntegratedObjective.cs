using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Deadzone.Power;

// Read-only integrated Workshop guidance. Receipts, recipes and unlock timers remain owned by gameplay.
[DisallowMultipleComponent]
public sealed class SCV_IntegratedObjective : MonoBehaviour
{
    SCV_ProgressionManager progression;
    DEF_WorkshopReadiness readiness;
    SCV_ResourceManager resources;
    SCV_Workbench bench;
    SCV_Crusher crusher;
    DEF_GridManager defenseGrid;
    PWR_GridManager powerGrid;
    MED_GridManager clinicGrid;
    Canvas canvas;
    RectTransform objective;
    TMP_Text goal, notice;
    GameObject noticePanel;
    float noticeRemaining, completedAt = -100;
    string completedDescription;
    bool visible = true, observed;
    int milestone;

    void Awake()
    {
        progression = GetComponent<SCV_ProgressionManager>();
        readiness = GetComponent<DEF_WorkshopReadiness>();
        resources = FindLocal<SCV_ResourceManager>();
        bench = FindLocal<SCV_Workbench>();
        crusher = FindLocal<SCV_Crusher>();
        defenseGrid = FindLocal<DEF_GridManager>();
        powerGrid = FindLocal<PWR_GridManager>();
        clinicGrid = FindLocal<MED_GridManager>();
    }
    T FindLocal<T>() where T : Component
    {
        foreach (var item in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (item.gameObject.scene == gameObject.scene) return item;
        return null;
    }
    Camera PlayerCamera()
    {
        var input = DEF_VRInput.Instance;
        return input != null && input.gameObject.scene == gameObject.scene && DEF_VRInput.Active && input.xrCamera != null
            ? input.xrCamera : Camera.main;
    }
    bool InWorkshop(Camera camera)
    {
        if (camera == null || resources == null || defenseGrid == null || powerGrid == null || clinicGrid == null) return true;
        if (PWR_AreaGate.PlayerInside || MED_AreaGate.PlayerInside || defenseGrid.WorldToTile(camera.transform.position, out _, out _)) return false;
        var head = camera.transform.position;
        float own = Distance(head, resources.transform.position), margin = visible ? 1f : 0f;
        var powerCenter = powerGrid.transform.TransformPoint(new Vector3(powerGrid.gridSize.x * powerGrid.cellSize * .5f, 0, powerGrid.gridSize.y * powerGrid.cellSize * .5f));
        return own <= Distance(head, defenseGrid.transform.position) + margin && own <= Distance(head, powerCenter) + margin && own <= Distance(head, clinicGrid.transform.position) + margin;
    }
    static float Distance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
    bool Ready
    {
        get
        {
            var defense = DEF_Progression.Current;
            return defense != null && defense.gameObject.scene == gameObject.scene &&
                defense.State.HasPrerequisite(DEF_ProgressionState.Prerequisite.Workshop14);
        }
    }
    int Milestone()
    {
        if (Ready) return 16;
        int value = progression.CurrentStage >= SCV_ProgressionManager.WorkshopStage.ProcessPart ? 1 : 0;
        if (progression.CurrentStage >= SCV_ProgressionManager.WorkshopStage.CraftSupplies) value |= 2;
        if (readiness != null && readiness.FreshAmmoCrafted) value |= 4;
        if (readiness != null && readiness.FreshRepairCrafted) value |= 8;
        return value;
    }
    void LateUpdate()
    {
        if (progression == null) return;
        var camera = PlayerCamera();
        if (camera == null) return;
        if (canvas == null) Build(camera);
        if (canvas.transform.parent != camera.transform) canvas.transform.SetParent(camera.transform, false);
        canvas.transform.localPosition = new Vector3(0, 0, 1.15f);
        canvas.worldCamera = camera;
        visible = InWorkshop(camera);
        canvas.enabled = visible;
        int current = Milestone();
        if (observed && current != milestone && (current == 16 || (current & milestone) == milestone))
        {
            completedDescription = current == 16 ? "Fresh supplies and practice delivery verified." :
                (current & 8) != (milestone & 8) ? "Fresh RepairKit crafted." :
                (current & 4) != (milestone & 4) ? "Fresh Ammo Box crafted." :
                (current & 2) != (milestone & 2) ? "First Part produced." : "Salvage wreck fully harvested.";
            completedAt = Time.unscaledTime;
        }
        else if (observed && current != milestone) completedAt = -100; // Reset is never an accomplishment.
        observed = true; milestone = current;
        bool check = Time.unscaledTime - completedAt < 2.2f;
        goal.text = check ? "COMPLETED\n" + completedDescription : (Ready ? "WORKSHOP COMPLETE\n" : "WORKSHOP — NEXT\n") + NextAction();
        goal.color = check ? new Color(.55f, 1f, .65f) : Color.white;
        DEF_UIPresentationPolicy.SetCompletionMark(goal, check);
        objective.localScale = Vector3.one * (1 + (check ? .035f * Mathf.Sin(Mathf.Clamp01((Time.unscaledTime - completedAt) / 1.6f) * Mathf.PI) : 0));
        if (visible) noticeRemaining = Mathf.Max(0, noticeRemaining - Time.unscaledDeltaTime);
        noticePanel.SetActive(noticeRemaining > 0);
    }
    string NextAction()
    {
        if (Ready)
        {
            var state = DEF_Progression.Current.State;
            bool power = state.HasPrerequisite(DEF_ProgressionState.Prerequisite.Power6);
            bool clinic = state.HasPrerequisite(DEF_ProgressionState.Prerequisite.Clinic10);
            return "Take extra Ammo Boxes and RepairKits to Defense. " +
                (power && clinic ? "All station requirements are verified; return to Defense." :
                !power && !clinic ? "Finish Power and Clinic objectives, then return to Defense." :
                !power ? "Finish the Power objective, then return to Defense." : "Finish the Clinic objective, then return to Defense.");
        }
        if (progression.CurrentStage == SCV_ProgressionManager.WorkshopStage.HarvestSalvage)
            return "Empty a salvage wreck: aim an empty hand at its HARVEST button and squeeze/release trigger repeatedly. The whole wreck must disappear.";
        if (progression.CurrentStage == SCV_ProgressionManager.WorkshopStage.ProcessPart)
            return crusher != null && crusher.IsProcessing ? "Crusher is processing. Wait for the first Part to finish." :
                "At the Crusher, aim at PROCESS SCRAP and squeeze/release trigger once. 2 Scrap makes 1 Part; wait for processing to finish.";
        bool ammo = readiness != null && readiness.FreshAmmoCrafted;
        bool kit = readiness != null && readiness.FreshRepairCrafted;
        if (!ammo || !kit)
        {
            int cost = bench != null ? (!ammo ? bench.AmmoPartsCost : bench.RepairPartsCost) : (!ammo ? 1 : 2);
            string item = !ammo ? "AMMO" : "REPAIR KIT";
            int parts = resources != null ? resources.parts : 0;
            if (parts < cost && crusher != null && crusher.IsProcessing) return "Crusher is processing. Wait for the Part, then make " + item + " at the Workbench.";
            if (parts < cost) return "Make Parts at PROCESS SCRAP for " + item + ": need " + cost + ", have " + parts + ". Each process uses 2 Scrap. Harvest more salvage if needed.";
            return "At the Workbench, aim at CRAFT " + item + " and squeeze/release trigger. Cost: " + cost + " Parts. Keep the physical item that appears.";
        }
        if (readiness != null && readiness.PracticeDeliveryReceived) return "Practice delivery accepted. Waiting for Workshop readiness verification.";
        return "Grip a crafted Ammo Box or RepairKit. Release the Ammo at AMMO STORAGE, or the kit at DAMAGED PANEL. One practice delivery is enough; neither target must be full.";
    }
    public void ShowUnlock(string message)
    {
        var camera = PlayerCamera();
        if (canvas == null && camera != null) Build(camera);
        if (notice == null) return;
        notice.text = message;
        noticeRemaining = DEF_UIPresentationPolicy.ReadSeconds(message);
    }
    void Build(Camera camera)
    {
        var root = new GameObject("Workshop context objective", typeof(RectTransform), typeof(Canvas));
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
        root.transform.SetParent(camera.transform, false); root.transform.localPosition = new Vector3(0, 0, 1.15f);
        root.transform.localScale = Vector3.one * .001f;
        objective = Panel(root.transform, "Workshop objective", new Vector2(600, 260), new Vector2(550, 250));
        goal = Text(objective, "Next action", 24);
        noticePanel = Panel(root.transform, "Workshop unlock", new Vector2(600, 230), new Vector2(550, -100)).gameObject;
        notice = Text(noticePanel.transform, "Unlock instruction", 24);
        noticePanel.SetActive(false);
    }
    static RectTransform Panel(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition = position;
        var image = go.GetComponent<Image>(); image.color = new Color(.015f, .02f, .025f, .94f); image.raycastTarget = false;
        return rect;
    }
    static TMP_Text Text(Transform parent, string name, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var text = go.GetComponent<TextMeshProUGUI>(); text.transform.SetParent(parent, false);
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(20, 14); text.rectTransform.offsetMax = new Vector2(-20, -14);
        text.fontSize = size; text.alignment = TextAlignmentOptions.TopLeft; text.textWrappingMode = TextWrappingModes.Normal;
        text.color = Color.white; text.raycastTarget = false; return text;
    }
    void OnDisable() { if (canvas != null) canvas.enabled = false; }
    void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
}
