using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class DEF_AndroidPreflight
{
    [MenuItem("Defense Yard/MP2B/Android Static Preflight (No Build)")]
    public static void Run()
    {
        if(Application.isPlaying)throw new Exception("Read-only Edit Mode preflight only");
        var report=new List<string>{"Static preflight UTC "+DateTime.UtcNow.ToString("o"),"NO BUILD / NO PLAY / NO DEVICE TEST"};
        report.Add("Android module supported: "+BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android));
        report.Add("Active target: "+EditorUserBuildSettings.activeBuildTarget);
        report.Add("Android architecture: "+PlayerSettings.Android.targetArchitectures);
        report.Add("Android backend: "+PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android));
        report.Add("Android APIs: "+string.Join(",",PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)));
        report.Add("Android identifier: "+PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android));
        var external=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.Android.AndroidExternalToolsSettings")).FirstOrDefault(t=>t!=null);
        if(external==null)report.Add("UNVERIFIED custom SDK/NDK/JDK paths: Android settings assembly not loaded");
        else foreach(var name in new[]{"sdkRootPath","ndkRootPath","jdkRootPath","gradlePath"})
        {
            try{var path=external.GetProperty(name,BindingFlags.Public|BindingFlags.Static)?.GetValue(null) as string;report.Add(name+": "+(path??"unavailable")+" exists="+(!string.IsNullOrEmpty(path)&&Directory.Exists(path)));}
            catch(Exception e){report.Add(name+": unavailable "+e.GetType().Name);}
        }
        foreach(var scene in EditorBuildSettings.scenes)report.Add("Global build scene "+scene.enabled+": "+scene.path);
        var assets=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/DefenseYard"}).Select(AssetDatabase.GUIDToAssetPath).ToList();
        var scenes=AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes", "Assets/Development/Scenes/Defense"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();assets.AddRange(scenes);
        int missing=0,objects=0;
        void Check(GameObject root,string path)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {objects++;int count=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);if(count>0){missing+=count;report.Add("MISSING SCRIPT "+path+" :: "+t.name+" count="+count);}}
        }
        foreach(var path in assets.Where(p=>p.EndsWith(".prefab")))Check(AssetDatabase.LoadAssetAtPath<GameObject>(path),path);
        var previous=SceneManager.GetActiveScene();
        foreach(var path in scenes)
        {
            var existing=SceneManager.GetSceneByPath(path);bool opened=!existing.isLoaded;var scene=opened?EditorSceneManager.OpenScene(path,OpenSceneMode.Additive):existing;
            try{foreach(var root in scene.GetRootGameObjects())Check(root,path);}
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        SceneManager.SetActiveScene(previous);
        int unresolved=0;
        foreach(var path in assets)
        {
            foreach(var guid in Regex.Matches(File.ReadAllText(path),@"guid: ([a-fA-F0-9]{32})").Cast<Match>().Select(m=>m.Groups[1].Value).Distinct())
            {if(guid.StartsWith("0000000000000000"))continue;if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid))){unresolved++;report.Add("UNRESOLVED GUID "+path+" :: "+guid);}}
        }
        report.Add("Inspected "+assets.Count+" Defense scenes/prefabs, "+objects+" objects; missing scripts="+missing+" unresolved GUID references="+unresolved);
        foreach(var guid in AssetDatabase.FindAssets("t:Shader",new[]{"Assets/DefenseYard/Shaders"}))
        {var path=AssetDatabase.GUIDToAssetPath(guid);var shader=AssetDatabase.LoadAssetAtPath<Shader>(path);var messages=ShaderUtil.GetShaderMessages(shader);report.Add("Imported shader "+path+": messages="+messages.Length+" current-editor-only");foreach(var message in messages)report.Add(message.severity+" "+message.message);}
        report.Add("Android shader variants, Gradle/IL2CPP packaging, XR runtime, input and device performance NOT TESTED.");
        File.WriteAllLines("../android-static-preflight.txt",report);Debug.Log(string.Join("\n",report));
    }
}
