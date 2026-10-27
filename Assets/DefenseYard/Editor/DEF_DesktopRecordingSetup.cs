using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

// Preparation only. Never starts Play, records, or supplies prerequisite evidence.
public static class DEF_DesktopRecordingSetup
{
    public const string Output="Assets/DefenseYard/Scenes/DEF_Desktop_Recording_Demo.unity";
    const string Source="Assets/DefenseYard/Scenes/DEF_Integration_Staging.unity";
    const string Simulator="Assets/Samples/XR Interaction Toolkit/3.5.1/XR Interaction Simulator/XR Interaction Simulator.prefab";
    public static void PrepareBatch()
    {
        if(!Application.isBatchMode || Application.isPlaying)throw new Exception("Batch Edit Mode only");
        EditorSceneManager.OpenScene("Assets/DefenseYard/Scenes/DEF_WorkshopAmmo_Demo.unity");
        DEF_IntegrationStaging.UpdateStagingInput();Prepare();
    }
    [MenuItem("Defense Yard/MP2B/Prepare Separate Desktop Recording Demo (No Play)")]
    public static void Prepare()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        if(System.IO.File.Exists(Output))throw new Exception("Recording demo exists; refusing overwrite");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Simulator);
        if(prefab==null)throw new Exception("Installed simulator prefab missing");
        var previous=SceneManager.GetActiveScene();Scene scene=default;
        if(!AssetDatabase.CopyAsset(Source,Output))throw new Exception("Scene copy failed");
        try {
            scene=EditorSceneManager.OpenScene(Output,OpenSceneMode.Additive);
            var rig=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<XROrigin>(true)).Single();
            var input=rig.GetComponent<DEF_VRInput>();
            if(input==null)throw new Exception("Staging Defense input missing");
            var right=rig.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Right Controller");
            DEF_IntegrationStaging.BindDefenseAim(input,right);
            input.simulateInEditor=true;input.desktopRig=null;input.xrCamera=rig.Camera;
            if(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<XRInteractionSimulator>(true)).Any())
                throw new Exception("Unexpected simulator already present");
            PrefabUtility.InstantiatePrefab(prefab,scene);
            foreach(var canvas in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)))
                if(canvas.name=="Defense Interface")DEF_IntegrationStaging.EnsureTrackedUI(canvas);
            // Power/Clinic sources and every gameplay gate are copied unchanged.
            var gate=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DEF_Progression>(true)).Single();
            var serialized=new SerializedObject(gate);
            if(serialized.FindProperty("power").objectReferenceValue!=null || serialized.FindProperty("clinic").objectReferenceValue!=null)
                throw new Exception("Source readiness changed; review genuine adapters before recording preparation");
            int missing=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            if(missing!=0)throw new Exception("Missing scripts in recording demo: "+missing);
            EditorSceneManager.SaveScene(scene);
            System.IO.File.WriteAllLines("../desktop-demo-preparation.txt",new[]{"Prepared "+Output,"One existing Felix rig; installed simulator; Near/Far aim; tracked Defense UI", "Power/Clinic sources null; no prerequisite overrides", "Missing scripts="+missing,"No Play Mode or capture executed by preparation"});
        }
        finally {
            if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
        }
    }
}
