using System;
using System.Collections.Generic;
using System.Reflection;
using Deadzone.Power;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

// Deterministic Edit Mode contract checks; never gameplay evidence.
public static class DEF_PowerValidation
{
    public static void RunBatch()
    {
        if (!Application.isBatchMode || Application.isPlaying) throw new Exception("Batch Edit Mode only");
        DEF_AuditValidation.RunBatch();
        RunPower();
    }
    [UnityEditor.MenuItem("Defense Yard/MP2B/Run All Checks Including Power")]
    public static void RunInEditor()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        DEF_AuditValidation.RunAll();RunPower();
    }
    static void RunPower()
    {
        var previous=SceneManager.GetActiveScene();
        var fixture=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(fixture);
        var results = new List<string>();
        var objects = new List<GameObject>();
        GameObject Make(string name) { var go = new GameObject(name); objects.Add(go); return go; }
        void Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(obj, args);
        void Field(object obj, string name, object value) => obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
        void Property(object obj, string name, object value) => Field(obj, "<" + name + ">k__BackingField", value);
        void Check(bool ok, string text) { results.Add((ok ? "PASS " : "FAIL ") + text); }
        try
        {
            var charger = Make("Fixture charger").AddComponent<PWR_CellCharger>();
            var stats = charger.GetComponent<PWR_StatBlock>(); stats.AddStat("Progress", 0, 0, 100); Field(charger,"stats",stats);
            var lamp = Make("Fixture lamp").AddComponent<PWR_Floodlight>();
            var director = Make("Fixture director").AddComponent<PWR_LevelDirector>(); director.charger=charger; director.floodlight=lamp;
            var adapter = Make("Fixture adapter").AddComponent<DEF_PowerReadiness>(); adapter.director=director; adapter.charger=charger; adapter.floodlight=lamp;
            var cell=Make("Exact charged output").AddComponent<PWR_Grabbable>();
            var other=Make("Unrelated battery").AddComponent<PWR_Grabbable>();
            var bank=Make("Bank").AddComponent<PWR_PowerNode>(); bank.role=PWR_PowerRole.Storage;
            int receipts=0; adapter.Completed += (_,__,___)=>receipts++;
            adapter.BeginSession(1);
            Call(charger,"HandleCellPicked",(object)null); Check(!charger.IsCellCollected,"null pickup cannot complete charger");
            Property(charger,"IsComplete",true); Property(charger,"ChargedCell",cell);
            Call(charger,"HandleCellPicked",other); Check(!charger.IsCellCollected,"unrelated battery cannot complete charger");
            Call(charger,"HandleCellPicked",cell); Check(charger.IsCellCollected,"exact charged output pickup completes charger");
            Check(receipts==0,"pickup without fresh charge chain does not unlock Defense");
            adapter.BeginSession(2); Check(adapter.PendingReason.Contains("Reload"),"new session rejects completed one-shot station");
            Property(charger,"IsComplete",false); Property(charger,"IsCellCollected",false); Property(charger,"ChargedCell",null);
            adapter.BeginSession(3); Property(charger,"HasBattery",true); Call(adapter,"Inserted",charger);
            Property(charger,"IsComplete",true); Property(charger,"ChargedCell",cell); stats.Set("Progress",100);
            Call(adapter,"Charged",charger); Check(!(bool)typeof(DEF_PowerReadiness).GetField("charged",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(adapter),"full stat without actual power transfer is rejected");
            Call(adapter,"Transfer",bank,charger.GetComponent<PWR_PowerNode>(),1f); Call(adapter,"Charged",charger);
            Call(adapter,"Broken",lamp);
            lamp.bulb=Make("Replacement").AddComponent<PWR_StatBlock>(); Field(lamp,"bulbUnbreakable",true); Call(adapter,"Replaced",lamp);
            Check(receipts==0,"charge and replacement still require pickup");
            Property(charger,"IsCellCollected",true); Call(adapter,"Collected",charger); Check(receipts==1,"fresh exact source chain publishes once");
            Call(adapter,"Collected",charger); Call(adapter,"Replaced",lamp); Check(receipts==1,"duplicate source notifications do not republish");
            adapter.BeginSession(4); Call(adapter,"Collected",charger); Check(receipts==1,"old pickup cannot credit new session");
            director.floodlight=null; Call(director,"HandleCellCollected",charger); Check(true,"missing light fails closed without null dereference");
            director.floodlight=lamp;
            Property(charger,"IsComplete",false);Property(charger,"IsCellCollected",false);Property(charger,"HasBattery",false);Property(charger,"ChargedCell",null);
            Field(lamp,"bulbUnbreakable",false);Field(lamp,"hadBulb",true);
            lamp.bulb.AddStat("BulbLife",10,0,10);
            var lightStats=lamp.GetComponent<PWR_StatBlock>();lightStats.AddStat("Brightness",0,0,100);Field(lamp,"stats",lightStats);Field(lamp,"node",lamp.GetComponent<PWR_PowerNode>());
            var replacementPrefab=Make("Replacement prefab").AddComponent<PWR_StatBlock>();replacementPrefab.AddStat("BulbLife",10,0,10);lamp.bulbPrefab=replacementPrefab.gameObject;
            charger.chargedCellPrefab=cell.gameObject;
            adapter.BeginSession(5);Call(adapter,"OnEnable");adapter.enabled=false;
            charger.OnItemAccepted(null);Call(adapter,"Transfer",bank,charger.GetComponent<PWR_PowerNode>(),1f);stats.Set("Progress",100);Call(charger,"Complete");
            var expired=lamp.bulb;lamp.BreakBulb();Check(expired.Get("BulbLife")==0,"real BreakBulb depletes the original bulb");
            UnityEngine.Object.DestroyImmediate(expired.gameObject); // represents death completing before the lamp Update
            lamp.OnItemAccepted(null); // real source callbacks must flush Broken BEFORE Installed
            Check((bool)typeof(DEF_PowerReadiness).GetField("broken",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(adapter) &&
                (bool)typeof(DEF_PowerReadiness).GetField("replaced",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(adapter),
                "replacement before Update preserves actual break then installation evidence");
            Call(lamp,"Update");Call(charger.ChargedCell,"HandlePicked");
            Check(receipts==1,"temporarily disabled adapter observes source events without publishing");
            adapter.enabled=true;Call(adapter,"OnEnable");Check(receipts==2,"reenabled adapter publishes fresh observed source chain once");
            Call(lamp,"Update");Call(adapter,"OnEnable");Check(receipts==2,"later Update and repeated enable do not duplicate completion");
            var consumer=Make("Power consumer").AddComponent<DEF_Progression>();
            for(int i=1;i<5;i++)consumer.State.NewSession();Field(consumer,"power",adapter);
            Call(consumer,"OnEnable");Call(consumer,"OnDisable");Call(consumer,"OnEnable");
            var ready=(HashSet<DEF_ProgressionState.Prerequisite>)typeof(DEF_ProgressionState).GetField("ready",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(consumer.State);
            Check(ready.Contains(DEF_ProgressionState.Prerequisite.Power6)&&consumer.State.Current==DEF_ProgressionState.Stage.TeamPrerequisites&&receipts==2,
                "controller reconnect replays real completed Power without resetting one-shot sources or bypassing other prerequisites");
            adapter.BeginSession(6);adapter.enabled=false;adapter.enabled=true;Call(adapter,"OnEnable");
            Check(receipts==2,"disable and reenable cannot revive stale session credit");
        }
        catch(Exception e) { results.Add("FAIL " + e); }
        finally { foreach(var go in objects) if(go!=null) UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.CloseScene(fixture,true);SceneManager.SetActiveScene(previous); }
        System.IO.File.WriteAllLines("../power-editmode-results.txt",results);
        Debug.Log(string.Join("\n",results));
        if(results.Exists(x=>x.StartsWith("FAIL"))) throw new Exception("Power validation failed");
    }
}

