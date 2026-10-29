using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

// Independent Edit Mode fixture. Does not enter Play Mode.
public static class DEF_AuditValidation
{
    
    public static void RunBatch()
    {
        if (!Application.isBatchMode || Application.isPlaying) throw new Exception("Batch Edit Mode only");
        EditorSceneManager.OpenScene("Assets/Development/Scenes/Defense/DEF_WorkshopAmmo_Demo.unity");
        RunAll();
        var names=new[]{"repair","progression","ammo-dualmode","audit","runtime-edge","felix-compat","affordance","walkthrough"};
        foreach(var name in names) {
            var lines=System.IO.File.ReadAllLines("../"+name+"-editmode-results.txt");
            if(System.Array.Exists(lines,l=>l.StartsWith("FAIL ")))throw new Exception("Failed suite: "+name);
        }
    }
    [MenuItem("Defense Yard/MP2B/Run All Current Edit Mode Checks")]
    public static void RunAll()
    {
        DEF_RepairValidation.Run();DEF_ProgressionValidation.Run();DEF_AmmoDualModeValidation.Run();Run();DEF_RuntimeEdgeValidation.Run();DEF_FelixCompatibilityValidation.Run();DEF_AffordanceValidation.Run();
        DEF_WalkthroughValidation.Run();
    }
    [MenuItem("Defense Yard/MP2B/Open Existing Ammo Demo")]
    public static void OpenDemo()
    {
        if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene("Assets/Development/Scenes/Defense/DEF_WorkshopAmmo_Demo.unity");
    }
    [MenuItem("Defense Yard/MP2B/Run Audit Regression Edit Mode Checks")]
    public static void Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var results=new List<string>();
        var oldState=DEF_GameState.I;var oldWaves=DEF_WaveManager.I;
        var previousActors=DEF_Zombie.all;DEF_Zombie.all=new List<DEF_Zombie>();
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        GameObject Make(string name)=>new GameObject(name);
        void Check(bool ok,string name)=>results.Add((ok?"PASS ":"FAIL ")+name);
        void Set(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
        object Get(object obj,string name)=>obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(obj);
        object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,args);
        void Ready(DEF_ProgressionState s)
        {
            foreach(DEF_ProgressionState.Prerequisite p in Enum.GetValues(typeof(DEF_ProgressionState.Prerequisite)))s.AcceptPrerequisite(p,s.Session,p.ToString());
        }
        void Prepare(DEF_ProgressionState s,string target)
        {
            s.Place(s.Session,s.Attempt,target);s.CompleteIntroduction(s.Session,s.Attempt,s.Introduction);
            s.Repair(s.Session,s.Attempt,target,10);s.CompleteIntroduction(s.Session,s.Attempt,s.Introduction);
            s.Refill(s.Session,s.Attempt,10,"receipt"+s.Attempt);s.CompleteIntroduction(s.Session,s.Attempt,s.Introduction);
        }
        try
        {
            var state=Make("Audit state").AddComponent<DEF_GameState>();DEF_GameState.I=state;
            var gate=state.gameObject.AddComponent<DEF_Progression>();
            var presenter=Make("Audit presenter").AddComponent<DEF_ProgressionPresenter>();
            presenter.goalText=Make("Goal").AddComponent<TextMeshPro>();presenter.introductionText=Make("Intro").AddComponent<TextMeshPro>();Set(gate,"presenter",presenter);
            var waves=Make("Audit waves").AddComponent<DEF_WaveManager>();DEF_WaveManager.I=waves;
            var trap=Make("Pre-existing trap").AddComponent<DEF_ShockTrap>();
            Check(!trap.CanDischarge,"pre-existing Shock discharge denied during preparation");
            Ready(gate.State);
            state.scrap=180;state.ammo=55;
            var ownedTurret=Make("Attempt sentry").AddComponent<DEF_Turret>();
            Call(gate,"Placed",ownedTurret);
            gate.State.CompleteIntroduction(gate.State.Session,gate.State.Attempt,gate.State.Introduction);
            gate.State.Repair(gate.State.Session,gate.State.Attempt,ownedTurret.GetEntityId().ToString(),10);
            gate.State.CompleteIntroduction(gate.State.Session,gate.State.Attempt,gate.State.Introduction);
            gate.State.Refill(gate.State.Session,gate.State.Attempt,10,"first refill");
            gate.State.CompleteIntroduction(gate.State.Session,gate.State.Attempt,gate.State.Introduction);
            waves.StartWave();
            Check(waves.phase==DEF_WaveManager.Phase.Idle && gate.State.Current==DEF_ProgressionState.Stage.WaveReady && state.waveNumber==0,"missing portal configuration rejects before consuming wave eligibility");
            var points=(List<Vector3>)Get(waves,"spawnPoints");var portals=(List<Vector2Int>)Get(waves,"portalCells");
            points.Add(Vector3.zero);Check(!waves.ValidateSpawnConfiguration(1,4,out _),"mismatched portals rejected");
            portals.Add(Vector2Int.zero);
            Check(waves.ValidateSpawnConfiguration(1,4,out _),"installed walker prefab and valid portal configuration accepted");
            points[0]=new Vector3(float.NaN,0,0);Check(!waves.ValidateSpawnConfiguration(1,4,out _),"invalid portal coordinate rejected");points[0]=Vector3.zero;
            Check(!waves.ValidateSpawnConfiguration(1,0,out _),"empty quota rejected");
            waves.StartWave();
            Check(waves.phase==DEF_WaveManager.Phase.Spawning && gate.State.Current==DEF_ProgressionState.Stage.WaveActive,"valid prepared wave starts through real public method");
            Check(!trap.CanDischarge,"preplaced Shock stays locked during first wave before advanced reveal");
            bool spawned=(bool)Call(waves,"SpawnOne");
            Check(spawned && (int)Get(waves,"spawnedThisWave")==1,"successful real prefab spawn increments count");
            points.Clear();Check(!(bool)Call(waves,"SpawnOne") && (int)Get(waves,"spawnedThisWave")==1,"partial spawn failure does not increment successful count");
            Call(waves,"FailAttempt","fixture spawn failure");
            Check(waves.AttemptFailed && !trap.CanDischarge && gate.State.Current!=DEF_ProgressionState.Stage.FirstWaveComplete,"partial failed wave cannot grant mastery or discharge Shock");
            var unrelated=Make("Unrelated actor").AddComponent<DEF_Zombie>(); if(!DEF_Zombie.all.Contains(unrelated))DEF_Zombie.all.Add(unrelated);
            var shot=Make("Owned projectile").AddComponent<DEF_Projectile>();waves.TrackProjectile(shot);
            state.playerHealth=0;
            int attempt=gate.State.Attempt;
            waves.StartWave();
            Check(!waves.AttemptFailed && waves.phase==DEF_WaveManager.Phase.Idle && state.playerHealth==state.maxHealth && state.waveNumber==0 && gate.State.Attempt==attempt+1 && gate.State.TeamReady,"death/start retries real world and retains team prerequisites");
            Check(ownedTurret==null && state.scrap==220 && state.ammo==55,"retry removes attempt defense and restores pre-placement Defense budget");
            Check(shot==null && unrelated!=null && DEF_Zombie.all.Contains(unrelated),"retry retires owned projectile and preserves unrelated actor");
            Check(!trap.CanDischarge,"pre-existing trap remains locked after retry");
            points.Add(Vector3.zero);Prepare(gate.State,"new target");waves.StartWave();
            Check(state.waveNumber==1 && gate.State.Current==DEF_ProgressionState.Stage.WaveActive,"retry can reach real first-wave eligibility again");

            // Adapter test injects the same private callback used by its configured workbench event.
            // It validates session/delivery accounting, not the workbench recipe or XR interaction.
            var crafting=Make("Old workshop flags").AddComponent<SCV_ProgressionManager>();
            Set(crafting,"currentStage",SCV_ProgressionManager.WorkshopStage.WorkshopComplete);
            var bench=Make("Configured workbench").AddComponent<SCV_Workbench>();
            var readiness=Make("Workshop adapter").AddComponent<DEF_WorkshopReadiness>();Set(readiness,"crafting",crafting);Set(readiness,"workbench",bench);
            int receipts=0;readiness.Completed+=(source,session,id)=>receipts++;
            readiness.BeginSession(2);Call(readiness,"Accepted","old:1");Call(readiness,"Check");
            Check(receipts==0,"old WorkshopComplete plus a new unproven delivery fails closed");
            var ledger=Make("Craft ledger").AddComponent<SCV_ResourceManager>();ledger.AddAmmo(1);ledger.AddRepairKit(1);
            var ammo=Make("Fresh ammo").AddComponent<SCV_CraftedItem>();ammo.BindLedger(ledger);
            var kit=Make("Fresh kit").AddComponent<SCV_CraftedItem>();Set(kit,"itemType",SCV_CraftedItem.ItemType.RepairKit);kit.BindLedger(ledger);
            Call(readiness,"Crafted",ammo);Call(readiness,"Crafted",kit);Call(readiness,"Accepted",ledger.GetEntityId()+":"+ammo.UnitId);
            Check(receipts==1,"fresh two-type craft evidence plus matching physical receipt publishes");
            readiness.BeginSession(3);Call(readiness,"Accepted",ledger.GetEntityId()+":"+ammo.UnitId);Call(readiness,"Check");
            Check(receipts==1,"new session rejects previous session crafted identity and flags");
            var advanced=new DEF_ProgressionState();Ready(advanced);Prepare(advanced,"sentry");advanced.BeginWave(1,1);advanced.ClearWave(1,1,1,150);
            Check(advanced.IntroductionActive && !advanced.AdvancedUnlocked,"wave clear introduces advanced pair before unlock");
            advanced.CompleteIntroduction(1,1,advanced.Introduction);
            Check(advanced.AdvancedUnlocked && !advanced.MissileUnlocked,"advanced pair revealed together while missile remains locked");
            Check(!advanced.RecordControl(0,1,"old",true),"old-session combat evidence rejected");
            advanced.RecordControl(1,1,"bar",false);
            Check(advanced.Current==DEF_ProgressionState.Stage.AdvancedPractice,"barricade alone cannot unlock missile");
            Check(!advanced.RecordControl(1,1,"bar",true),"replayed control receipt cannot supply second threshold");
            advanced.RecordControl(1,1,"shock",true);
            Check(advanced.Current==DEF_ProgressionState.Stage.MissileIntroduction && !advanced.MissileUnlocked,"both control and actual stun precede missile introduction");
            advanced.CompleteIntroduction(1,1,advanced.Introduction);
            Check(advanced.MissileUnlocked && !advanced.RecordSplash(1,1,"single",1),"single victim cannot satisfy missile multi-hit");
            Check(advanced.RecordSplash(1,1,"double",2) && advanced.Current==DEF_ProgressionState.Stage.JuggernautReady,"two positive distinct victims open Juggernaut goal");
            advanced.BeginWave(1,1);advanced.RecordCombat(1,1,"Sentry","one",true);advanced.ClearWave(1,1,3,150);
            Check(advanced.Current==DEF_ProgressionState.Stage.JuggernautReady,"wave survival without mixed sustainment cannot complete challenge");
            advanced.BeginWave(1,1);
            advanced.RecordCombat(1,1,"Sentry","shot2",true);advanced.RecordCombat(1,1,"Shock","pulse2",false);
            advanced.RecordSustainment(1,1,true,"kit");advanced.RecordSustainment(1,1,false,"box");
            Check(!advanced.ClearWave(1,1,3,0),"dead base cannot complete mixed challenge");
            Check(advanced.ClearWave(1,1,3,150) && advanced.Current==DEF_ProgressionState.Stage.Complete,"live-base mixed defense plus physical sustainment completes challenge");
            advanced.RetryDefense();Check(!advanced.MissileUnlocked && advanced.TeamReady,"retry clears advanced evidence while retaining team prerequisites");
            gate.BeginNewSession();
            Check(!gate.State.TeamReady && !gate.IsAllowed(DEF_Action.BasicSentry),"new full session clears readiness distinctly from retry");
        }
        catch(Exception error){results.Add("FAIL exception "+error);}
        finally
        {
            EditorSceneManager.CloseScene(scene,true);DEF_Zombie.all=previousActors;SceneManager.SetActiveScene(previous);
            DEF_GameState.I=oldState;DEF_WaveManager.I=oldWaves;
            System.IO.File.WriteAllLines("../audit-editmode-results.txt",results);Debug.Log(string.Join("\n",results));
        }
    }
}



