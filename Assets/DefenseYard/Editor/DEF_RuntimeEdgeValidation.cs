using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
public static class DEF_RuntimeEdgeValidation
{
    [MenuItem("Defense Yard/MP2B/Run Runtime Edge Edit Mode Checks")]
    public static void Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var results=new List<string>();var oldState=DEF_GameState.I;var oldWave=DEF_WaveManager.I;
        var previousActors=DEF_Zombie.all;DEF_Zombie.all=new List<DEF_Zombie>();
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        GameObject Make(string name)=>new GameObject(name);
        void Check(bool ok,string name)=>results.Add((ok?"PASS ":"FAIL ")+name);
        void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
        object Get(object obj,string field)=>obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);
        object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);
        void Ready(DEF_ProgressionState s){foreach(DEF_ProgressionState.Prerequisite p in Enum.GetValues(typeof(DEF_ProgressionState.Prerequisite)))s.AcceptPrerequisite(p,s.Session,p.ToString());}
        void Prepare(DEF_ProgressionState s)
        {
            Ready(s);s.Place(s.Session,s.Attempt,"fixture");s.CompleteIntroduction(s.Session,s.Attempt,s.Introduction);
            s.Repair(s.Session,s.Attempt,"fixture",10);s.CompleteIntroduction(s.Session,s.Attempt,s.Introduction);
            s.Refill(s.Session,s.Attempt,10,"prep"+s.Session);s.CompleteIntroduction(s.Session,s.Attempt,s.Introduction);
        }
        try
        {
            var state=Make("Edge state").AddComponent<DEF_GameState>();DEF_GameState.I=state;
            var gate=state.gameObject.AddComponent<DEF_Progression>();
            var presenter=Make("Edge presenter").AddComponent<DEF_ProgressionPresenter>();
            presenter.goalText=Make("Goal").AddComponent<TextMeshPro>();presenter.introductionText=Make("Intro").AddComponent<TextMeshPro>();Set(gate,"presenter",presenter);
            var waves=Make("Edge waves").AddComponent<DEF_WaveManager>();DEF_WaveManager.I=waves;
            ((List<Vector3>)Get(waves,"spawnPoints")).Add(Vector3.zero);((List<Vector2Int>)Get(waves,"portalCells")).Add(Vector2Int.zero);
            Prepare(gate.State);waves.StartWave();
            var oldShot=Make("Airborne first wave missile").AddComponent<DEF_Projectile>();oldShot.explosive=true;waves.TrackProjectile(oldShot);
            var unrelated=Make("Unrelated projectile").AddComponent<DEF_Projectile>();
            Set(waves,"spawnedThisWave",1);Set(waves,"expectedSpawns",1);Set(waves,"toSpawn",0);waves.phase=DEF_WaveManager.Phase.Active;
            results.Add("INFO before clear: actors="+DEF_Zombie.all.Count+" stage="+gate.State.Current+" combat="+gate.IsAllowed(DEF_Action.Combat)+" training="+DEF_HUD.TrainingActive+" blocking="+DEF_HUD.BlockingWorldInput);
            Call(waves,"Update");
            results.Add("INFO after clear: phase="+waves.phase+" stage="+gate.State.Current+" actors="+DEF_Zombie.all.Count);
            Check(oldShot==null && !waves.OwnsProjectile(oldShot) && unrelated!=null && waves.phase==DEF_WaveManager.Phase.Cleared,"actual wave-clear hook retires owned airborne projectile only");
            gate.State.CompleteIntroduction(gate.State.Session,gate.State.Attempt,gate.State.Introduction);
            gate.State.RecordControl(gate.State.Session,gate.State.Attempt,"bar",false);gate.State.RecordControl(gate.State.Session,gate.State.Attempt,"shock",true);
            gate.State.CompleteIntroduction(gate.State.Session,gate.State.Attempt,gate.State.Introduction);gate.State.RecordSplash(gate.State.Session,gate.State.Attempt,"practice splash",2);
            var stale=Make("Airborne practice projectile").AddComponent<DEF_Projectile>();waves.TrackProjectile(stale);
            waves.StartWave();
            Check(stale==null && !waves.OwnsProjectile(stale) && state.waveNumber==3,"actual challenge start retires outstanding practice projectile");
            Call(waves,"SpawnOne");var actors=(List<DEF_Zombie>)Get(waves,"attemptActors");var brute=actors[actors.Count-1];
            Call(gate,"ProjectileDamage",stale,brute,10f);Call(gate,"Splash",stale,2);
            gate.State.RecordCombat(gate.State.Session,gate.State.Attempt,"Shock","new shock",true);
            gate.State.RecordSustainment(gate.State.Session,gate.State.Attempt,true,"new kit");gate.State.RecordSustainment(gate.State.Session,gate.State.Attempt,false,"new box");
            gate.State.ClearWave(gate.State.Session,gate.State.Attempt,3,150);
            Check(gate.State.Current==DEF_ProgressionState.Stage.JuggernautReady,"retired projectile damage hook cannot supply second defense type to later challenge");
            gate.State.BeginWave(gate.State.Session,gate.State.Attempt);
            var freshShot=Make("Current challenge shot").AddComponent<DEF_Projectile>();waves.TrackProjectile(freshShot);
            Call(gate,"ProjectileDamage",freshShot,brute,5f);
            gate.State.RecordCombat(gate.State.Session,gate.State.Attempt,"Shock","fresh shock",true);
            gate.State.RecordSustainment(gate.State.Session,gate.State.Attempt,true,"fresh kit");gate.State.RecordSustainment(gate.State.Session,gate.State.Attempt,false,"fresh box");
            Check(gate.State.ClearWave(gate.State.Session,gate.State.Attempt,3,150) && gate.State.Current==DEF_ProgressionState.Stage.Complete,"current-wave projectile hook still contributes valid combat evidence");
            gate.BeginNewSession();
            var ledger=Make("Edge ledger").AddComponent<SCV_ResourceManager>();Set(gate,"repairLedger",ledger);
            SCV_CraftedItem Item(bool repair)
            {
                var item=Make(repair?"Kit":"Box").AddComponent<SCV_CraftedItem>();
                if(repair)Set(item,"itemType",SCV_CraftedItem.ItemType.RepairKit);
                if(!item.BindLedger(ledger))throw new Exception("Fixture binding failed");return item;
            }
            Ready(gate.State);var turret=Make("Repair target").AddComponent<DEF_Turret>();Call(gate,"Placed",turret);
            gate.State.CompleteIntroduction(gate.State.Session,gate.State.Attempt,gate.State.Introduction);
            var repair=turret.GetComponentInChildren<DEF_WorkshopRepairReceiver>();ledger.ResetRepairInventory(1);var kit=Item(true);
            int beforeSession=gate.State.Session;bool deferred=false,receiptOld=false;int receiptCount=0;
            Action reset=()=>{gate.BeginNewSession();deferred=gate.State.Session==beforeSession && turret!=null;};
            repair.PhysicalRepairAccepted+=(target,before,after,id)=>{receiptCount++;receiptOld=gate.State.Session==beforeSession && target==turret && before<after;};
            ledger.OnResourcesChanged+=reset;
            bool repaired=repair.TryDeliver(kit);ledger.OnResourcesChanged-=reset;
            Check(repaired && deferred && receiptOld && receiptCount==1 && kit==null && ledger.repairKits==0,"inventory-listener full reset waits until exact repair commit and old-session receipt finish");
            Check(gate.State.Session==beforeSession+1 && gate.State.Current==DEF_ProgressionState.Stage.TeamPrerequisites && turret==null,"queued full reset then clears repair evidence and target once");
            Ready(gate.State);gate.State.Place(gate.State.Session,gate.State.Attempt,"ammo fixture");gate.State.CompleteIntroduction(gate.State.Session,gate.State.Attempt,gate.State.Introduction);
            gate.State.Repair(gate.State.Session,gate.State.Attempt,"ammo fixture",10);gate.State.CompleteIntroduction(gate.State.Session,gate.State.Attempt,gate.State.Introduction);
            var intake=Make("Ammo intake").AddComponent<DEF_WorkshopAmmoReceiver>();intake.ledger=ledger;intake.destination=state;state.ammo=20;
            ledger.ResetAmmoInventory(1);var box=Item(false);beforeSession=gate.State.Session;
            ledger.OnResourcesChanged+=reset;bool delivered=intake.TryDeliver(box);ledger.OnResourcesChanged-=reset;
            Check(delivered && box==null && ledger.ammo==0 && state.ammo==30 && gate.State.Session==beforeSession+1 && gate.State.Current==DEF_ProgressionState.Stage.TeamPrerequisites,"inventory-listener session reset preserves one ammo credit without leaking old receipt into new session");
            Prepare(gate.State);gate.State.BeginWave(gate.State.Session,gate.State.Attempt);waves.phase=DEF_WaveManager.Phase.Active;state.ammo=20;
            ledger.ResetAmmoInventory(1);box=Item(false);int attempt=gate.State.Attempt;bool retryDeferred=false;
            Action retry=()=>{state.playerHealth=0;gate.BeginDefenseRetry();retryDeferred=gate.State.Attempt==attempt;};
            ledger.OnResourcesChanged+=retry;delivered=intake.TryDeliver(box);ledger.OnResourcesChanged-=retry;
            Check(delivered && retryDeferred && gate.State.Attempt==attempt+1 && gate.State.TeamReady && gate.State.Current==DEF_ProgressionState.Stage.PlaceSentry && ledger.ammo==0 && box==null,"inventory-listener retry defers world and attempt reset through ammo receipt");
            gate.BeginNewSession();Prepare(gate.State);state.ammo=state.maxAmmo;ledger.ResetAmmoInventory(1);box=Item(false);
            int unchanged=gate.State.Session;bool notified=false;Action rejected=()=>notified=true;ledger.OnResourcesChanged+=rejected;
            Check(!intake.TryDeliver(box) && !notified && ledger.ammo==1 && box!=null && gate.State.Session==unchanged,"rejected full delivery performs neither inventory notification nor reset");
            ledger.OnResourcesChanged-=rejected;
            state.ammo=20;Check(intake.TryDeliver(box) && ledger.ammo==0 && box==null && state.ammo==30,"fresh delivery succeeds after rejected full delivery and completed reset scopes");
        }
        catch(Exception error){results.Add("FAIL exception "+error);}
        finally
        {
            EditorSceneManager.CloseScene(scene,true);DEF_Zombie.all=previousActors;SceneManager.SetActiveScene(previous);DEF_GameState.I=oldState;DEF_WaveManager.I=oldWave;
            System.IO.File.WriteAllLines("../runtime-edge-editmode-results.txt",results);Debug.Log(string.Join("\n",results));
        }
    }
}
