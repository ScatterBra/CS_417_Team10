using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

[InitializeOnLoad]
public static class DEF_AmmoValidation
{
    const string Flag = "DEF.AmmoValidation";
    static int failures;
    static SCV_ResourceManager testLedger;
    static System.Collections.Generic.List<string> results = new();
    static DEF_AmmoValidation() { EditorApplication.playModeStateChanged += Changed; }

    [MenuItem("Defense Yard/MP2B/Create Ammo Demo")]
    public static void CreateDemo() { TryCreateDemo(); }
    static bool TryCreateDemo()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.OpenScene("Assets/Development/Scenes/Workshop/SCV_ScrapyardWorkshop_LOCAL_TEST.unity");
        var ledger = UnityEngine.Object.FindFirstObjectByType<SCV_ResourceManager>();
        var state = UnityEngine.Object.FindFirstObjectByType<DEF_GameState>();
        state.ammo = 30;
        var bench = UnityEngine.Object.FindFirstObjectByType<SCV_Workbench>();
        var crafting=UnityEngine.Object.FindAnyObjectByType<SCV_ProgressionManager>();
        var readiness=crafting.gameObject.AddComponent<DEF_WorkshopReadiness>();
        var readinessData=new SerializedObject(readiness);
        readinessData.FindProperty("crafting").objectReferenceValue=crafting;
        readinessData.FindProperty("workbench").objectReferenceValue=bench;
        readinessData.FindProperty("teachingReceiver").objectReferenceValue=UnityEngine.Object.FindAnyObjectByType<SCV_AmmoReceiver>();
        readinessData.FindProperty("teachingRepairReceiver").objectReferenceValue=UnityEngine.Object.FindAnyObjectByType<SCV_RepairReceiver>();
        readinessData.ApplyModifiedPropertiesWithoutUndo();
        var go = new GameObject("DEF Workshop Ammo Intake - release box here");
        go.transform.position = bench.transform.position + new Vector3(1.2f, 1f, 0);
        var receiver = go.AddComponent<DEF_WorkshopAmmoReceiver>();
        receiver.ledger = ledger; receiver.destination = state;
        go.GetComponent<BoxCollider>().size = new Vector3(.8f,.8f,.8f);
        go.GetComponent<BoxCollider>().isTrigger = true;
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Intake platform";
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0,-.42f,0);
        visual.transform.localScale = new Vector3(.8f,.08f,.8f);
        var label = new GameObject("Intake instructions").AddComponent<TextMeshPro>();
        label.transform.SetParent(go.transform, false);
        label.transform.localPosition = new Vector3(0,.7f,0);
        label.fontSize = 2; label.rectTransform.sizeDelta = new Vector2(3,1);
        label.text = "DEFENSE AMMO INTAKE\nRelease crafted box here.\nStarts at 30/60; each box adds up to 10.";
        receiver.feedbackText = label;
        var link = UnityEngine.Object.FindFirstObjectByType<SCV_DefenseSupplyLink>();
        var so = new SerializedObject(link);
        so.FindProperty("ammoIntake").objectReferenceValue = receiver;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Development/Scenes/Defense/DEF_WorkshopAmmo_Demo.unity");
        AssetDatabase.SaveAssets();
        return true;
    }
    [MenuItem("Defense Yard/MP2B/Run Ammo Validation")]
    public static void BuildAndRun()
    {
        if (!TryCreateDemo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        SessionState.SetBool(Flag, true);
        EditorApplication.isPlaying = true;
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Flag, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) EditorApplication.delayCall += Run;
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Flag, false);
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(Flag + ".Exit", 1));
            else EditorApplication.delayCall += () => EditorSceneManager.OpenScene("Assets/Development/Scenes/Defense/DEF_WorkshopAmmo_Demo.unity");
        }
    }
    static void Check(bool condition, string name)
    {
        results.Add((condition ? "PASS " : "FAIL ") + name);
        if (!condition) failures++;
    }
    static SCV_CraftedItem Box()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SCV_ScrapyardWorkshop/Prefabs/SCV_AmmoBox.prefab");
        var box = UnityEngine.Object.Instantiate(prefab).GetComponent<SCV_CraftedItem>();
        box.BindLedger(testLedger);
        return box;
    }
    static void Run()
    {
        failures = 0;
        results.Clear();
        try
        {
            var ledger = new GameObject("Ledger").AddComponent<SCV_ResourceManager>();
            testLedger = ledger;
            var state = new GameObject("Defense").AddComponent<DEF_GameState>();
            var intake = new GameObject("Intake").AddComponent<DEF_WorkshopAmmoReceiver>();
            intake.ledger = ledger; intake.destination = state;
            state.ammo = 30; ledger.ammo = 4;
            var first = Box();
            Check(intake.TryDeliver(first) && state.ammo == 40 && ledger.ammo == 3 && first.Consumed && !first.gameObject.activeSelf, "real prefab success consumes exact box and ledger once");
            Check(!intake.TryDeliver(first) && state.ammo == 40 && ledger.ammo == 3, "same-frame repeated delivery rejected");
            var full = Box(); state.ammo = 60;
            Check(!intake.TryDeliver(full) && !full.Consumed && ledger.ammo == 3 && full.gameObject.activeSelf, "full capacity preserves box and ledger");
            state.ammo = 58;
            Check(intake.TryDeliver(full) && state.ammo == 60 && ledger.ammo == 2, "rejected box can retry; partial capacity adds only two");
            var empty = Box(); state.ammo = 30; ledger.ammo = 0;
            Check(!intake.TryDeliver(empty) && !empty.Consumed && state.ammo == 30, "empty ledger preserves physical box");
            ledger.ammo = 2; ledger.AddAmmo(1); intake.ledger = null;
            Check(!intake.TryDeliver(empty) && !empty.Consumed && ledger.ammo == 3, "missing ledger rejection");
            intake.ledger = ledger; intake.destination = null;
            Check(!intake.TryDeliver(empty) && !empty.Consumed, "offline Defense rejection");
            intake.destination = state;
            var invalid = new GameObject("Repair kit").AddComponent<SCV_CraftedItem>();
            typeof(SCV_CraftedItem).GetField("itemType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(invalid, SCV_CraftedItem.ItemType.RepairKit);
            Check(!intake.TryDeliver(invalid) && !invalid.Consumed && ledger.ammo == 3, "wrong item rejection");
            var foreign = new GameObject("Unbound box").AddComponent<SCV_CraftedItem>();
            Check(!intake.TryDeliver(foreign) && !foreign.Consumed && ledger.ammo == 3, "unbound physical box cannot spend another ledger");
            var competitor = new GameObject("Second intake").AddComponent<DEF_WorkshopAmmoReceiver>();
            competitor.ledger = ledger; competitor.destination = state;
            bool reentryRejected = false, laterListener = false;
            Action reentry = () => { reentryRejected = !intake.TryDeliver(empty) && !competitor.TryDeliver(empty) && !ledger.SpendAmmo(1); };
            Action throwing = () => throw new Exception("intentional validation listener");
            Action later = () => laterListener = true;
            ledger.OnResourcesChanged += reentry; ledger.OnResourcesChanged += throwing; ledger.OnResourcesChanged += later;
            Check(intake.TryDeliver(empty) && reentryRejected && laterListener && ledger.ammo == 2 && state.ammo == 40, "notification failure isolated; reentry blocked; later listeners run");
            ledger.OnResourcesChanged -= reentry; ledger.OnResourcesChanged -= throwing; ledger.OnResourcesChanged -= later;
            var retry = Box();
            int fullDestination = 10;
            Check(!ledger.TryConsumePhysicalAmmo(retry, ref fullDestination, 10, 1, out _) && ledger.ammo == 2 && !retry.Consumed && fullDestination == 10, "destination rejection preserves all states");
            int unchanged = 0;
            Check(!ledger.TryConsumePhysicalAmmo(retry, ref unchanged, 10, 0, out _) && unchanged == 0 && ledger.ammo == 2 && !retry.Consumed, "invalid transfer size preserves all states");
            intake.enabled = false;
            Check(!intake.TryDeliver(retry) && ledger.ammo == 2 && !retry.Consumed, "disabled intake rejects direct calls");
            intake.enabled = true;
            Check(intake.TryDeliver(retry) && ledger.ammo == 1, "released claim retries successfully");
            state.ammo = 30;
            var fresh = Box();
            Check(intake.TryDeliver(fresh) && state.ammo == 40 && ledger.ammo == 0, "fresh item after state reset accepted without stale claim");
            intake.DeliverPresented();
            Check(intake.LastMessage.Contains("no box"), "button without presented item does not find remote boxes");
            ledger.ammo = 2; state.ammo = 30;
            var workshop = new GameObject("Workshop sink").AddComponent<SCV_AmmoTest>();
            typeof(SCV_AmmoTest).GetField("resourceManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(workshop, ledger);
            var shared = Box();
            Check(workshop.LoadPhysicalAmmo(shared) && !intake.TryDeliver(shared) && ledger.ammo == 1 && state.ammo == 30 && workshop.StoredAmmo == 1, "Workshop winner prevents Defense duplicate consumption");
            var defenseFirst = Box();
            Check(intake.TryDeliver(defenseFirst) && !workshop.LoadPhysicalAmmo(defenseFirst) && ledger.ammo == 0 && workshop.StoredAmmo == 1, "Defense winner prevents Workshop duplicate consumption");
        }
        catch (Exception e) { failures++; results.Add("FAIL exception " + e); }
        BeginPhysicsChecks();
    }
    static DEF_GameState physicsState;
    static SCV_ResourceManager physicsLedger;
    static SCV_CraftedItem physicsBox;
    static double physicsDeadline;
    static int physicsStage;
    static void BeginPhysicsChecks()
    {
        physicsLedger = new GameObject("Physics ledger").AddComponent<SCV_ResourceManager>();
        testLedger = physicsLedger;
        physicsLedger.ammo = 2;
        physicsState = new GameObject("Physics Defense").AddComponent<DEF_GameState>();
        physicsState.ammo = 60;
        var receiver = new GameObject("Physics intake").AddComponent<DEF_WorkshopAmmoReceiver>();
        receiver.transform.position = new Vector3(1000, 10, 1000);
        receiver.ledger = physicsLedger;
        receiver.destination = physicsState;
        physicsBox = Box();
        var body = physicsBox.GetComponent<Rigidbody>();
        body.useGravity = false;
        body.position = receiver.transform.position;
        var extra = new GameObject("Second collider");
        extra.transform.SetParent(physicsBox.transform, false);
        extra.AddComponent<BoxCollider>().size = Vector3.one * .1f;
        Physics.SyncTransforms();
        physicsStage = 0;
        physicsDeadline = EditorApplication.timeSinceStartup + 1.5;
        EditorApplication.update += PollPhysics;
    }
    static void PollPhysics()
    {
        if (EditorApplication.timeSinceStartup < physicsDeadline) return;
        if (physicsStage == 0)
        {
            Check(physicsBox != null && !physicsBox.Consumed && physicsLedger.ammo == 2 && physicsState.ammo == 60,
                "real physics multiple contacts at full capacity preserve box and ledger");
            physicsState.ammo = 45;
            physicsStage = 1;
            physicsDeadline = EditorApplication.timeSinceStartup + 1.5;
            return;
        }
        EditorApplication.update -= PollPhysics;
        Check(physicsBox == null && physicsLedger.ammo == 1 && physicsState.ammo == 55,
            "real trigger-stay retry with two colliders consumes exactly one box/unit");
        System.IO.File.WriteAllLines("../ammo-validation-results.txt", results);
        Debug.Log(string.Join("\n", results));
        SessionState.SetInt(Flag + ".Exit", failures == 0 ? 0 : 1);
        EditorApplication.isPlaying = false;
    }
}


