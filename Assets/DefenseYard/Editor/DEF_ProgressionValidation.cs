using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using S = DEF_ProgressionState.Stage;
using P = DEF_ProgressionState.Prerequisite;

// Test-only provider; never added to the generated demo or any production scene.
public sealed class DEF_TestReadiness : DEF_PrerequisiteSource
{
    public P kind;
    public override P Kind => kind;
    public void Emit(string id) => Publish(id);
}
public static class DEF_ProgressionValidation
{
    [MenuItem("Defense Yard/MP2B/Create Locked Progression Demo")]
    public static void CreateDemo()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Development/Scenes/Defense/DefenseYard.unity");
        var state = UnityEngine.Object.FindAnyObjectByType<DEF_GameState>();
        if (state == null) throw new Exception("Defense scene has no GameState");
        var controller = state.gameObject.AddComponent<DEF_Progression>();
        var board = new GameObject("Defense progression presentation - independent writer");
        var presenter = board.AddComponent<DEF_ProgressionPresenter>();
        var canvas = board.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        TMP_Text Text(string name, float offset)
        {
            var text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(board.transform, false); text.fontSize = 24;
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f,.5f);
            text.rectTransform.anchoredPosition = new Vector2(0, offset * 150);
            text.rectTransform.sizeDelta = new Vector2(700,100);
            return text;
        }
        presenter.introductionText = Text("Introduction", .8f); presenter.goalText = Text("Next goal", 0); presenter.failureText = Text("Action feedback", -.8f);
        var so = new SerializedObject(controller); so.FindProperty("presenter").objectReferenceValue = presenter; so.ApplyModifiedPropertiesWithoutUndo();
        // No fake Power/Clinic readiness source is installed. This demo intentionally stays locked.
        presenter.Present(controller);
        var hud = UnityEngine.Object.FindAnyObjectByType<DEF_HUD>();
        if (hud != null)
        {
            foreach (var button in hud.buyButtons) button.gameObject.SetActive(false);
            hud.ammoText.transform.parent.gameObject.SetActive(false);
            hud.ammoText.gameObject.SetActive(false); hud.scrapText.gameObject.SetActive(false);
            hud.shopButton.gameObject.SetActive(false); hud.startButton.gameObject.SetActive(false);
            hud.guideButton.gameObject.SetActive(false); hud.guidePanel.SetActive(false); hud.objectivePanel.SetActive(false);
        }
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Development/Scenes/Defense/DEF_Progression_Locked_Demo.unity");
    }
    [MenuItem("Defense Yard/MP2B/Run Progression Edit Mode Checks")]
    public static void Run()
    {
        if (Application.isPlaying) throw new Exception("Edit Mode only");
        var results = new List<string>(); var roots = new List<GameObject>(); var old = DEF_GameState.I;
        void Check(bool value, string name) => results.Add((value ? "PASS " : "FAIL ") + name);
        GameObject Make(string name) { var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave }; roots.Add(go); return go; }
        void Set(object obj, string name, object value) => obj.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(obj,value);
        try
        {
            var model = new DEF_ProgressionState();
            Check(!model.Refill(1,1,10,"early") && !model.Place(1,1,"early") && !model.BeginWave(1,1), "out-of-order evidence cannot advance locked model");
            Check(!model.AcceptPrerequisite(P.Power6,0,"old"), "old session evidence rejected");
            model.AcceptPrerequisite(P.Power6,1,"power"); model.AcceptPrerequisite(P.Clinic10,1,"clinic");
            Check(model.Current == S.TeamPrerequisites, "Power AND Clinic alone keep Defense locked");
            Check(!model.AcceptPrerequisite(P.Power6,1,"power"), "replayed prerequisite receipt rejected");
            model.AcceptPrerequisite(P.Workshop14,1,"teaching"); Check(model.Current == S.PlaceSentry, "all three prerequisite sources unlock basic placement");
            Check(model.Place(1,1,"sentry") && model.Current == S.RepairIntroduction && !model.Repair(1,1,"sentry",20), "placement opens introduction before repair evidence is allowed");
            Check(!model.CompleteIntroduction(1,1,0), "stale introduction acknowledgement rejected");
            model.CompleteIntroduction(1,1,model.Introduction);
            Check(!model.Repair(1,1,"other",20) && !model.Repair(1,1,"sentry",0) && !model.Repair(1,1,"sentry",float.NaN), "wrong target zero and invalid repair deltas rejected");
            Check(model.Repair(1,1,"sentry",20) && model.Current == S.AmmoIntroduction && !model.Refill(1,1,10,"early"), "positive repair opens ammo introduction before refill");
            model.CompleteIntroduction(1,1,model.Introduction);
            Check(model.Refill(1,1,10,"physical-unit") && model.Current == S.WaveIntroduction && !model.BeginWave(1,1), "real refill threshold precedes wave introduction");
            model.CompleteIntroduction(1,1,model.Introduction); Check(model.BeginWave(1,1) && !model.BeginWave(1,1), "first wave starts once after introduction");
            Check(!model.ClearWave(1,1,1,0) && !model.ClearWave(1,1,2,100), "dead base and wrong wave cannot complete milestone");
            Check(model.ClearWave(1,1,1,100), "wave1 base alive completes bounded milestone");
            model.RetryDefense(); Check(model.TeamReady && model.Current == S.PlaceSentry && !model.Place(1,1,"old"), "Defense retry retains prerequisites and rejects old attempt evidence");
            model.NewSession(); Check(!model.TeamReady && model.Current == S.TeamPrerequisites && !model.AcceptPrerequisite(P.Power6,1,"old"), "new run clears readiness and rejects previous session");
            var state = Make("Gated state").AddComponent<DEF_GameState>(); DEF_GameState.I = state;
            var gate = state.gameObject.AddComponent<DEF_Progression>();
            var presenter = Make("Presenter fixture").AddComponent<DEF_ProgressionPresenter>();
            presenter.introductionText = Make("Intro fixture").AddComponent<TextMeshPro>(); presenter.goalText = Make("Goal fixture").AddComponent<TextMeshPro>();
            Set(gate,"presenter",presenter);
            var power = Make("Power fixture").AddComponent<DEF_TestReadiness>(); power.kind = P.Power6;
            var clinic = Make("Clinic fixture").AddComponent<DEF_TestReadiness>(); clinic.kind = P.Clinic10;
            var workshop = Make("Workshop fixture").AddComponent<DEF_TestReadiness>(); workshop.kind = P.Workshop14;
            Set(gate,"power",power); Set(gate,"clinic",clinic); Set(gate,"workshop",workshop);
            gate.SendMessage("OnDisable"); gate.SendMessage("OnEnable");
            Check(!DEF_Turret.TryPlace(1,1,false) && !DEF_Turret.TryPlace(1,1,true) && !DEF_Barricade.TryPlace(1,1) && !DEF_ShockTrap.TryPlace(1,1), "all public placement entry points enforce locked gate before spending");
            var turret = Make("Repair fixture").AddComponent<DEF_Turret>(); turret.health = 50;
            var barrier = Make("Barrier fixture").AddComponent<DEF_Barricade>(); barrier.health = 50;
            var shock = Make("Shock fixture").AddComponent<DEF_ShockTrap>(); shock.health = 50;
            var crate = Make("Crate fixture").AddComponent<DEF_AmmoCrate>();
            Check(!turret.TryRepair() && !barrier.TryRepair() && !shock.TryRepair() && crate.Harvest(10)==0 && turret.health==50, "repair and legacy crate entry points cannot bypass locked gate");
            var waves = Make("Wave fixture").AddComponent<DEF_WaveManager>(); waves.StartWave();
            int zombies = DEF_Zombie.all.Count; DEF_Zombie.Spawn(Vector3.zero,1,null);
            Check(waves.phase == DEF_WaveManager.Phase.Idle && DEF_Zombie.all.Count == zombies && !gate.IsAllowed(DEF_Action.Combat), "public wave and ambient spawn entry points remain blocked");
            var intake = Make("Ammo fixture").AddComponent<DEF_WorkshopAmmoReceiver>();
            Check(!intake.TryDeliver(null) && !gate.IsAllowed(DEF_Action.RemoteAmmo), "physical Defense and remote showcase paths respect progression while teaching receiver remains separate");
            var foreign = Make("Unconfigured source").AddComponent<DEF_TestReadiness>(); foreign.kind=P.Power6; foreign.BeginSession(1); foreign.Emit("foreign");
            Check(gate.State.Current==S.TeamPrerequisites, "unconfigured source cannot publish readiness");
            power.Emit("p"); clinic.Emit("c"); Check(!gate.IsAllowed(DEF_Action.BasicSentry), "configured provider AND gate remains locked until teaching readiness");
            workshop.Emit("w"); Check(gate.IsAllowed(DEF_Action.BasicSentry) && !gate.IsAllowed(DEF_Action.Repair) && !gate.IsAllowed(DEF_Action.Missile), "only basic content unlocks with all configured prerequisite evidence");
            presenter.enabled=false; Check(!gate.IsAllowed(DEF_Action.BasicSentry), "missing inactive presenter fails closed"); presenter.enabled=true;
            gate.BeginNewSession(); Check(!gate.IsAllowed(DEF_Action.BasicSentry), "component new session relocks actual entry gate");
            UnityEngine.Object.DestroyImmediate(gate); Check(DEF_Progression.Allows(DEF_Action.Missile), "no progression component preserves legacy permission behavior");
        }
        catch(Exception error) { results.Add("FAIL exception " + error); }
        finally
        {
            foreach(var go in roots) if(go!=null) UnityEngine.Object.DestroyImmediate(go);
            DEF_GameState.I=old;
            System.IO.File.WriteAllLines("../progression-editmode-results.txt",results); Debug.Log(string.Join("\n",results));
        }
    }
}

