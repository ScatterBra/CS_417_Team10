using System;
using System.IO;
using System.Linq;
using Deadzone.Power;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.XR.CoreUtils;

public static class DEF_PowerStaging
{
    public const string Output = "Assets/Development/Scenes/Defense/DEF_Power_Integration_Staging.unity";
    const string Temporary = "Assets/Development/Scenes/Defense/DEF_Power_Source_Temporary.unity";
    static T[] All<T>(Scene scene) where T:Component => scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<T>(true)).ToArray();
    public static void PrepareDesktopBatch()
    {
        if(!Application.isBatchMode || Application.isPlaying)throw new Exception("Batch Edit Mode only");
        const string path="Assets/Development/Scenes/Defense/DEF_Power_Desktop_Demo.unity";
        if(File.Exists(path))throw new Exception("Desktop test scene already exists; inspect before overwrite");
        if(!AssetDatabase.CopyAsset(Output,path))throw new Exception("Desktop copy failed");
        var scene=EditorSceneManager.OpenScene(path);
        var rig=All<XROrigin>(scene).Single();
        rig.GetComponent<DEF_VRInput>().simulateInEditor=true;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/XR Interaction Simulator/XR Interaction Simulator.prefab");
        if(prefab==null)throw new Exception("Installed simulator missing");
        PrefabUtility.InstantiatePrefab(prefab,scene);
        var hold=new GameObject("Power desktop test hold point").transform;hold.SetParent(rig.Camera.transform,false);hold.localPosition=new Vector3(.25f,-.25f,.7f);
        All<PWR_InteractionInput>(scene).Single().desktopHoldPoint=hold;
        if(new SerializedObject(All<DEF_Progression>(scene).Single()).FindProperty("clinic").objectReferenceValue!=null)throw new Exception("Unexpected Clinic provider");
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText("../power-desktop-preparation.txt","Separate simulator copy, authentic Power adapter, Clinic locked. No gameplay executed.");
    }
    public static void SetDesktopPowerStartBatch()
    {
        if(!Application.isBatchMode || Application.isPlaying)throw new Exception("Batch Edit Mode only");
        var scene=EditorSceneManager.OpenScene("Assets/Development/Scenes/Defense/DEF_Power_Desktop_Demo.unity");
        var original=EditorSceneManager.OpenScene("Assets/Development/Scenes/Power/PWR.unity",OpenSceneMode.Additive);
        try
        {
            var source=All<XROrigin>(original).Single();var target=All<XROrigin>(scene).Single();
            target.transform.SetPositionAndRotation(source.transform.position+new Vector3(-40,0,0),source.transform.rotation);
            EditorSceneManager.SaveScene(scene);
        }
        finally {EditorSceneManager.CloseScene(original,true);}
    }
    public static void ComposeBatch()
    {
        if(!Application.isBatchMode || Application.isPlaying) throw new Exception("Batch Edit Mode only");
        if(File.Exists(Output) || File.Exists(Temporary)) throw new Exception("Inspect existing staging before regenerating");
        if(!AssetDatabase.CopyAsset("Assets/Development/Scenes/Defense/DEF_Integration_Staging.unity",Output) ||
           !AssetDatabase.CopyAsset("Assets/Development/Scenes/Power/PWR.unity",Temporary)) throw new Exception("Copy failed");
        var destination=EditorSceneManager.OpenScene(Output);
        var power=EditorSceneManager.OpenScene(Temporary,OpenSceneMode.Additive);
        try
        {
            var rig=All<XROrigin>(destination).Single();
            var oldRig=All<XROrigin>(power).Single();
            var director=All<PWR_LevelDirector>(power).Single();
            if(director.charger==null || director.floodlight==null) throw new Exception("Power source references missing");
            // Rebind the station's input bridge to Felix's existing rig; no second rig/UI architecture.
            var bridge=All<PWR_PlayerRig>(power).Single();
            bridge.origin=rig.transform;
            bridge.leftController=PWR_PlayerRig.FindDeep(rig.transform,"Left Controller");
            bridge.rightController=PWR_PlayerRig.FindDeep(rig.transform,"Right Controller");
            if(bridge.leftController==null || bridge.rightController==null) throw new Exception("Controller binding missing");
            foreach(var c in All<MonoBehaviour>(power).Where(x=>x!=null))
            {
                var so=new SerializedObject(c);var field=so.GetIterator();
                while(field.Next(true)) if(field.propertyType==SerializedPropertyType.ObjectReference)
                {
                    if(field.objectReferenceValue==oldRig.Camera)field.objectReferenceValue=rig.Camera;
                    else if(field.objectReferenceValue==oldRig)field.objectReferenceValue=rig;
                    else if(field.objectReferenceValue==oldRig.transform)field.objectReferenceValue=rig.transform;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            UnityEngine.Object.DestroyImmediate(oldRig.gameObject);
            foreach(var es in All<EventSystem>(power))UnityEngine.Object.DestroyImmediate(es.gameObject);
            foreach(var camera in All<Camera>(power))UnityEngine.Object.DestroyImmediate(camera.gameObject);
            // One shared locomotion owner. Power's non-XR fallback disables camera tracking and must not run on Felix's rig.
            foreach(var fallback in All<PWR_DesktopRig>(power))fallback.enabled=false;
            // Owner 9172 wall clearance, applied only to our temporary copy before its one translation.
            // Accept an already updated owner source; reject ambiguous geometry instead of moving another wall.
            var wall = All<Transform>(power).Where(t => t.name == "Cube (6)" &&
                Vector3.Distance(t.localPosition, new Vector3(-.03f,1.55f,7.45f)) < .001f &&
                Vector3.Distance(t.localScale, new Vector3(15,3,.1f)) < .001f).ToArray();
            if(wall.Length == 1)
            {
                wall[0].localPosition = new Vector3(.95f,1.55f,7.45f);
                wall[0].localScale = new Vector3(13.110227f,3,.1f);
            }
            else if(wall.Length != 0 || !All<Transform>(power).Any(t => t.name == "Cube (6)" &&
                Vector3.Distance(t.localPosition,new Vector3(.95f,1.55f,7.45f)) < .001f &&
                Vector3.Distance(t.localScale,new Vector3(13.110227f,3,.1f)) < .001f))
                throw new Exception("Owner 9172 wall geometry could not be identified safely");
            foreach(var root in power.GetRootGameObjects())root.transform.position+=new Vector3(-40,0,0);
            SceneManager.MergeScenes(power,destination);
            SceneManager.SetActiveScene(destination);
            var adapter=new GameObject("Defense authentic Power readiness").AddComponent<DEF_PowerReadiness>();
            adapter.director=director;adapter.charger=director.charger;adapter.floodlight=director.floodlight;
            var gate=All<DEF_Progression>(destination).Single();
            var gateSO=new SerializedObject(gate);gateSO.FindProperty("power").objectReferenceValue=adapter;
            gateSO.FindProperty("clinic").objectReferenceValue=null;gateSO.ApplyModifiedPropertiesWithoutUndo();
            int missing=All<Transform>(destination).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            int cross=0;
            foreach(var c in All<MonoBehaviour>(destination).Where(x=>x!=null))
            {
                var it=new SerializedObject(c).GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference)
                {
                    var value=it.objectReferenceValue;var go=value is Component comp?comp.gameObject:value as GameObject;
                    if(go!=null&&!EditorUtility.IsPersistent(go)&&go.scene!=destination)cross++;
                }
            }
            if(missing!=0||cross!=0||All<XROrigin>(destination).Length!=1||All<DEF_PowerReadiness>(destination).Length!=1)throw new Exception($"Invalid staging missing={missing} cross={cross}");
            EditorSceneManager.SaveScene(destination);
            File.WriteAllLines("../power-staging-results.txt",new[]{"Power PR8 objects at -40m X; Workshop at +40m X; Defense original origin.",
                "Felix rig retained; Power input bridge rebound; Power fallback camera driver disabled.",
                "One exact director/charger/floodlight readiness adapter. Clinic null, no readiness overrides.",
                "Missing scripts=0 cross-scene refs=0 rigs=1", "Static composition only; spatial traversal and XR interactions NOT verified."});
        }
        finally
        {
            if(power.IsValid()&&power.isLoaded)EditorSceneManager.CloseScene(power,true);
            AssetDatabase.DeleteAsset(Temporary);
        }
    }
}
