using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.XR.CoreUtils;

public static class DEF_ClinicStaging
{
    [MenuItem("Defense Yard/MP2B/Bind Owner Clinic Managers In Active Integrated Scene")]
    public static void BindOwnerManagers()
    {
        if (Application.isPlaying) throw new Exception("Edit Mode only");
        var scene=SceneManager.GetActiveScene();
        var training=All<DEF_ClinicTraining>(scene).Single();
        if (All<MED_ProgressionManager>(scene).Length==0) training.gameObject.AddComponent<MED_ProgressionManager>();
        if (All<MED_AreaGate>(scene).Length==0) training.gameObject.AddComponent<MED_AreaGate>();
        var achievement=All<MED_AchievementManager>(scene).SingleOrDefault();
        if (achievement==null) achievement=training.gameObject.AddComponent<MED_AchievementManager>();
        var so=new SerializedObject(achievement);
        so.FindProperty("trophyPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<MED_Trophy>("Assets/Suhaas/Prefabs/MED_Trophy.prefab");
        so.FindProperty("shelfPosition").vector3Value=new Vector3(7.2f,0,3.5f);
        var clinicRoot=All<MED_GridManager>(scene).Single().transform;
        so.FindProperty("presentationRoot").objectReferenceValue=clinicRoot;
        All<MED_ProgressionManager>(scene).Single().presentationRoot=clinicRoot;
        so.ApplyModifiedPropertiesWithoutUndo();
        if(so.FindProperty("trophyPrefab").objectReferenceValue==null)throw new Exception("Owner trophy prefab missing");
        var spawner=new SerializedObject(training.wildSpawner);
        var patches=spawner.FindProperty("patches"); patches.arraySize=5;
        var names=new[]{"CrateWall","HerbCluster","HerbTrail","ScatteredMix","SupplyCache"};
        for(int i=0;i<names.Length;i++) { var patch=AssetDatabase.LoadAssetAtPath<MED_WildPatch>("Assets/Suhaas/Data/Patches/MED_Patch_"+names[i]+".asset"); if(patch==null)throw new Exception("Missing owner patch "+names[i]); patches.GetArrayElementAtIndex(i).objectReferenceValue=patch; }
        spawner.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
    }
    const string Output="Assets/DefenseYard/Scenes/DEF_Clinic_Integration_Staging.unity";
    const string Temporary="Assets/DefenseYard/Scenes/DEF_Clinic_Source_Temporary.unity";
    static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
    [MenuItem("Defense Yard/MP2B/Prepare Full Desktop Demo No Play")]
    public static void Desktop()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        const string path="Assets/DefenseYard/Scenes/DEF_Full_Desktop_Demo.unity";
        if(File.Exists(path))throw new Exception("Inspect existing desktop copy before overwrite");
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(!AssetDatabase.CopyAsset(Output,path))throw new Exception("Desktop copy failed");
        var scene=EditorSceneManager.OpenScene(path);var rig=All<XROrigin>(scene).Single();
        rig.GetComponent<DEF_VRInput>().simulateInEditor=true;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/XR Interaction Simulator/XR Interaction Simulator.prefab");
        if(prefab==null)throw new Exception("Installed simulator missing");PrefabUtility.InstantiatePrefab(prefab,scene);
        var report=new System.Text.StringBuilder("TEST ONLY full staging copy. Authentic gates retained.\n");
        foreach(var c in rig.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null))
        {
            string type=c.GetType().Name;
            if(!type.Contains("Move")&&!type.Contains("Mediator")&&!type.Contains("BodyTransformer")&&!type.Contains("TrackedPose")&&!type.Contains("InputActionManager"))continue;
            report.AppendLine(type+" enabled="+c.enabled+" active="+c.gameObject.activeInHierarchy+" object="+c.name);
            var it=new SerializedObject(c).GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue!=null)
                report.AppendLine("  "+it.propertyPath+"="+it.objectReferenceValue.name+" @ "+AssetDatabase.GetAssetPath(it.objectReferenceValue));
        }
        EditorSceneManager.SaveScene(scene);File.WriteAllText("../full-desktop-rig-preflight.txt",report.ToString());
    }
    [MenuItem("Defense Yard/MP2B/Compose Clinic Staging No Play")]
    public static void Compose()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        if(File.Exists(Output)||File.Exists(Temporary))throw new Exception("Inspect existing Clinic staging before regenerating");
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(!AssetDatabase.CopyAsset(DEF_PowerStaging.Output,Output)||!AssetDatabase.CopyAsset("Assets/Suhaas/Scenes/MED_Clinic.unity",Temporary))throw new Exception("Scene copy failed");
        var destination=EditorSceneManager.OpenScene(Output);var clinic=EditorSceneManager.OpenScene(Temporary,OpenSceneMode.Additive);
        try
        {
            var rig=All<XROrigin>(destination).Single();var oldRig=All<XROrigin>(clinic).Single();
            var oldPatient=All<MED_PlayerResources>(clinic).Single();
            var patient=new GameObject("Clinic patient resources - separate from Defense base").AddComponent<MED_PlayerResources>();
            SceneManager.MoveGameObjectToScene(patient.gameObject,clinic);EditorUtility.CopySerialized(oldPatient,patient);
            foreach(var c in All<MonoBehaviour>(clinic).Where(x=>x!=null))
            {
                var so=new SerializedObject(c);var it=so.GetIterator();
                while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference)
                {
                    if(it.objectReferenceValue==oldPatient)it.objectReferenceValue=patient;
                    else if(it.objectReferenceValue==oldRig.Camera)it.objectReferenceValue=rig.Camera;
                    else if(it.objectReferenceValue==oldRig)it.objectReferenceValue=rig;
                    else if(it.objectReferenceValue==oldRig.transform)it.objectReferenceValue=rig.transform;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            if(!oldPatient.transform.IsChildOf(oldRig.transform))UnityEngine.Object.DestroyImmediate(oldPatient);
            UnityEngine.Object.DestroyImmediate(oldRig.gameObject);
            foreach(var es in All<EventSystem>(clinic))UnityEngine.Object.DestroyImmediate(es.gameObject);
            foreach(var cam in All<Camera>(clinic))UnityEngine.Object.DestroyImmediate(cam.gameObject);
            foreach(var manager in All<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>(clinic))UnityEngine.Object.DestroyImmediate(manager);
            var placer=All<MED_BuildingPlacer>(clinic).Single();
            var root=(Transform)new SerializedObject(placer).FindProperty("buildingsContainer").objectReferenceValue;
            if(root==null)throw new Exception("Clinic placed-building root missing");
            foreach(var sim in All<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>(clinic))UnityEngine.Object.DestroyImmediate(sim.gameObject);
            foreach(var go in clinic.GetRootGameObjects())go.transform.position+=new Vector3(80,0,0);
            SceneManager.MergeScenes(clinic,destination);SceneManager.SetActiveScene(destination);
            var adapter=new GameObject("Defense authentic Clinic readiness").AddComponent<DEF_ClinicReadiness>();
            adapter.patient=patient;adapter.buildingsRoot=root;
            adapter.medicineRecipe=AssetDatabase.LoadAssetAtPath<MED_Recipe>("Assets/Suhaas/Data/Recipes/MED_Recipe_Medicine.asset");
            adapter.benchDefinition=AssetDatabase.LoadAssetAtPath<MED_BuildingDefinition>("Assets/Suhaas/Data/MED_Def_PrepBench.asset");
            adapter.stationDefinition=AssetDatabase.LoadAssetAtPath<MED_BuildingDefinition>("Assets/Suhaas/Data/MED_Def_TreatmentStation.asset");
            var gate=new SerializedObject(All<DEF_Progression>(destination).Single());gate.FindProperty("clinic").objectReferenceValue=adapter;gate.ApplyModifiedPropertiesWithoutUndo();
            int missing=All<Transform>(destination).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));int cross=0;
            foreach(var c in All<MonoBehaviour>(destination).Where(x=>x!=null))
            {
                var it=new SerializedObject(c).GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference)
                {var v=it.objectReferenceValue;var go=v is Component comp?comp.gameObject:v as GameObject;if(go!=null&&!EditorUtility.IsPersistent(go)&&go.scene!=destination)cross++;}
            }
            if(missing!=0||cross!=0||All<XROrigin>(destination).Length!=1||All<MED_PlayerResources>(destination).Length!=1||adapter.medicineRecipe==null||adapter.benchDefinition==null||adapter.stationDefinition==null)throw new Exception("Clinic staging reference validation failed: missing="+missing+", cross="+cross);
            EditorSceneManager.SaveScene(destination);
            File.WriteAllText("../clinic-staging-results.txt","PASS isolated actual Clinic scene composed; one Felix XR rig; one separate Clinic patient; scoped fresh OnBuilt bench/station binding; actual Medicine recipe; missing scripts=0; cross-scene refs=0. Power and Workshop authentic providers retained. No readiness flags or gameplay executed.");
        }
        finally {if(clinic.IsValid()&&clinic.isLoaded)EditorSceneManager.CloseScene(clinic,true);AssetDatabase.DeleteAsset(Temporary);}
    }
}
