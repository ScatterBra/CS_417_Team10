using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class DEF_ClinicValidation
{
    [MenuItem("Defense Yard/MP2B/Run All Checks Including Clinic")]
    public static void RunAll(){if(Application.isPlaying)throw new Exception("Edit Mode only");DEF_PowerValidation.RunInEditor();Run();}
    public static void RunBatch(){if(!Application.isBatchMode)throw new Exception("Batch only");DEF_PowerValidation.RunBatch();Run();}
    static void Run()
    {
        var previous=SceneManager.GetActiveScene();var fixture=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(fixture);var results=new List<string>();var recipes=new List<MED_Recipe>();var definitions=new List<MED_BuildingDefinition>();
        var cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(x=>x.enabled).ToArray();foreach(var c in cameras)c.enabled=false;
        var camera=new GameObject("Fixture head").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,1,-.5f);
        void Field(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
        void Call(object o,string n)=>o.GetType().GetMethod(n,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
        void Check(bool ok,string text)=>results.Add((ok?"PASS ":"FAIL ")+text);
        MED_PlayerResources p=null;MED_PrepBench b=null;MED_TreatmentStation s=null;MED_MedicineCabinet cabinet=null;DEF_ClinicReadiness a=null;MED_Recipe recipe=null;int count=0;
        GameObject group=null;
        T Make<T>(string name)where T:Component {var g=new GameObject(name);g.transform.SetParent(group.transform);return g.AddComponent<T>();}
        void Setup()
        {
            if(group!=null)UnityEngine.Object.DestroyImmediate(group);group=new GameObject("Clinic fixture");count=0;
            p=Make<MED_PlayerResources>("Patient");b=Make<MED_PrepBench>("Bench");s=Make<MED_TreatmentStation>("Station");cabinet=Make<MED_MedicineCabinet>("Cabinet");
            Field(b,"player",p);Field(s,"player",p);Field(cabinet,"player",p);Call(s,"OnEnable");Call(cabinet,"OnEnable");
            recipe=ScriptableObject.CreateInstance<MED_Recipe>();recipes.Add(recipe);recipe.duration=1;
            recipe.inputs.Add(new MED_ResourceCost{type=MED_ResourceType.Herbs,amount=2});recipe.inputs.Add(new MED_ResourceCost{type=MED_ResourceType.Water,amount=1});
            recipe.outputs.Add(new MED_ResourceCost{type=MED_ResourceType.Medicine,amount=1});Field(b,"recipes",new[]{recipe});
            a=Make<DEF_ClinicReadiness>("Readiness");a.patient=p;a.bench=b;a.station=s;a.medicineRecipe=recipe;a.Completed+=(_,__,___)=>count++;Call(a,"OnEnable");a.BeginSession(1);
        }
        bool Craft(){p.Add(MED_ResourceType.Herbs,2);p.Add(MED_ResourceType.Water,1);bool started=b.TryStartRecipe(recipe);if(started)Call(b,"Complete");return started;}
        bool Dose(){Field(s,"playerOnPad",true);if(s.State==MED_TreatmentStation.StationState.Idle&&!s.TryTreat())return false;Call(s,"CompleteDose");return true;}
        void FullChain(){for(int i=0;i<3;i++){Craft();s.TryRefill();Dose();}}
        try
        {
            Setup();Check(!b.TryStartRecipe(recipe)&&p.Get(MED_ResourceType.Herbs)==0,"insufficient recipe inputs leave inventory unchanged");
            p.Add(MED_ResourceType.Herbs,2);Check(!b.TryStartRecipe(recipe)&&p.Get(MED_ResourceType.Herbs)==2,"missing second ingredient cannot partially spend first");
            p.Add(MED_ResourceType.Water,1);bool reentered=false;p.OnResourceChanged+=(t,n)=>{if(t==MED_ResourceType.Herbs)reentered=p.TrySpend(MED_ResourceType.Water,1);};
            Check(b.TryStartRecipe(recipe)&&!reentered&&p.Get(MED_ResourceType.Water)==0,"recipe commits all inputs before reentrant notifications");
            Call(b,"Complete");Call(b,"Complete");Check(p.Get(MED_ResourceType.Medicine)==1,"repeated craft completion emits one output");
            Check(cabinet.TryStock()&&cabinet.TryTake()&&s.TryRefill(),"crafted unit survives cabinet stock and take into treatment");Dose();
            Check(p.Health==70&&count==0,"actual crafted dose heals without premature readiness");
            for(int i=0;i<2;i++){Craft();s.TryRefill();Dose();}Check(p.Health==100&&count==1,"actual craft stock dose chain reaches 100 and publishes once");
            Call(s,"CompleteDose");Check(count==1,"repeated dose callback does not duplicate receipt");
            Craft();s.TryRefill();float stock=s.StoredMedicine.Value;Check(!s.TryTreat()&&s.StoredMedicine.Value==stock,"full health rejects treatment without stock loss");
            a.BeginSession(2);Check(count==1,"new full session cannot reuse completed patient readiness");

            Setup();var controller=Make<DEF_Progression>("Clinic consumer");Field(controller,"clinic",a);Call(controller,"OnEnable");
            Craft();s.TryRefill();Dose();int savedGeneration=p.EvidenceGeneration;
            Call(controller,"OnDisable");Call(controller,"OnEnable");
            Check(p.Health==70&&p.EvidenceGeneration==savedGeneration,"controller reconnect preserves partial Clinic treatment and generation");
            Call(controller,"OnDisable");for(int i=0;i<2;i++){Craft();s.TryRefill();Dose();}
            Call(controller,"OnEnable");
            var ready=(HashSet<DEF_ProgressionState.Prerequisite>)typeof(DEF_ProgressionState).GetField("ready",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(controller.State);
            Check(p.Health==100&&count==1&&ready.Contains(DEF_ProgressionState.Prerequisite.Clinic10),"controller reconnect receives completed Clinic chain while other prerequisites remain pending");
            controller.BeginNewSession();Call(controller,"OnDisable");Call(controller,"OnEnable");
            Check(!ready.Contains(DEF_ProgressionState.Prerequisite.Clinic10),"new session cannot replay old Clinic completion");

            Setup();p.Add(MED_ResourceType.Medicine,3);for(int i=0;i<3;i++){s.TryRefill();Dose();}Check(p.Health==100&&count==0,"debug Medicine heals normally but never authenticates craft");
            Setup();s.StoredMedicine.Set(3);for(int i=0;i<3;i++)Dose();Check(p.Health==100&&count==0,"initial station stock never authenticates craft");
            Setup();p.SetHealth(100);Check(count==0,"direct health change never publishes completion");
            Setup();Craft();a.BeginSession(2);s.TryRefill();Dose();Check(count==0&&p.Health==70,"previous session crafted inventory remains useful but unproven");
            Setup();p.Add(MED_ResourceType.Herbs,2);p.Add(MED_ResourceType.Water,1);b.TryStartRecipe(recipe);a.BeginSession(2);Call(b,"Complete");s.TryRefill();Dose();Check(count==0,"craft in flight across reset cannot credit new session");
            Setup();bool nested=false;Craft();p.OnResourceChanged+=(t,n)=>{if(t==MED_ResourceType.Medicine)nested=s.TryRefill();};s.TryRefill();Check(!nested&&s.StoredMedicine.Value==1&&p.Get(MED_ResourceType.Medicine)==0,"reentrant stock request cannot double spend or double stock");
            Setup();Craft();p.OnResourceChanged+=(t,n)=>{if(t==MED_ResourceType.Medicine)a.BeginSession(2);};s.TryRefill();Dose();Check(count==0,"reset during stock notification invalidates transferred receipt");
            Setup();Craft();s.TryRefill();p.OnResourceChanged+=(t,n)=>{if(t==MED_ResourceType.Health)a.BeginSession(2);};Dose();Check(count==0&&p.Health==70&&s.StoredMedicine.Value==0,"reset during dose notification preserves effects without stale readiness");
            Setup();Craft();s.TryRefill();Field(s,"playerOnPad",true);s.TryTreat();Call(s,"OnDisable");Call(s,"CompleteDose");Check(p.Health==50&&s.StoredMedicine.Value==1,"disabling station cancels in-flight dose without consumption");
            Setup();a.enabled=false;FullChain();Check(count==0&&p.Health==100,"disabled adapter observes genuine chain without publishing");a.enabled=true;Call(a,"OnEnable");Check(count==1,"reenabled adapter publishes observed current session once");
            Setup();var other=Make<MED_PrepBench>("Other bench");Field(other,"player",p);Field(other,"recipes",new[]{recipe});var configured=b;b=other;FullChain();b=configured;Check(count==0,"another bench cannot authenticate configured craft source");
            Setup();var otherPatient=Make<MED_PlayerResources>("Other patient");a.patient=otherPatient;FullChain();Check(count==0,"changing configured patient invalidates current source binding");
            Setup();recipe.inputs[0]=new MED_ResourceCost{type=MED_ResourceType.Herbs,amount=-1};Check(!b.TryStartRecipe(recipe)&&p.Get(MED_ResourceType.Medicine)==0,"negative recipe costs rejected without free medicine");
            Setup();s.StoredMedicine.Set(s.StoredMedicine.Max);Craft();Check(!s.TryRefill()&&p.Get(MED_ResourceType.Medicine)==1,"full station preserves exact inventory unit");
            Setup();Craft();s.TryRefill();s.StoredMedicine.Set(0);s.StoredMedicine.Set(1);Dose();Check(count==0,"external stock mutation cannot recreate medicine provenance");
            Setup();var benchDef=ScriptableObject.CreateInstance<MED_BuildingDefinition>();var stationDef=ScriptableObject.CreateInstance<MED_BuildingDefinition>();definitions.Add(benchDef);definitions.Add(stationDef);
            a.buildingsRoot=group.transform;a.benchDefinition=benchDef;a.stationDefinition=stationDef;a.BeginSession(2);
            Check(a.bench==null&&a.station==null,"dynamic Clinic adapter waits for fresh actual placed-building callbacks");
            var outside=new GameObject("Foreign building");SceneManager.MoveGameObjectToScene(outside,fixture);var foreign=outside.AddComponent<MED_Building>();Field(foreign,"popDuration",0f);foreign.Initialize(benchDef,new[]{Vector2Int.zero},0);
            Check(a.bench==null,"foreign Clinic container cannot bind readiness sources");
            var builtBench=b.gameObject.AddComponent<MED_Building>();Field(builtBench,"popDuration",0f);builtBench.Initialize(benchDef,new[]{Vector2Int.zero},0);
            var builtStation=s.gameObject.AddComponent<MED_Building>();Field(builtStation,"popDuration",0f);builtStation.Initialize(stationDef,new[]{Vector2Int.one},0);
            Check(a.bench==b&&a.station==s,"real scoped OnBuilt callbacks bind exact live bench and station");FullChain();Check(count==1,"dynamically built actual sources complete the authentic treatment chain");
            a.BeginSession(3);Check(a.bench==null&&a.station==null&&count==1,"new full session cannot reuse previous building readiness");
            Setup();a.buildingsRoot=group.transform;a.benchDefinition=benchDef;a.stationDefinition=stationDef;a.BeginSession(2);
            void Place(Component component,MED_BuildingDefinition definition){var building=component.gameObject.AddComponent<MED_Building>();Field(building,"popDuration",0f);building.Initialize(definition,new[]{Vector2Int.zero},0);}
            Place(b,benchDef);Place(s,stationDef);var firstBench=b;var firstStation=s;
            Craft();s.TryRefill();Dose();
            b=Make<MED_PrepBench>("Second legitimate bench");Field(b,"player",p);Field(b,"recipes",new[]{recipe});Place(b,benchDef);
            s=Make<MED_TreatmentStation>("Second legitimate station");Field(s,"player",p);Call(s,"OnEnable");Place(s,stationDef);
            Craft();s.TryRefill();Dose();Check(p.Health==90&&count==0,"actual doses from two legitimate benches and stations continue one health chain");
            Craft(); // This paid output must survive its bench being demolished.
            UnityEngine.Object.DestroyImmediate(b.gameObject);UnityEngine.Object.DestroyImmediate(firstBench.gameObject);UnityEngine.Object.DestroyImmediate(firstStation.gameObject);
            var secondStation=s;UnityEngine.Object.DestroyImmediate(secondStation.gameObject);
            b=Make<MED_PrepBench>("Replacement bench");Field(b,"player",p);Field(b,"recipes",new[]{recipe});Place(b,benchDef);
            s=Make<MED_TreatmentStation>("Replacement station");Field(s,"player",p);Call(s,"OnEnable");Place(s,stationDef);
            s.TryRefill();Dose();Check(p.Health==100&&count==1,"replacement station accepts legitimately crafted unit after original benches and stations are removed");
            a.BeginSession(3);Check(count==1,"multiple-building evidence is cleared on new full session");
        }
        catch(Exception e){results.Add("FAIL "+e);}
        finally {EditorSceneManager.CloseScene(fixture,true);foreach(var r in recipes)UnityEngine.Object.DestroyImmediate(r);foreach(var d in definitions)UnityEngine.Object.DestroyImmediate(d);SceneManager.SetActiveScene(previous);foreach(var c in cameras)if(c!=null)c.enabled=true;}
        System.IO.File.WriteAllLines("../clinic-editmode-results.txt",results);Debug.Log(string.Join("\n",results));
        if(results.Any(x=>x.StartsWith("FAIL")))throw new Exception("Clinic validation failed");
    }
}

