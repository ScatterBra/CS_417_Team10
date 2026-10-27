using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
public static class DEF_IntegrationStaging
{
    const string Path="Assets/DefenseYard/Scenes/DEF_Integration_Staging.unity";
    static T[] All<T>(Scene s) where T:Component => s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
    static void Ref(UnityEngine.Object o,string field,UnityEngine.Object value) {var so=new SerializedObject(o);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    [MenuItem("Defense Yard/MP2B/Compose Isolated Integration Staging (No Play)")]
    public static void Compose()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var previous=SceneManager.GetActiveScene();
        var destination=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
        var copies=new List<string>(); var opened=new List<Scene>{destination};
        try {
            if(destination.GetRootGameObjects().Length!=1 || destination.GetRootGameObjects()[0].name!="Directional Light")throw new Exception("Staging is no longer the pristine canonical copy; refusing overwrite");
            Scene Copy(string source,string suffix) {
                string temp="Assets/DefenseYard/Scenes/DEF_StagingTemporary"+suffix+".unity";
                if(File.Exists(temp))throw new Exception("Temporary scene exists; inspect before reuse: "+temp);
                if(!AssetDatabase.CopyAsset(source,temp))throw new Exception("Copy failed");copies.Add(temp);
                var scene=EditorSceneManager.OpenScene(temp,OpenSceneMode.Additive);opened.Add(scene);return scene;
            }
            var workshop=Copy("Assets/DefenseYard/Scenes/DEF_WorkshopAmmo_Demo.unity","Workshop");
            var defense=Copy("Assets/DefenseYard/Scenes/DEF_Progression_Locked_Demo.unity","Defense");
            var rig=All<XROrigin>(workshop).Single();var oldRig=All<XROrigin>(defense).Single();
            var state=All<DEF_GameState>(defense).Single();var oldState=All<DEF_GameState>(workshop).Single();
            var ledger=All<SCV_ResourceManager>(workshop).Single();var readiness=All<DEF_WorkshopReadiness>(workshop).Single();
            var controller=All<DEF_Progression>(defense).Single();
            Ref(controller,"workshop",readiness);Ref(controller,"repairLedger",ledger);Ref(controller,"power",null);Ref(controller,"clinic",null);
            Ref(readiness,"workbench",All<SCV_Workbench>(workshop).Single());
            Ref(readiness,"crafting",All<SCV_ProgressionManager>(workshop).Single());
            Ref(readiness,"teachingReceiver",All<SCV_AmmoReceiver>(workshop).Single());
            Ref(readiness,"teachingRepairReceiver",All<SCV_RepairReceiver>(workshop).Single());
            var right=rig.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Right Controller");
            if(right==null)throw new Exception("Felix right controller not found; no guessed input binding");
            var input=rig.gameObject.AddComponent<DEF_VRInput>();input.simulateInEditor=false;input.rayOrigin=right;input.xrCamera=rig.Camera;
            BindDefenseAim(input,right);input.desktopRig=null;
            foreach(var intake in All<DEF_WorkshopAmmoReceiver>(workshop)){intake.destination=state;intake.ledger=ledger;intake.transform.position=new Vector3(5,1,5);}
            // Remote showcase buttons stay in their original scene; staging presents physical ammo/repair.
            foreach(var button in All<Button>(workshop)) {
                for(int i=0;i<button.onClick.GetPersistentEventCount();i++) {
                    string method=button.onClick.GetPersistentMethodName(i);
                    if(method=="DeliverAmmo"||method=="RepairNearestDefense")button.gameObject.SetActive(false);
                }
            }
            foreach(var root in workshop.GetRootGameObjects()) {
                if(root.name=="Main Camera"||root.name=="Directional Light"||root.name.Contains("Simulator")||root.GetComponent<DEF_Barricade>()!=null)UnityEngine.Object.DestroyImmediate(root);
                else if(root.GetComponent<DEF_WorkshopAmmoReceiver>()==null)root.transform.position+=new Vector3(40,0,0);
            }
                        foreach(var c in All<MonoBehaviour>(defense).Concat(All<MonoBehaviour>(workshop)).Where(c=>c!=null)) {
                var so=new SerializedObject(c);var field=so.GetIterator();
                while(field.Next(true))if(field.propertyType==SerializedPropertyType.ObjectReference) {
                    var value=field.objectReferenceValue;
                    if(value==oldRig.Camera)field.objectReferenceValue=rig.Camera;
                    else if(value==oldRig)field.objectReferenceValue=rig;
                    else if(value==oldState)field.objectReferenceValue=state;
                    else if(value is DEF_VRInput && ((Component)value).transform.root==oldRig.transform.root)field.objectReferenceValue=input;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            UnityEngine.Object.DestroyImmediate(oldState.gameObject);
            UnityEngine.Object.DestroyImmediate(oldRig.transform.root.gameObject);
            foreach(var root in defense.GetRootGameObjects().Where(r=>r.name=="UI Event System").ToArray())UnityEngine.Object.DestroyImmediate(root);
            SceneManager.MergeScenes(workshop,destination);SceneManager.MergeScenes(defense,destination);
            // World-space staging panels use separate writers and do not replace Felix wrist UI.
            foreach(var root in destination.GetRootGameObjects().Where(r=>r.name=="Defense Interface"||r.name=="Defense progression presentation - independent writer")) {
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=rig.Camera;
                if(root.name=="Defense Interface")EnsureTrackedUI(canvas);
                root.transform.position=root.name=="Defense Interface"?new Vector3(4,2,4):new Vector3(4,4,4);root.transform.localScale=Vector3.one*.003f;
            }
            var report=new List<string>{"Canonical base c5157dd8f8eb76a4e089b21dc4f06322cf3b3e4d; local staging only", "Power/Clinic remain null and locked; no execution", "Workshop shifted +40m X; Defense remains at original grid origin", "Felix wrist objects and existing manager references retained by scene merge"};
            if(All<XROrigin>(destination).Length!=1||All<DEF_GameState>(destination).Length!=1||All<DEF_Progression>(destination).Length!=1)throw new Exception("Duplicate core ownership");
            int missing=0,cross=0;
            foreach(var t in All<Transform>(destination))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            foreach(var c in All<MonoBehaviour>(destination).Where(c=>c!=null)) {
                var it=new SerializedObject(c).GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference) {
                    var value=it.objectReferenceValue;var component=value as Component;var go=value as GameObject;
                    if(component!=null)go=component.gameObject;
                    if(go!=null&&!EditorUtility.IsPersistent(go)&&go.scene!=destination){cross++;report.Add("Cross-scene reference "+c.name+"."+it.propertyPath);}
                }
            }
            report.Add("Missing scripts="+missing+" cross-scene refs="+cross+" XR rigs="+All<XROrigin>(destination).Length+" state="+All<DEF_GameState>(destination).Length);
            report.Add("Ray origin="+right.name+" camera="+rig.Camera.name+" ray interactor="+(input.rayInteractor!=null));
            report.Add("Not runtime verified; spatial layout, panel readability and actual controller paths need approved manual testing");
            if(missing!=0||cross!=0)throw new Exception("Staging reference validation failed");
            EditorSceneManager.SaveScene(destination);File.WriteAllLines("../integration-staging-results.txt",report);
        }
        finally {
            foreach(var scene in opened)if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
            SceneManager.SetActiveScene(previous);
            foreach(var temp in copies)AssetDatabase.DeleteAsset(temp);
        }
    }
    public static void BindDefenseAim(DEF_VRInput input,Transform right)
    {
        var near=right.GetComponentInChildren<NearFarInteractor>(true);
        if(near==null)throw new Exception("Felix Near/Far interactor missing; refusing teleport fallback");
        input.nearFarInteractor=near;input.rayInteractor=null;input.rayOrigin=right;
    }
    public static void EnsureTrackedUI(Canvas canvas)
    {
        if(canvas.GetComponent<TrackedDeviceGraphicRaycaster>()==null)canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
    }
    [MenuItem("Defense Yard/MP2B/Update Local Staging Aim And UI (No Play)")]
    public static void UpdateStagingInput()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var previous=SceneManager.GetActiveScene();
        if(SceneManager.GetSceneByPath(Path).isLoaded)throw new Exception("Close staging first; do not overwrite an open scene");
        var scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
        try {
            var rig=All<XROrigin>(scene).Single();var input=rig.GetComponent<DEF_VRInput>();
            if(input==null)throw new Exception("Defense input missing");
            BindDefenseAim(input,rig.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Right Controller"));
            EnsureTrackedUI(All<Canvas>(scene).Single(c=>c.name=="Defense Interface"));
            EditorSceneManager.SaveScene(scene);
        }
        finally {EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
    }
}
