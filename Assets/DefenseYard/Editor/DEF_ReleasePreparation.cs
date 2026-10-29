using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Events;
public static class DEF_ReleasePreparation
{
    [MenuItem("Defense Yard/MP2B/Prepare Open Integrated Release Copy")]
    public static void Prepare()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var s=SceneManager.GetActiveScene();if(!new[]{"DEF_Full_Desktop_Demo","DEF_Clinic_Integration_Staging","Deadzone_Final"}.Contains(s.name))throw new Exception("Open isolated full integration copy");
        var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
        var readiness=all.OfType<DEF_ClinicReadiness>().Single();
        var training=all.OfType<DEF_ClinicTraining>().SingleOrDefault();if(training==null)training=readiness.gameObject.AddComponent<DEF_ClinicTraining>();
        training.wildSpawner=all.OfType<MED_WildSpawner>().Single();
        training.patient=readiness.patient;training.buildingsRoot=readiness.buildingsRoot;training.benchDefinition=readiness.benchDefinition;training.medicineRecipe=readiness.medicineRecipe;
        if(training.directions==null){var g=new GameObject("Clinic ordered instructions");SceneManager.MoveGameObjectToScene(g,s);g.transform.position=new Vector3(80,2,6);training.directions=g.AddComponent<TextMesh>();training.directions.characterSize=.07f;training.directions.fontSize=40;training.directions.anchor=TextAnchor.MiddleCenter;training.directions.text="CLINIC: Harvest Herbs, Water and supplies with the correct tools.";}
        var hud=all.OfType<DEF_HUD>().Single();
        if(!s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Button>(true)).Any(b=>b.name=="Full Run Restart"))
        {
            var button=UnityEngine.Object.Instantiate(hud.startButton,hud.startButton.transform.parent);button.name="Full Run Restart";
            button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,training.RestartFullRun);
            var rect=button.GetComponent<RectTransform>();rect.anchoredPosition+=new Vector2(0,-100);
            foreach(var text in button.GetComponentsInChildren<TMPro.TMP_Text>(true))text.text="RESTART FULL RUN";
            foreach(var text in button.GetComponentsInChildren<Text>(true))text.text="RESTART FULL RUN";
        }
        // Dedicated render assets: preserve shared desktop settings and remove Quest SSAO only on copies.
        const string renderer="Assets/DefenseYard/DEF_Quest_Renderer.asset";
        const string pipeline="Assets/DefenseYard/DEF_Quest_Pipeline.asset";
        if(!System.IO.File.Exists(renderer)&&!AssetDatabase.CopyAsset("Assets/Settings/PC_Renderer.asset",renderer))throw new Exception("Renderer copy failed");
        if(!System.IO.File.Exists(pipeline)&&!AssetDatabase.CopyAsset("Assets/Settings/PC_RPAsset.asset",pipeline))throw new Exception("Pipeline copy failed");
        foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(renderer))if(asset.name.Contains("AmbientOcclusion")){var so=new SerializedObject(asset);var active=so.FindProperty("m_Active");if(active!=null){active.boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}}
        var rp=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(pipeline);
        var config=new SerializedObject(rp);var renderers=config.FindProperty("m_RendererDataList");if(renderers==null||renderers.arraySize==0)throw new Exception("Renderer binding missing");
        renderers.GetArrayElementAtIndex(0).objectReferenceValue=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.ScriptableRendererData>(renderer);config.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(rp);
        DEF_StationRoutes.Generate(s);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
        Debug.Log("Integrated copy prepared. Compile/check and manually verify restart button reachability before release.");
    }
}
