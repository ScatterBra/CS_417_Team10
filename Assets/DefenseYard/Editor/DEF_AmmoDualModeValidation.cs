using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class DEF_AmmoDualModeValidation
{
    [MenuItem("Defense Yard/MP2B/Run Dual Mode Edit Mode Checks")]
    public static void Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before these isolated Edit Mode checks.");
        var results = new List<string>();
        var roots = new List<GameObject>();
        var previousState = DEF_GameState.I;
        var previousScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var testScene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(testScene);
        GameObject Make(string name) { var go = new GameObject(name) { hideFlags = HideFlags.HideInHierarchy }; roots.Add(go); return go; }
        void Check(bool ok, string name) { results.Add((ok ? "PASS " : "FAIL ") + name); }
        void Set(object target, string name, object value) => target.GetType().GetField(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(target, value);
        try
        {
            var ledger = Make("Test ledger").AddComponent<SCV_ResourceManager>();
            var state = Make("Test state").AddComponent<DEF_GameState>();
            DEF_GameState.I = state;
            var link = Make("Test remote link").AddComponent<SCV_DefenseSupplyLink>();
            Set(link, "resourceManager", ledger); Set(link, "defenseAmmoPerBox", 7);
            SCV_CraftedItem Box(SCV_ResourceManager source)
            {
                var go = Make("Test physical unit"); go.AddComponent<BoxCollider>(); go.AddComponent<Rigidbody>().useGravity = false;
                var item = go.AddComponent<SCV_CraftedItem>();
                if (!item.BindLedger(source)) throw new Exception("Test registration failed");
                return item;
            }
            ledger.ResetAmmoInventory(1); state.ammo = 20;
            link.DeliverAmmo();
            Check(ledger.ammo == 0 && state.ammo == 27, "remote serialized legacy count works without intake and respects seven-round config");
            ledger.ResetAmmoInventory(1); var first = Box(ledger); state.ammo = 20;
            link.DeliverAmmo();
            Check(first == null && ledger.ammo == 0 && state.ammo == 27, "remote retires its exact represented unit");
            Check(!ledger.TryConsumePhysicalAmmo(first, ref state.ammo, 60, 10, out _), "remote-first physical repeat rejected");
            ledger.ResetAmmoInventory(1); var physical = Box(ledger); state.ammo = 20;
            Check(ledger.TryConsumePhysicalAmmo(physical, ref state.ammo, 60, 10, out _) && state.ammo == 30, "physical delivery succeeds");
            link.DeliverAmmo(); Check(ledger.ammo == 0 && state.ammo == 30, "physical-first remote repeat rejected");
            ledger.ResetAmmoInventory(1); var full = Box(ledger); state.ammo = 60;
            link.DeliverAmmo(); Check(full != null && !full.Consumed && ledger.ammo == 1 && state.ammo == 60, "remote full preserves unit and representation");
            state.ammo = 20; state.enabled = false; link.DeliverAmmo();
            Check(full != null && ledger.ammo == 1 && state.ammo == 20, "remote offline destination preserves states"); state.enabled = true;
            var other = Make("Other ledger").AddComponent<SCV_ResourceManager>(); other.ResetAmmoInventory(1); var otherBox = Box(other);
            link.DeliverAmmo(); Check(full == null && otherBox != null && other.ammo == 1, "remote selection cannot retire another ledger representation");
            Check(!ledger.TryConsumePhysicalAmmo(otherBox, ref state.ammo, 60, 10, out _), "foreign-ledger physical delivery rejected");
            ledger.ResetAmmoInventory(1); var lost = Box(ledger); UnityEngine.Object.DestroyImmediate(lost.gameObject); state.ammo = 20;
            link.DeliverAmmo(); Check(ledger.ammo == 0 && state.ammo == 27, "missing representation remains usable once through remote inventory");
            ledger.ResetAmmoInventory(1); var stale = Box(ledger); ledger.ResetAmmoInventory(1); state.ammo = 20;
            Check(!ledger.TryConsumePhysicalAmmo(stale, ref state.ammo, 60, 10, out _) && ledger.ammo == 1, "same-count explicit reset invalidates old physical identity");
            link.DeliverAmmo(); Check(stale != null && !stale.Consumed && ledger.ammo == 0, "reset remote inventory does not destroy stale unrelated prop");
            ledger.ResetAmmoInventory(1); var again = Box(ledger); state.ammo = 20;
            bool blocked = false, later = false;
            Action reentry = () => { int d = 0; blocked = !ledger.TryTransferRemoteAmmo(ref d, 60, 10, out _) && !ledger.ResetAmmoInventory(10); };
            Action throwing = () => throw new Exception("intentional test subscriber");
            Action tail = () => later = true;
            ledger.OnResourcesChanged += reentry; ledger.OnResourcesChanged += throwing; ledger.OnResourcesChanged += tail;
            link.DeliverAmmo(); Check(blocked && later && again == null && ledger.ammo == 0 && state.ammo == 27, "remote publication isolates subscriber failure and rejects reentry/reset");
            ledger.OnResourcesChanged -= reentry; ledger.OnResourcesChanged -= throwing; ledger.OnResourcesChanged -= tail;
            ledger.ResetAmmoInventory(1); var held = Box(ledger); var grab = held.gameObject.AddComponent<XRGrabInteractable>();
            var manager = Make("Test XRI manager").AddComponent<XRInteractionManager>();
            var hand = Make("Test XRI interactor").AddComponent<XRDirectInteractor>();
            // Edit Mode does not invoke runtime Awake; initialize only these isolated fixtures.
            typeof(XRGrabInteractable).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(grab,null); typeof(XRDirectInteractor).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(hand,null);
            hand.interactionManager = manager; grab.interactionManager = manager;
            manager.RegisterInteractor((IXRInteractor)hand); manager.RegisterInteractable((IXRInteractable)grab);
            manager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)grab);
            Check(grab.isSelected, "XRI select lifecycle marks test representation held (not simulator input)");
            state.ammo = 20; link.DeliverAmmo();
            Check(held != null && ledger.ammo == 1 && state.ammo == 20, "remote transfer does not consume held representation");
            manager.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)grab);
            link.DeliverAmmo(); Check(held == null && ledger.ammo == 0 && state.ammo == 27, "released representation can be transferred remotely");
            var unbound = Make("Unbound legacy prop").AddComponent<SCV_CraftedItem>(); ledger.ResetAmmoInventory(1);
            Check(!ledger.TryConsumePhysicalAmmo(unbound, ref state.ammo, 60, 10, out _), "unbound legacy prop is not silently assigned provenance");
            Check(unbound.BindLedger(ledger), "explicit legacy source registration reserves existing inventory without minting ammo");
            Check(ledger.TryConsumePhysicalAmmo(unbound, ref state.ammo, 60, 10, out _) && ledger.ammo == 0, "explicitly registered legacy prop consumes once");
        }
        catch (Exception e) { results.Add("FAIL exception " + e); }
        finally
        {
            foreach (var go in roots) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            DEF_GameState.I = previousState;
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(testScene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousScene);
            System.IO.File.WriteAllLines("../ammo-dualmode-editmode-results.txt", results);
            Debug.Log(string.Join("\n", results));
        }
    }
}


