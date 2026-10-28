using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class DEF_RecordingPlayRunner
{
 static DEF_RecordingPlayRunner(){EditorApplication.playModeStateChanged+=Changed;}
 [MenuItem("Defense Yard/Recording/Run Native Acceptance")]
 public static void Run(){if(EditorApplication.isPlaying)throw new System.Exception("Stop Play before acceptance");DEF_WorkshopRecordingComposer.Prepare();SessionState.SetBool("DEF Recording Probe",true);EditorApplication.isPlaying=true;}
 [MenuItem("Defense Yard/Recording/Run Presentation Acceptance")]
 public static void Presentation(){SessionState.SetBool("DEF Recording Presentation Only",true);Run();}
 static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("DEF Recording Probe",false)){SessionState.SetBool("DEF Recording Probe",false);var probe=new GameObject("Native recording acceptance probe").AddComponent<DEF_RecordingNativeProbe>();probe.presentationOnly=SessionState.GetBool("DEF Recording Presentation Only",false);SessionState.SetBool("DEF Recording Presentation Only",false);}}
}
