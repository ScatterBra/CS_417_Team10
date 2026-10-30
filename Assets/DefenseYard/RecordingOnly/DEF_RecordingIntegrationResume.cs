#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Deadzone.Power;

// RECORDING-ONLY PREPARED CHECKPOINT. Never merge this file into main.
// These starting objects/flags are explicit debug fixtures, NOT evidence that a player
// completed the earlier stations, crafted this first dose, or delivered this first box.
// No historical completion/craft/delivery receipts or achievement events are replayed.
public sealed class DEF_RecordingIntegrationResume : MonoBehaviour
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register() { SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initial() => Install(SceneManager.GetActiveScene());
    static void Loaded(Scene scene, LoadSceneMode mode) => Install(scene);
    static void Install(Scene scene)
    {
        if (scene.name != "Deadzone_Final") return;
        foreach (var existing in FindObjectsByType<DEF_RecordingIntegrationResume>(FindObjectsSortMode.None))
            if (existing.gameObject.scene == scene) return;
        var go = new GameObject("RECORDING ONLY - prepared integration checkpoint");
        SceneManager.MoveGameObjectToScene(go, scene); go.AddComponent<DEF_RecordingIntegrationResume>();
    }
    IEnumerator Start()
    {
        // Normal Awake/OnEnable/Start must finish first, especially Clinic evidence generation.
        yield return null; yield return null;
        float deadline = Time.realtimeSinceStartup + 12;
        while ((DEF_Progression.Current == null || DEF_Pathfinder.I == null || !DEF_Pathfinder.I.Ready ||
                PWR_DefenseCellDock.Instance == null || FindAnyObjectByType<DEF_ClinicMedicineDelivery>() == null ||
                FindAnyObjectByType<DEF_ProgressionPresenter>() == null || !FindAnyObjectByType<DEF_ProgressionPresenter>().Ready) && Time.realtimeSinceStartup < deadline)
            yield return null;
        try { Seed(); Debug.LogWarning("RECORDING RESUME READY: prepared prior state, not prior-milestone evidence. Subsequent gameplay uses normal consumption and waves."); }
        catch (Exception error) { Debug.LogError("RECORDING RESUME FAILED. Do not record this partial fixture: " + error); }
        // Deliberately no Update/retry reseeding. Spending and depleted ammunition stay real.
    }
    void Seed()
    {
        var progression = Need(DEF_Progression.Current, "Defense progression");
        var game = Need(DEF_GameState.I, "Defense state");
        var clinic = Need(FindAnyObjectByType<DEF_ClinicTraining>(), "Clinic training");
        Need(PWR_DefenseCellDock.Instance, "Power dock");
        var bench = Need(FindAnyObjectByType<SCV_Workbench>(FindObjectsInactive.Include), "Workshop workbench");
        var ledger = Need(Get<SCV_ResourceManager>(bench, "resourceManager"), "Workshop inventory");
        var workshop = Need(FindAnyObjectByType<SCV_ProgressionManager>(), "Workshop progression");
        workshop.CancelInvoke();
        Set(workshop, "currentStage", SCV_ProgressionManager.WorkshopStage.WorkshopComplete);
        Set(workshop, "craftedAmmo", true); Set(workshop, "craftedRepairKit", true);
        foreach (string field in new[] { "crusherInteraction", "workbenchInteraction", "defenseSupplyInteraction" })
            Need(Get<GameObject>(workshop, field), field).SetActive(true);
        var reveal = Get<GameObject>(workshop, "revealCanvas"); if (reveal != null) reveal.SetActive(false);
        Call(workshop, "UpdateObjective");
        var workshopHUD = workshop.GetComponent<SCV_IntegratedObjective>();
        if (workshopHUD != null) { Set(workshopHUD, "observed", false); Set(workshopHUD, "completedAt", -100f); }
        ledger.AddScrap(200); ledger.AddParts(60);

        // Prepared gate flags only: no AcceptPrerequisite or fake receipt IDs.
        var ready = Get<HashSet<DEF_ProgressionState.Prerequisite>>(progression.State, "ready");
        foreach (DEF_ProgressionState.Prerequisite p in Enum.GetValues(typeof(DEF_ProgressionState.Prerequisite))) ready.Add(p);
        Property(progression.State, "Current", DEF_ProgressionState.Stage.JuggernautReady);
        game.playerHealth = game.maxHealth; game.ammo = 30; game.scrap = 700; game.waveNumber = 2;
        Call(progression, "Refresh");
        SeedClinic(clinic, progression);
        SeedPower();
        SpawnWorkshopItem(bench, ledger, "ammoBoxPrefab", new Vector3(-1.2f, 1.1f, -1.7f), false);
        SpawnWorkshopItem(bench, ledger, "repairKitPrefab", new Vector3(1.2f, 1.1f, -1.7f), true);
        PlaceStarter(false); PlaceStarter(true); // Prepared defenses only; no finale evidence is seeded.
        game.scrap = 500; game.ammo = 30;
        Set(progression, "retryScrap", (int?)500); Set(progression, "retryAmmo", 30);
        Call(progression, "Refresh");
        MoveToOutpost();
    }
    void SeedClinic(DEF_ClinicTraining training, DEF_Progression progression)
    {
        var readiness = Need(training.GetComponent<DEF_ClinicReadiness>(), "Clinic readiness");
        var aid = Need(training.GetComponent<DEF_ClinicFieldAid>(), "Field Aid");
        var delivery = Need(training.GetComponent<DEF_ClinicMedicineDelivery>(), "Medicine delivery");
        var patient = Need(training.patient, "Clinic player inventory");
        Property(training, "Step", 10); Set(training, "pendingStep", 0);
        Set(training, "unloaded", true); Set(training, "randomFound", true); Set(training, "placed", true);
        var hud = FindAnyObjectByType<MED_PlayerHUD>();
        if (hud != null)
        {
            Set(hud, "clinicCompleted", true); Set(hud, "observedStep", 10); Set(hud, "completedAt", -100f);
            Set(hud, "previousIntroduction", false); Call(hud, "ClearStaleFeedback");
            Call(Get<object>(hud, "introductionQueue"), "Clear");
            Set(hud, "currentIntroduction", null); Set(hud, "introductionRemaining", 0f);
        }
        // BeginSession already armed the real future-build/craft observers. Keep those bindings intact.
        Set(readiness, "published", true); Set(readiness, "complete", true); Set(readiness, "expectedHealth", 100);
        Property(readiness, "PendingReason", "Recording checkpoint: Clinic tutorial prepared. Craft extra Medicine for subsequent real deliveries.");
        patient.SetHealth(100); patient.Add(MED_ResourceType.Herbs, 20); patient.Add(MED_ResourceType.Water, 10);
        Set(aid, "unlocked", true); Set(aid, "unlockedSession", progression.State.Session); Set(aid, "pendingUnlockNotice", false);
        var source = new GameObject("RECORDING SEED SOURCE - not a performed Medicine craft"); source.transform.SetParent(transform, false);
        var seedBench = source.AddComponent<MED_PrepBench>(); seedBench.enabled = false;
        Get<HashSet<MED_PrepBench>>(readiness, "eligibleBenches").Add(seedBench);
        var dose = new MED_MedicineUnit(patient, seedBench, training.medicineRecipe, patient.EvidenceGeneration);
        var spawner = Need(FindAnyObjectByType<MED_DeliverySpawner>(), "Clinic supply-cart spawner");
        var route = Need(DEF_ClinicCourierRoute.Ensure(training, Get<MED_DeliveryPath>(spawner, "path")), "Courier route");
        Need(route.Route, route.FailureReason); Need(route.DefenseTerminal, "Medical bay");
        Set(delivery, "route", route); Call(delivery, "EnsureControls");
        Set(delivery, "unit", dose); SetEnum(delivery, "state", "Delivered");
        var package = GameObject.CreatePrimitive(PrimitiveType.Cube);
        package.name = "RECORDING PREPARED Medicine - not a recorded delivery";
        package.transform.position = route.DefenseTerminal.position + new Vector3(1.15f, 1, 0);
        package.transform.localScale = new Vector3(.5f, .4f, .5f);
        package.GetComponent<Renderer>().sharedMaterial = Get<Material>(delivery, "blue");
        package.GetComponent<Collider>().enabled = false;
        Set(delivery, "cargoVisual", package); Call(delivery, "DockDeliveredPackage");
        if (!delivery.Delivered || !aid.IsQualified) throw new InvalidOperationException("Prepared Clinic dose is not usable; no success claim.");
    }
    void SeedPower()
    {
        var dock = Need(PWR_DefenseCellDock.Instance, "Defense Cell Dock");
        var charger = Need(FindAnyObjectByType<PWR_CellCharger>(FindObjectsInactive.Include), "Power charger");
        var cell = Instantiate(Need(charger.chargedCellPrefab, "Charged Cell prefab"));
        cell.name = "RECORDING PREPARED Charged Cell - not a recorded charge";
        var grab = Need(cell.GetComponent<PWR_Grabbable>(), "Charged Cell grab component");
        Property(charger, "ChargedCell", grab); Property(charger, "IsComplete", true); Property(charger, "IsCellCollected", true);
        Property(charger, "HasBattery", false); charger.Locked = false;
        if (charger.slottedBatteryVisual != null) charger.slottedBatteryVisual.SetActive(false);
        typeof(PWR_LevelDirector).GetProperty("StationComplete", BindingFlags.Static | BindingFlags.Public).GetSetMethod(true).Invoke(null, new object[] { true });
        var director = FindAnyObjectByType<PWR_LevelDirector>(); if (director != null) { director.CancelInvoke("ShowIntro"); Set(director, "introShown", true); }
        var power = FindAnyObjectByType<PWR_Progression>();
        if (power != null)
        {
            power.StopAllCoroutines(); Set(power, "unlocking", false); Property(power, "Stage", PWR_Stage.Online);
            foreach (string field in new[] { "chute", "bin", "tank" }) { var content = Get<GameObject>(power, field); if (content != null) content.SetActive(true); }
            var powerBench = Get<PWR_Workbench>(power, "workbench");
            if (powerBench != null) foreach (var recipe in powerBench.recipes) recipe.locked = false;
            if (PWR_BuildSystem.Instance != null) foreach (string id in new[] { "CoalGenerator", "OilGenerator", "ConcretePad", "ConveyorBelt" }) PWR_BuildSystem.Instance.Unlock(id);
        }
        // Copy only the dock's persistent physical state, deliberately NOT its CellDocked event.
        Property(dock, "IsDocked", true); Set(dock, "dockedCell", grab);
        var xr = cell.GetComponent<XRGrabInteractable>(); if (xr != null) xr.enabled = false;
        foreach (var collider in cell.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        grab.Body.isKinematic = true; cell.transform.SetParent(dock.transform, false);
        cell.transform.localPosition = dock.mountOffset; cell.transform.localRotation = Quaternion.identity;
        Get<GameObject>(dock, "beacon").SetActive(false); Set(dock, "nextScan", 0f);
        Call(dock, "Tint", Get<Renderer>(dock, "ring"), new Color(.25f, .9f, 1));
        Call(dock, "UpdateLabel");
    }
    void SpawnWorkshopItem(SCV_Workbench bench, SCV_ResourceManager ledger, string prefabField, Vector3 offset, bool repair)
    {
        var prefab = Need(Get<GameObject>(bench, prefabField), prefabField);
        if (repair) ledger.AddRepairKit(1); else ledger.AddAmmo(1);
        var at = Need(DEF_BaseStructure.Instance, "Outpost").transform.position + offset;
        var go = Instantiate(prefab, at, Quaternion.identity); go.name = "RECORDING PREPARED " + prefabField;
        if (!Need(go.GetComponentInChildren<SCV_CraftedItem>(true), "Physical Workshop representation").BindLedger(ledger))
            throw new InvalidOperationException("Prepared physical item could not bind its finite inventory unit.");
    }
    void PlaceStarter(bool missile)
    {
        var grid = Need(FindAnyObjectByType<DEF_GridManager>(), "Defense grid");
        var baseAt = Need(DEF_BaseStructure.Instance, "Outpost").transform.position;
        int preferredX = missile ? 13 : 11, preferredZ = missile ? 11 : 13;
        if (DEF_Turret.TryPlace(preferredX, preferredZ, missile))
        {
            var prepared = DEF_Pathfinder.I.OccupantAt(preferredX, preferredZ).GetComponent<DEF_Turret>();
            if (!missile) prepared.health = Mathf.Max(1, prepared.maxHealth - 40);
            return;
        }
        for (int z = 0; z < grid.depth; z++) for (int x = 0; x < grid.width; x++)
        {
            Vector3 at = grid.TileCenter(x, z); float distance = Vector3.Distance(at, baseAt);
            if (distance < 5 || distance > 8 || at.z < baseAt.z) continue;
            if (!DEF_Turret.TryPlace(x, z, missile)) continue;
            var placed = DEF_Pathfinder.I.OccupantAt(x, z).GetComponent<DEF_Turret>();
            if (!missile) placed.health = Mathf.Max(1, placed.maxHealth - 40);
            return;
        }
        throw new InvalidOperationException("No valid starter defense placement; place defenses manually before recording.");
    }
    void MoveToOutpost()
    {
        var rig = Need(PWR_PlayerRig.Instance, "Player rig"); var origin = Need(rig.origin, "Player origin");
        DEF_RecordingMovementGuard.Install(origin); // Keep a valid collision capsule if startup tracking reports height zero.
        var camera = DEF_VRInput.Instance != null && DEF_VRInput.Instance.xrCamera != null ? DEF_VRInput.Instance.xrCamera : Camera.main;
        if (camera == null || !camera.transform.IsChildOf(origin)) throw new InvalidOperationException("Cannot safely locate current player camera.");
        Vector3 destination = DEF_BaseStructure.Instance.transform.position + new Vector3(0, 0, 5);
        Vector3 delta = destination - camera.transform.position; delta.y = 0;
        var controllers = origin.GetComponentsInChildren<CharacterController>();
        var enabledControllers = new List<CharacterController>();
        foreach (var controller in controllers) if (controller.enabled) { controller.enabled = false; enabledControllers.Add(controller); }
        try { origin.position += delta; }
        finally { foreach (var controller in enabledControllers) if (controller != null) controller.enabled = true; }
    }
    static T Need<T>(T value, string name) where T : UnityEngine.Object => value != null ? value : throw new InvalidOperationException("Missing " + name);
    static FieldInfo Field(object owner, string name) => owner.GetType().GetField(name, Flags) ?? throw new MissingFieldException(owner.GetType().Name, name);
    static T Get<T>(object owner, string name) => (T)Field(owner, name).GetValue(owner);
    static void Set(object owner, string name, object value) => Field(owner, name).SetValue(owner, value);
    static void SetEnum(object owner, string name, string value) { var field = Field(owner, name); field.SetValue(owner, Enum.Parse(field.FieldType, value)); }
    static void Property(object owner, string name, object value) => (owner.GetType().GetProperty(name, Flags) ?? throw new MissingMemberException(name)).GetSetMethod(true).Invoke(owner, new[] { value });
    static object Call(object owner, string name, params object[] args) => (owner.GetType().GetMethod(name, Flags) ?? throw new MissingMethodException(name)).Invoke(owner, args);
}
#endif
