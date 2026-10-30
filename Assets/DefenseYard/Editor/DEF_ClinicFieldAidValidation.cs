using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Deadzone.Power;

// Disposable Edit Mode fixture; never grants resources or edits the user's live run.
public static class DEF_ClinicFieldAidValidation
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object o, string field, object value) => o.GetType().GetField(field, Private).SetValue(o, value);
    static object Call(object o, string method, params object[] args) => o.GetType().GetMethod(method, Private).Invoke(o, args);
    [MenuItem("Defense Yard/MP2B/Validate Clinic Field Aid")]
    public static void Run()
    {
        if (Application.isPlaying) throw new Exception("Edit Mode only; stop the live run before testing.");
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var results = new List<string>();
        var oldCameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        var enabledCameras = new List<Camera>(); foreach (var c in oldCameras) if (c.enabled) { enabledCameras.Add(c); c.enabled = false; }
        MED_Recipe recipe = null;
        PWR_PlayerRig oldRig = PWR_PlayerRig.Instance;
        var oldDefense = DEF_GameState.I;
        var hudField = typeof(MED_PlayerHUD).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        var oldHud = hudField.GetValue(null);
        bool oldArea = MED_AreaGate.PlayerInside;
        T Make<T>(string name) where T : Component => new GameObject(name).AddComponent<T>();
        void Check(bool ok, string name) { results.Add((ok ? "PASS " : "FAIL ") + name); }
        try
        {
            var hud = Make<MED_PlayerHUD>("Fixture feedback sink"); Call(hud, "Awake");
            typeof(MED_AreaGate).GetProperty("PlayerInside").GetSetMethod(true).Invoke(null, new object[] { true });
            var camera = Make<Camera>("Field Aid fixture camera"); camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 1, -.5f);
            var patient = Make<MED_PlayerResources>("Fixture resources");
            var bench = Make<MED_PrepBench>("Paid recipe bench"); Set(bench, "player", patient);
            var station = Make<MED_TreatmentStation>("Treatment station"); Set(station, "player", patient); Call(station, "OnEnable");
            recipe = ScriptableObject.CreateInstance<MED_Recipe>(); recipe.duration = 1;
            recipe.inputs.Add(new MED_ResourceCost { type = MED_ResourceType.Herbs, amount = 2 });
            recipe.inputs.Add(new MED_ResourceCost { type = MED_ResourceType.Water, amount = 1 });
            recipe.outputs.Add(new MED_ResourceCost { type = MED_ResourceType.Medicine, amount = 1 }); Set(bench, "recipes", new[] { recipe });
            var readiness = Make<DEF_ClinicReadiness>("Readiness"); readiness.patient = patient; readiness.bench = bench; readiness.station = station; readiness.medicineRecipe = recipe;
            Call(readiness, "OnEnable"); readiness.BeginSession(1);
            int receipts = 0; readiness.Completed += (_, __, ___) => receipts++;
            void Craft() { patient.Add(MED_ResourceType.Herbs, 2); patient.Add(MED_ResourceType.Water, 1); if (!bench.TryStartRecipe(recipe)) throw new Exception("Fixture recipe rejected"); Call(bench, "Complete"); }
            bool Transfer(Predicate<MED_MedicineUnit> filter, Action<MED_MedicineUnit> commit, bool current = true) =>
                (bool)Call(patient, "TransferMedicine", commit, current, filter);
            Predicate<MED_MedicineUnit> qualifies = unit => (bool)Call(readiness, "IsVerifiedMedicine", unit);
            Craft(); int before = patient.Get(MED_ResourceType.Medicine);
            Check(!Transfer(qualifies, _ => { }) && patient.Get(MED_ResourceType.Medicine) == before, "Field Aid cannot take required tutorial dose before actual readiness");
            for (int i = 0; i < 3; i++)
            {
                if (i > 0) Craft();
                Check(station.TryRefill(), "fresh tutorial dose stocks station " + i);
                Set(station, "playerOnPad", true); if (station.State == MED_TreatmentStation.StationState.Idle) station.TryTreat();
                Call(station, "CompleteDose");
            }
            Check(patient.Health == 100 && receipts == 1, "original three-dose self-treatment still publishes once");
            Craft(); int commits = 0; before = patient.Get(MED_ResourceType.Medicine);
            Check(Transfer(qualifies, _ => commits++) && commits == 1 && patient.Get(MED_ResourceType.Medicine) == before - 1 && patient.Health == 100,
                "qualified extra dose consumed once without healing or duplicate readiness");
            Craft(); before = patient.Get(MED_ResourceType.Medicine);
            Check(!Transfer(_ => false, _ => commits++) && patient.Get(MED_ResourceType.Medicine) == before, "rejected selector cannot debit or reinsert stock");

            var training = readiness.gameObject.AddComponent<DEF_ClinicTraining>(); training.patient = patient; training.medicineRecipe = recipe;
            Set(training, "pendingStep", 0); typeof(DEF_ClinicTraining).GetProperty("Step").GetSetMethod(true).Invoke(training, new object[] { 10 });
            var progression = Make<DEF_Progression>("Fixture progression");
            DEF_GameState.I = progression.GetComponent<DEF_GameState>();
            var aid = readiness.gameObject.AddComponent<DEF_ClinicFieldAid>();
            Set(aid, "training", training); Set(aid, "readiness", readiness); Set(aid, "progression", progression);
            Set(aid, "unlocked", true); Set(aid, "unlockedSession", progression.State.Session);
            var delivery = readiness.gameObject.AddComponent<DEF_ClinicMedicineDelivery>();
            Set(delivery, "training", training); Set(delivery, "readiness", readiness); Set(delivery, "aid", aid); Set(delivery, "nextScan", float.PositiveInfinity);
            var inbound = Make<MED_DeliveryPath>("Fixture inbound path"); inbound.SetWorldPolyline(new[] { Vector3.back * 2, Vector3.zero });
            var outbound = Make<MED_DeliveryPath>("Fixture outbound path"); outbound.SetWorldPolyline(new[] { Vector3.zero, Vector3.forward * 4 });
            var helper = readiness.gameObject.AddComponent<DEF_ClinicCourierRoute>(); Set(helper, "route", outbound); Set(helper, "originalInbound", inbound);
            var terminal = new GameObject("Fixture delivery terminal").transform; terminal.position = Vector3.forward * 4;
            typeof(DEF_ClinicCourierRoute).GetProperty("DefenseTerminal").GetSetMethod(true).Invoke(helper, new object[] { terminal });
            Set(delivery, "route", helper);
            MED_SupplyCart NewCart()
            {
                var c = Make<MED_SupplyCart>("Fixture cart");
                Set(c, "unloadButtons", Array.Empty<MED_ActionButton>()); Set(c, "wheels", Array.Empty<Transform>()); Set(c, "crates", Array.Empty<GameObject>());
                Set(c, "path", inbound); Set(c, "inboundPath", inbound); c.ConfigureDefenseExport(outbound); Call(c, "SetState", MED_SupplyCart.CartState.Waiting);
                Set(delivery, "cart", c); return c;
            }
            object Cargo() => typeof(DEF_ClinicMedicineDelivery).GetField("unit", Private).GetValue(delivery);
            string CargoState() => typeof(DEF_ClinicMedicineDelivery).GetField("state", Private).GetValue(delivery).ToString();
            var courier = NewCart(); before = patient.Get(MED_ResourceType.Medicine);
            Check(delivery.LoadOrReturn() && patient.Get(MED_ResourceType.Medicine) == before - 1 && courier.HoldForMedicine, "LOAD moves one authentic dose into held cargo");
            var identity = Cargo();
            Check(delivery.Send() && ReferenceEquals(identity, Cargo()) && !courier.HoldForMedicine, "SEND moves same dose into transit without second debit");
            Check(!delivery.Send() && ReferenceEquals(identity, Cargo()), "repeated SEND cannot create another shipment");
            var wrong = NewCart(); Call(wrong, "SetState", MED_SupplyCart.CartState.AtDefense); wrong.transform.position = terminal.position;
            Call(delivery, "Arrived", wrong); Check(!delivery.Delivered && ReferenceEquals(identity, Cargo()), "wrong-cart arrival cannot deliver cargo");
            Call(delivery, "Arrived", courier); Check(!delivery.Delivered, "arrival callback before actual endpoint rejected");
            Call(courier, "SetState", MED_SupplyCart.CartState.AtDefense); courier.transform.position = terminal.position;
            Call(delivery, "Arrived", courier); Call(delivery, "Arrived", courier);
            Check(delivery.Delivered && ReferenceEquals(identity, Cargo()), "real endpoint accepts once; duplicate callback cannot mint another dose");
            DEF_ClinicFieldAid.CancelForReset(progression, false); progression.State.RetryDefense();
            Check(delivery.Delivered && ReferenceEquals(identity, Cargo()), "Defense retry preserves delivered current-session Clinic stock");
            UnityEngine.Object.DestroyImmediate(courier.gameObject); Call(delivery, "Update");
            Check(delivery.Delivered, "destroying cart after arrival does not erase delivered stock");
            int uses = 0;
            Check(!delivery.TryConsumeDelivered(_ => uses++) && uses == 0, "Field Aid cannot consume remotely from Clinic");
            camera.transform.position = terminal.position + Vector3.up;
            Check(delivery.TryConsumeDelivered(_ => uses++) && uses == 1 && !delivery.TryConsumeDelivered(_ => uses++), "delivered dose consumed exactly once at Defense");
            camera.transform.position = new Vector3(0, 1, -.5f); Craft(); courier = NewCart(); delivery.LoadOrReturn(); identity = Cargo(); delivery.Send();
            UnityEngine.Object.DestroyImmediate(courier.gameObject); Call(delivery, "Update");
            Check(CargoState() == "Recoverable" && ReferenceEquals(identity, Cargo()), "destroyed outbound cart retains exact recoverable cargo without delivery");
            before = patient.Get(MED_ResourceType.Medicine);
            Check(delivery.LoadOrReturn() && patient.Get(MED_ResourceType.Medicine) == before + 1 && Cargo() == null, "recovery returns authentic unit once to inventory");
            courier = NewCart(); delivery.LoadOrReturn(); delivery.Send();
            typeof(MED_SupplyCart).GetProperty("MedicineTripBlocked").GetSetMethod(true).Invoke(courier, new object[] { true });
            identity = Cargo(); Check(delivery.LoadOrReturn() && CargoState() == "Recoverable" && ReferenceEquals(identity, Cargo()), "explicit stalled cancellation retains cargo, never credits arrival");
            courier = NewCart(); Check(delivery.Send(), "recovered exact dose can leave on next real empty cart without new debit");
            Call(courier, "SetState", MED_SupplyCart.CartState.AtDefense); courier.transform.position = terminal.position; Call(delivery, "Arrived", courier);
            camera.transform.position = terminal.position + Vector3.up;
            Set(aid, "delivery", delivery);
            var rig = Make<PWR_PlayerRig>("Fixture movement rig"); rig.origin = rig.transform; camera.transform.SetParent(rig.transform); Call(rig, "Awake");
            var desktop = Make<PWR_DesktopRig>("Disabled desktop provider"); desktop.enabled = false;
            before = patient.Get(MED_ResourceType.Medicine);
            Check((bool)typeof(DEF_ClinicFieldAid).GetProperty("Ready", Private).GetValue(aid), "activation fixture reaches qualified preconditions");
            Check(!(bool)Call(aid, "FindMovement") && patient.Get(MED_ResourceType.Medicine) == before && !desktop.enabled, "missing active locomotion rejects before debit and never enables disabled provider");
            Set(aid, "boosting", true); Set(aid, "desktopMove", desktop);
            Check(!aid.TryActivate() && patient.Get(MED_ResourceType.Medicine) == before && ReferenceEquals(typeof(DEF_ClinicFieldAid).GetField("desktopMove", Private).GetValue(aid), desktop),
                "repeated activation cannot spend or replace current provider");
            Set(aid, "usingXR", false); Set(aid, "priorSpeed", 2.5f); Set(aid, "lastWritten", 2.875f); desktop.moveSpeed = 2.875f;
            Call(aid, "StopBoost"); Check(desktop.moveSpeed == 2.5f && !aid.IsBoostActive, "restore exact prior movement speed");
            Set(aid, "boosting", true); Set(aid, "desktopMove", desktop); desktop.moveSpeed = 3.2f;
            Call(aid, "StopBoost"); Check(desktop.moveSpeed == 3.2f, "restoration does not overwrite a newer external movement setting");
            Set(aid, "boosting", true); Set(aid, "desktopMove", desktop); desktop.moveSpeed = 2.875f;
            DEF_ClinicFieldAid.CancelForReset(progression, false); Check(desktop.moveSpeed == 2.5f && !aid.IsBoostActive, "retry cancels and restores synchronously");
            patient.BeginEvidenceSession(); before = patient.Get(MED_ResourceType.Medicine);
            Check(!Transfer(qualifies, _ => commits++) && patient.Get(MED_ResourceType.Medicine) == before, "stale generation rejects without spending");
            Call(delivery, "Update"); Check(Cargo() == null && !delivery.Delivered, "generation change invalidates stale delivered stock");
            patient.Add(MED_ResourceType.Medicine, 1);
            Check(Transfer(null, _ => commits++, false), "legacy unqualified FIFO transfer remains available to legacy caller");
            DEF_ClinicFieldAid.CancelForReset(progression, true);
            Check(!(bool)typeof(DEF_ClinicFieldAid).GetField("unlocked", Private).GetValue(aid), "full reset removes Field Aid qualification");
            Call(aid, "OnDisable"); Call(aid, "StopBoost"); Check(desktop.moveSpeed == 2.5f, "repeated disable/reset restoration is idempotent");
        }
        catch (Exception e) { results.Add("FAIL fixture: " + e); }
        finally
        {
            EditorSceneManager.CloseScene(scene, true); if (recipe != null) UnityEngine.Object.DestroyImmediate(recipe);
            SceneManager.SetActiveScene(previous); foreach (var c in enabledCameras) if (c != null) c.enabled = true;
            DEF_GameState.I = oldDefense;
            hudField.SetValue(null, oldHud);
            typeof(MED_AreaGate).GetProperty("PlayerInside").GetSetMethod(true).Invoke(null, new object[] { oldArea });
            typeof(PWR_PlayerRig).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldRig });
        }
        System.IO.File.WriteAllLines("../clinic-field-aid-validation.txt", results); Debug.Log(string.Join("\n", results));
        if (results.Exists(x => x.StartsWith("FAIL"))) throw new Exception("Field Aid validation failed");
    }
}
