using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
public static class DEF_QuestBuildDiagnostics
{
    [MenuItem("Defense Yard/MP2B/Inspect Last Packaging Report (No Build)")]
    public static void Inspect()
    {
        var lines=new List<string>{"UTC "+DateTime.UtcNow.ToString("o"),"Active="+EditorUserBuildSettings.activeBuildTarget+" Selected="+EditorUserBuildSettings.selectedBuildTargetGroup};
        foreach(var group in new[]{BuildTargetGroup.Standalone,BuildTargetGroup.Android}) {
            var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            var feature=settings!=null?settings.GetFeature<MetaQuestFeature>():null;
            lines.Add(group+": settings="+(settings!=null)+" MetaQuest="+(feature!=null)+" enabled="+(feature!=null&&feature.enabled));
        }
        var report=BuildReport.GetLatestReport();
        if(report!=null) {
            lines.Add("Report="+report.summary.result+" warnings="+report.summary.totalWarnings+" errors="+report.summary.totalErrors);
            var messages=report.steps.SelectMany(s=>s.messages.Select(m=>new{step=s.name,m.type,m.content})).Where(m=>m.type==LogType.Warning||m.type==LogType.Error||m.type==LogType.Exception).ToArray();
            File.WriteAllLines("../quest-build-messages.tsv",messages.Select(m=>m.type+"\t"+m.step+"\t"+m.content.Replace("\n"," ").Replace("\r"," ")));
            lines.Add("Recorded messages="+messages.Length);
        }
        var old=EditorUserBuildSettings.selectedBuildTargetGroup;
        try {
            EditorUserBuildSettings.selectedBuildTargetGroup=BuildTargetGroup.Android;
            var issues=new List<OpenXRFeature.ValidationRule>();
            UnityEditor.XR.OpenXR.OpenXRProjectValidation.GetCurrentValidationIssues(issues,BuildTargetGroup.Android);
            lines.Add("Android-selected validation completed; issues="+issues.Count);
            lines.AddRange(issues.Select(i=>(i.error?"ERROR: ":"WARNING: ")+i.message));
        } catch(Exception e) {lines.Add("Validation exception="+e);}
        finally {EditorUserBuildSettings.selectedBuildTargetGroup=old;}
        File.WriteAllLines("../quest-build-diagnostics.txt",lines);
    }
    [MenuItem("Defense Yard/MP2B/Inspect Staging Sources (No Play)")]
    public static void InspectScenes()
    {
        var lines=new List<string>();
        foreach(var path in new[]{"Assets/Development/Scenes/Defense/DEF_Integration_Staging.unity","Assets/Development/Scenes/Defense/DEF_WorkshopAmmo_Demo.unity","Assets/Development/Scenes/Defense/DEF_Progression_Locked_Demo.unity"}) {
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path); bool opened=!scene.isLoaded;
            if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
            lines.Add(path);
            foreach(var root in scene.GetRootGameObjects())lines.Add("ROOT "+root.name+" :: "+string.Join(",",root.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null).Select(c=>c.GetType().Name).Distinct()));
            if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
        }
        File.WriteAllLines("../staging-source-inventory.txt",lines);
    }}