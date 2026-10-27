using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class DEF_ClinicTrainingValidation
{
    [MenuItem("Defense Yard/MP2B/Validate Clinic Ordered Gates")]
    public static void Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var lines=new System.Collections.Generic.List<string>();void Check(bool b,string n){lines.Add((b?"PASS ":"FAIL ")+n);}
        try
        {
            var t=new GameObject("Clinic fixture").AddComponent<DEF_ClinicTraining>();SceneManager.MoveGameObjectToScene(t.gameObject,scene);
            Check(t.Step==7&&!t.ReadyForTreatment,"fresh run begins at harvest, treatment locked");
            Check(t.ProtectLearningCart,"first delivery cannot time out before learning unload");
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            // Non-ExecuteAlways behaviours do not reliably run runtime lifecycle in Edit Mode.
            typeof(DEF_ClinicTraining).GetMethod("OnEnable",flags).Invoke(t,null);
            typeof(DEF_ClinicTraining).GetMethod("Start",flags).Invoke(t,null);
            void Field(object o,string n,object v)=>o.GetType().GetField(n,flags).SetValue(o,v);
            void AdvanceIntroduction(){Field(t,"revealUntil",Time.unscaledTime-1);typeof(DEF_ClinicTraining).GetMethod("Update",flags).Invoke(t,null);}
            var player=new GameObject("Patient").AddComponent<MED_PlayerResources>();t.patient=player;
            var tool=new GameObject("Sickle").AddComponent<MED_HarvestTool>();
            var source=new GameObject("Random herbs").AddComponent<MED_Harvestable>();
            Field(source,"destroyOnEmpty",false);source.Stat.Set(1);source.gameObject.AddComponent<DEF_ClinicRandomSource>();
            int before=player.Get(MED_ResourceType.Herbs);
            Check(!source.TryHarvest(tool,player)&&source.Stat.Value==1&&player.Get(MED_ResourceType.Herbs)==before,"initial introduction blocks real harvest without depletion or gain");
            AdvanceIntroduction();
            Check(source.TryHarvest(tool,player)&&source.IsEmpty,"real herbs exhausted before Water");
            var supply=new GameObject("Supply").AddComponent<MED_Harvestable>();Field(supply,"destroyOnEmpty",false);Field(supply,"requiredTool",MED_ToolType.Crowbar);supply.ConfigureResource(MED_ResourceType.Alcohol);supply.Stat.Set(1);Field(tool,"toolType",MED_ToolType.Crowbar);
            Check(supply.TryHarvest(tool,player)&&supply.IsEmpty,"real supplies exhausted before Water");
            var grid=new GameObject("Grid").AddComponent<MED_GridManager>();var spawner=new GameObject("Random source spawner").AddComponent<MED_WildSpawner>();
            var container=new GameObject("Random sources").transform;source.transform.SetParent(container);
            var template=new GameObject("Herb template").AddComponent<MED_Harvestable>();Field(template,"destroyOnEmpty",false);template.Stat.Set(2);
            Field(spawner,"grid",grid);Field(spawner,"container",container);Field(spawner,"herbPatchPrefab",template.gameObject);t.wildSpawner=spawner;
            var water=new GameObject("Water").AddComponent<MED_Harvestable>();Field(water,"destroyOnEmpty",false);Field(water,"requiredTool",MED_ToolType.Bucket);water.ConfigureResource(MED_ResourceType.Water);water.Stat.Set(2);Field(tool,"toolType",MED_ToolType.Bucket);
            Check(water.TryHarvest(tool,player)&&t.Step==7&&(int)typeof(DEF_ClinicTraining).GetField("pendingStep",flags).GetValue(t)==8,"last required harvest starts introduction before Step8");
            Field(water,"nextHitTime",-1f); // Ensure rejection tests the gate, not the hit cooldown.
            float remaining=water.Stat.Value;Check(!water.TryHarvest(tool,player)&&water.Stat.Value==remaining,"next introduction blocks depletion");
            AdvanceIntroduction();
            lines.Add("DIAGNOSTIC step="+t.Step+" pending="+typeof(DEF_ClinicTraining).GetField("pendingStep",flags).GetValue(t)+" subscribed="+typeof(DEF_ClinicTraining).GetField("subscribed",flags).GetValue(t)+" sourceCount="+container.GetComponentsInChildren<MED_Harvestable>().Length+" templateValue="+template.Stat.Value+" grid="+grid.GridSize);
            foreach(var h in container.GetComponentsInChildren<MED_Harvestable>())lines.Add("SOURCE "+h.name+" value="+h.Stat.Value+" active="+h.isActiveAndEnabled+" owner="+h.GetComponent<DEF_ClinicRandomSource>()?.Owner);
            Check(t.Step==8&&container.GetComponentsInChildren<MED_Harvestable>().Length==2,"Step8 replenishes exhausted random sources with real object");
            Check(!(bool)typeof(DEF_ClinicTraining).GetField("randomFound",flags).GetValue(t),"replenishment grants no harvest credit");
            var otherSpawner=new GameObject("Other spawner").AddComponent<MED_WildSpawner>();
            var foreign=new GameObject("Foreign random source").AddComponent<MED_Harvestable>();Field(foreign,"destroyOnEmpty",false);foreign.Stat.Set(1);foreign.gameObject.AddComponent<DEF_ClinicRandomSource>().Bind(otherSpawner);Field(tool,"toolType",MED_ToolType.Sickle);
            bool foreignHarvest=foreign.TryHarvest(tool,player);
            Check(foreignHarvest&&!(bool)typeof(DEF_ClinicTraining).GetField("randomFound",flags).GetValue(t),"real harvest from different spawner cannot satisfy Step8");
            var fresh=System.Linq.Enumerable.FirstOrDefault(container.GetComponentsInChildren<MED_Harvestable>(),h=>!h.IsEmpty);Field(tool,"toolType",MED_ToolType.Sickle);
            Check(fresh!=null&&fresh.TryHarvest(tool,player)&&(bool)typeof(DEF_ClinicTraining).GetField("randomFound",flags).GetValue(t),"fresh random source must actually be harvested");
        }
        catch(Exception e){lines.Add("FAIL fixture exception: "+e);}
        finally{foreach(var root in scene.GetRootGameObjects())foreach(var training in root.GetComponentsInChildren<DEF_ClinicTraining>(true))typeof(DEF_ClinicTraining).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(training,null);EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
        System.IO.File.WriteAllLines("../clinic-training-validation.txt",lines);Debug.Log(string.Join("\n",lines));if(lines.Exists(x=>x.StartsWith("FAIL")))throw new Exception("Clinic gate checks failed");
    }
}
