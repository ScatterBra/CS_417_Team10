using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
public static class DEF_QuestPackaging
{
    const string Source="Assets/DefenseYard/Scenes/DEF_Progression_Locked_Demo.unity";
    const string ScenePath="Assets/DefenseYard/Scenes/DEF_QuestPackaging_Demo.unity";
    const string TestId="com.darylokeke.cs417.mp2btest";
    [MenuItem("Defense Yard/MP2B/Prepare Quest Packaging Copy (No Play)")]
    public static void Prepare()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        if(File.Exists(ScenePath))throw new Exception("Test scene already exists; inspect before replacing it.");
        if(!AssetDatabase.CopyAsset(Source,ScenePath))throw new Exception("Could not copy locked demo");
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);var log=new List<string>();
        try
        {
            SceneManager.SetActiveScene(scene);
            var components=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null).ToArray();
            foreach(var c in components)
            {
                if(c==null||!c.GetType().Name.Contains("Simulator"))continue;
                if(c.GetComponentsInChildren<Transform>(true).Any(t=>t.GetComponents<MonoBehaviour>().Any(b=>b!=null&&b.GetType().Name=="XROrigin")))throw new Exception("Refusing to remove simulator object containing real XR Origin");
                log.Add("Removed simulator-only object: "+c.name+" / "+c.GetType().Name);UnityEngine.Object.DestroyImmediate(c.gameObject);
            }
            var remaining=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null).ToArray();
            int rigs=remaining.Count(c=>c.GetType().Name=="XROrigin");
            if(rigs!=1)throw new Exception("Expected one real XR Origin; found "+rigs);
            if(!remaining.Any(c=>c is DEF_Progression))throw new Exception("Missing fail-closed progression controller");
            foreach(var c in remaining.OfType<DEF_VRInput>())c.simulateInEditor=false;
            var label=new GameObject("INCOMPLETE DEMO - packaging only").AddComponent<TextMeshPro>();label.text="INCOMPLETE MP2B DEMO\nPACKAGING TEST ONLY\nPower / Clinic readiness not connected";label.fontSize=2;label.rectTransform.sizeDelta=new Vector2(8,3);label.transform.position=new Vector3(0,2,2);
            DEF_RouteValidation.Run();
            EditorSceneManager.SaveScene(scene);log.Add("Preserved XR Origins: "+rigs);log.Add("Power/Clinic completion not injected; locked scene remains incomplete.");
            File.WriteAllLines("../quest-packaging-prepare.txt",log);
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }
    [MenuItem("Defense Yard/MP2B/Build Incomplete Quest Demo APK (No Run)")]
    public static void Build() => BuildScene(ScenePath);
    [MenuItem("Defense Yard/MP2B/Build Integrated Candidate APK (No Run)")]
    public static void BuildIntegrated()
    {
        if(Application.isPlaying || EditorApplication.isCompiling) throw new Exception("Wait for Edit Mode compilation");
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) throw new Exception("Preparation cancelled");
        foreach(var path in new[]{"Assets/DefenseYard/Scenes/DEF_Clinic_Integration_Staging.unity","Assets/DefenseYard/Scenes/DEF_Clinic_Integration_Quest.unity","Assets/DefenseYard/Scenes/DEF_Full_Desktop_Demo.unity"})
        {
            if(!File.Exists(path)) throw new Exception("Required integrated source scene missing: "+path);
            EditorSceneManager.OpenScene(path);
            DEF_ReleasePreparation.Prepare();
            DEF_ClinicStaging.BindOwnerManagers();
            var scene=SceneManager.GetActiveScene();
            if(path.EndsWith("_Quest.unity")) {
                foreach(var c in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null).ToArray())
                    if(c.GetType().Name=="XRDeviceSimulator"||c.GetType().Name=="XRInteractionSimulator") {
                        if(c.GetComponentsInChildren<MonoBehaviour>(true).Any(x=>x!=null&&x.GetType().Name=="XROrigin"))throw new Exception("Simulator contains real rig");
                        UnityEngine.Object.DestroyImmediate(c.gameObject);
                    }
                foreach(var input in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DEF_VRInput>(true)))input.simulateInEditor=false;
            }
            DEF_RouteValidation.Run();
            EditorSceneManager.SaveScene(scene);
        }
        DEF_AuditValidation.RunAll();
        DEF_PowerValidation.RunPower(); DEF_ClinicValidation.Run(); DEF_ClinicTrainingValidation.Run();
        foreach(var name in new[]{"repair","progression","ammo-dualmode","audit","runtime-edge","felix-compat","affordance","walkthrough","power","clinic"}) {
            var report="../"+name+"-editmode-results.txt";
            if(!File.Exists(report)||File.ReadAllLines(report).Any(l=>l.StartsWith("FAIL ")))throw new Exception("Missing or failed suite: "+name);
        }
        BuildScene("Assets/DefenseYard/Scenes/DEF_Clinic_Integration_Quest.unity");
    }
    static void BuildScene(string buildScene)
    {
        if(Application.isPlaying)throw new Exception("No Play Mode or execution permitted");
        CheckHeadroom("Builds", "Temp", Path.GetTempPath());
        if(!File.Exists(buildScene))throw new Exception("Prepare and inspect the isolated copy first");
        var priorScene=SceneManager.GetActiveScene();
        var inspected=EditorSceneManager.OpenScene(buildScene,OpenSceneMode.Additive);
        try {
            var all=inspected.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            int missing=inspected.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            if(missing!=0 || all.Count(c=>c!=null&&c.GetType().Name=="XROrigin")!=1 || all.Any(c=>c!=null&&c.GetType().Name.Contains("Simulator")))throw new Exception("Headset preflight: missing="+missing+" rigs="+all.Count(c=>c!=null&&c.GetType().Name=="XROrigin")+" simulator="+string.Join(",",all.Where(c=>c!=null&&c.GetType().Name.Contains("Simulator")).Select(c=>c.GetType().Name+"@"+c.name)));
            if(buildScene.Contains("Clinic_Integration")) {
                var power=all.OfType<DEF_PowerReadiness>().Single();var clinic=all.OfType<DEF_ClinicReadiness>().Single();
                if(power.director==null||power.charger==null||power.floodlight==null||clinic.patient==null||clinic.buildingsRoot==null||clinic.medicineRecipe==null||clinic.benchDefinition==null||clinic.stationDefinition==null)throw new Exception("Authentic station references missing.");
                var training=all.OfType<DEF_ClinicTraining>().Single();
                var owner=all.OfType<MED_ProgressionManager>().Single();
                if(!owner.IntegrationMode || training.patient!=clinic.patient || training.buildingsRoot!=clinic.buildingsRoot || training.medicineRecipe!=clinic.medicineRecipe || training.wildSpawner==null || training.directions==null || all.OfType<MED_AreaGate>().Count()!=1 || all.OfType<MED_AchievementManager>().Count()!=1)throw new Exception("Integrated Clinic manager/training bindings missing or mismatched");
                var progression=new SerializedObject(all.OfType<DEF_Progression>().Single());
                foreach(var field in new[]{"power","clinic","workshop"})if(progression.FindProperty(field).objectReferenceValue==null)throw new Exception("Missing progression source: "+field);
            }
        } finally {EditorSceneManager.CloseScene(inspected,true);SceneManager.SetActiveScene(priorScene);}
        var target=NamedBuildTarget.Android;
        string oldId=PlayerSettings.GetApplicationIdentifier(target), oldName=PlayerSettings.productName;
        if(oldId==TestId)throw new Exception("Test identifier matches existing identifier; stop and review");
        bool oldBundle=EditorUserBuildSettings.buildAppBundle;
        var oldPipeline=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
        var oldQualityPipeline=QualitySettings.renderPipeline;
        var oldTarget=EditorUserBuildSettings.activeBuildTarget;
        var oldSelected=EditorUserBuildSettings.selectedBuildTargetGroup;
        string folder="Builds/DefensePackagingTest/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        Directory.CreateDirectory(folder);
        string apk=folder+"/INCOMPLETE-MP2B-DEMO.apk";
        var snapshots=new Dictionary<string,byte[]>();
        foreach(var path in new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","Assets/Settings/PC_RPAsset.asset","Assets/Settings/UniversalRenderPipelineGlobalSettings.asset"}) {
            // Do not overwrite an unsaved legitimate editor edit with an on-disk snapshot.
            var asset=AssetDatabase.LoadMainAssetAtPath(path);
            if(asset!=null && EditorUtility.IsDirty(asset))throw new Exception("Save or review dirty settings before packaging: "+path);
            snapshots.Add(path,File.ReadAllBytes(path));
            DurableWrite(folder+"/"+Path.GetFileName(path)+".before-build",snapshots[path],FileMode.CreateNew);
        }
        var metadata=new List<string>{"OriginalTarget="+oldTarget,"OriginalSelectedGroup="+oldSelected,"OriginalBundle="+oldBundle,"OriginalId="+oldId,"OriginalProduct="+oldName,"State=BACKUPS_VERIFIED_BUILD_NOT_STARTED","Capacity estimate: prior candidate cache growth about 7 GiB plus 5 GiB reserve; 12 GiB minimum is not a guarantee."};
        WriteMetadata(folder,metadata);
        var log=new List<string>{"Build-only started UTC "+DateTime.UtcNow.ToString("o"),"Explicit scene: "+buildScene,"Output: "+apk,"NO AUTORUN / NO INSTALL / NO PLAY"};
        var errors=new List<Exception>();
        try {
            CheckHeadroom(folder,"Temp",Path.GetTempPath());
            DEF_QuestXRValidation.Validate();
            if(buildScene.Contains("Clinic_Integration")){var quest=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/DefenseYard/DEF_Quest_Pipeline.asset");if(quest==null)throw new Exception("Prepare integrated release copy first: Quest pipeline missing");UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=quest;QualitySettings.renderPipeline=quest;}
            EditorUserBuildSettings.selectedBuildTargetGroup=BuildTargetGroup.Android;
            PlayerSettings.SetApplicationIdentifier(target,TestId);
            PlayerSettings.productName="INCOMPLETE MP2B Packaging Demo";
            EditorUserBuildSettings.buildAppBundle=false;
            CheckHeadroom(folder,"Temp",Path.GetTempPath());
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{buildScene},locationPathName=apk,target=BuildTarget.Android,options=BuildOptions.Development});
            log.Add("Result: "+report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings+" bytes="+report.summary.totalSize+" duration="+report.summary.totalTime);
            if(report.summary.result!=BuildResult.Succeeded)errors.Add(new Exception("Build result: "+report.summary.result));
            File.WriteAllLines(folder+"/build-messages.tsv",report.steps.SelectMany(s=>s.messages.Select(m=>m.type+"\t"+s.name+"\t"+m.content.Replace("\n"," ").Replace("\r"," "))));
        } catch(Exception e){errors.Add(e);}
        finally {
            try {
                Attempt(errors,"restore graphics pipeline",()=>UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=oldPipeline);
                Attempt(errors,"restore quality pipeline",()=>QualitySettings.renderPipeline=oldQualityPipeline);
                Attempt(errors,"identifier",()=>PlayerSettings.SetApplicationIdentifier(target,oldId));
                Attempt(errors,"product",()=>PlayerSettings.productName=oldName);
                Attempt(errors,"bundle",()=>EditorUserBuildSettings.buildAppBundle=oldBundle);
                Attempt(errors,"target",()=> {
                    if(EditorUserBuildSettings.activeBuildTarget!=oldTarget && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(oldTarget),oldTarget))throw new Exception("Target switch returned false");
                });
                Attempt(errors,"selected group",()=>EditorUserBuildSettings.selectedBuildTargetGroup=oldSelected);
            } finally {
                // No broad SaveAssets: restore each captured file even if other cleanup steps fail.
                foreach(var snapshot in snapshots)Attempt(errors,"restore "+snapshot.Key,()=>DurableWrite(snapshot.Key,snapshot.Value,FileMode.Create));
                Attempt(errors,"refresh",()=>AssetDatabase.Refresh());
                foreach(var snapshot in snapshots)Attempt(errors,"verify "+snapshot.Key,()=> {
                    if(!File.ReadAllBytes(snapshot.Key).SequenceEqual(snapshot.Value))throw new IOException("Exact bytes differ");
                });
                Attempt(errors,"verify target",()=> {if(EditorUserBuildSettings.activeBuildTarget!=oldTarget)throw new Exception("Target mismatch");});
                Attempt(errors,"verify selected group",()=> {if(EditorUserBuildSettings.selectedBuildTargetGroup!=oldSelected)throw new Exception("Selected group mismatch");});
                Attempt(errors,"verify bundle",()=> {if(EditorUserBuildSettings.buildAppBundle!=oldBundle)throw new Exception("Bundle mismatch");});
                metadata.Add("State="+(errors.Count==0?"BUILD_AND_RESTORATION_VERIFIED":"ERRORS_REVIEW_REQUIRED"));
                metadata.AddRange(errors.Select(e=>e.ToString()));
                Attempt(errors,"persist recovery metadata",()=>WriteMetadata(folder,metadata));
                log.AddRange(errors.Select(e=>e.ToString()));
                Attempt(errors,"persist build log",()=>DurableWrite("../quest-packaging-build.txt",System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine,log)),FileMode.Create));
            }
        }
        if(errors.Count>0)throw new AggregateException("Build/restoration issues; original failures and durable snapshots retained in "+folder,errors);
    }
    static void Attempt(List<Exception> errors,string step,Action action)
    {try{action();}catch(Exception e){errors.Add(new Exception(step,e));}}
    static void DurableWrite(string path,byte[] bytes,FileMode mode)
    {
        using(var stream=new FileStream(path,mode,FileAccess.Write,FileShare.Read)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
        if(!File.ReadAllBytes(path).SequenceEqual(bytes))throw new IOException("Durable write verification failed: "+path);
    }
    static void WriteMetadata(string folder,List<string> lines)
    {DurableWrite(folder+"/restoration-state.txt",System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine,lines)),FileMode.Create);}
    static void CheckHeadroom(params string[] paths)
    {
        const long required=12L*1024*1024*1024;
        foreach(var root in paths.Select(p=>Path.GetPathRoot(Path.GetFullPath(p))).Distinct(StringComparer.OrdinalIgnoreCase)) {
            long free=new DriveInfo(root).AvailableFreeSpace;
            if(free<required)throw new IOException("Packaging volume "+root+" has "+free+" bytes free; require 12 GiB (observed ~7 GiB growth + 5 GiB reserve). This estimate cannot guarantee capacity.");
        }
    }
}

