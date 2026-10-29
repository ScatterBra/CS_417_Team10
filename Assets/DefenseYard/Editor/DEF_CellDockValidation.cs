using System;
using System.Reflection;
using Deadzone.Power;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Edit Mode scan/lifecycle contract checks, not trigger, physics or headset evidence.
public static class DEF_CellDockValidation
{
    [MenuItem("Defense Yard/MP2C/Validate Cell Dock Contracts")]
    public static void Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode only.");
        // The production scan is global: never allow this fixture to modify open scene traps.
        if (UnityEngine.Object.FindObjectsByType<DEF_ShockTrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0)
            throw new InvalidOperationException("Run dock contracts with no existing scene traps loaded.");
        var previous = SceneManager.GetActiveScene();
        var randomState = UnityEngine.Random.state;
        var singleton = typeof(PWR_DefenseCellDock).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var oldInstance = singleton.GetValue(null);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        int passed = 0;
        void Check(bool condition, string label)
        {
            if (!condition) throw new Exception("FAIL Cell Dock: " + label);
            passed++;
            Debug.Log("PASS Cell Dock: " + label);
        }
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var scan = typeof(PWR_DefenseCellDock).GetMethod("OverchargeTraps", flags);
        var teardown = typeof(PWR_DefenseCellDock).GetMethod("OnDestroy", flags);
        PWR_DefenseCellDock dock = null;
        try
        {
            if (Resources.Load<DEF_Effects>("Prefabs/DEF_ShockArc") == null)
                throw new Exception("Required Shock arc fixture resource missing; hydrate project assets first.");
            var dockObject = new GameObject("Dock contract fixture");
            dockObject.SetActive(false);
            dock = dockObject.AddComponent<PWR_DefenseCellDock>();
            // Keep Awake/BuildVisuals out of Edit Mode. Invoke only the real scan and cleanup contracts.
            Check(dockObject.transform.childCount == 0, "inactive fixture did not create dock visuals");
            var preview = new GameObject("Disabled active placement preview").AddComponent<DEF_ShockTrap>();
            preview.enabled = false;
            scan.Invoke(dock, null);
            Check(preview.gameObject.activeInHierarchy && preview.rechargeSeconds == 7f && dock.OverchargedCount == 0,
                "active disabled preview is neither boosted nor counted");
            var first = new GameObject("First real enabled trap").AddComponent<DEF_ShockTrap>();
            first.rechargeSeconds = 7f;
            scan.Invoke(dock, null);
            Check(first.rechargeSeconds == 4f && dock.OverchargedCount == 1, "enabled real trap receives four-second recharge");
            scan.Invoke(dock, null);
            Check(dock.OverchargedCount == 1, "repeat scan does not duplicate registration");
            var later = new GameObject("Later enabled trap").AddComponent<DEF_ShockTrap>();
            later.rechargeSeconds = 9f;
            scan.Invoke(dock, null);
            Check(later.rechargeSeconds == 4f && dock.OverchargedCount == 2, "later trap receives boost");
            UnityEngine.Object.DestroyImmediate(first.gameObject);
            scan.Invoke(dock, null);
            Check(dock.OverchargedCount == 1, "destroyed trap removed from count");
            // Unity does not promise OnDestroy for a never-active object; explicitly exercise that callback.
            teardown.Invoke(dock, null);
            Check(later.rechargeSeconds == 9f, "dock teardown restores each surviving trap's original recharge");
            UnityEngine.Object.DestroyImmediate(dockObject);
            dock = null;
            Check(later.rechargeSeconds == 9f && preview.rechargeSeconds == 7f, "destruction preserves restored trap and unmodified preview");
            Debug.Log("CELL DOCK CONTRACTS: " + passed + " PASS");
        }
        finally
        {
            if (dock != null) teardown.Invoke(dock, null);
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            singleton.SetValue(null, oldInstance);
            UnityEngine.Random.state = randomState;
        }
    }
}
