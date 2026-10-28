using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class DEF_RecordingPlayRunner
{
 static DEF_RecordingPlayRunner(){EditorApplication.playModeStateChanged+=Changed;}
 [MenuItem("Defense Yard/Recording/Run Native Acceptance")]
 public static void Run(){if(EditorApplication.isPlaying)throw new System.Exception("Stop Play before acceptance");DEF_WorkshopRecordingComposer.Prepare();SessionState.SetBool("DEF Recording Probe",true);EditorApplication.isPlaying=true;}
 static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("DEF Recording Probe",false)){SessionState.SetBool("DEF Recording Probe",false);new GameObject("Native recording acceptance probe").AddComponent<DEF_RecordingNativeProbe>();}}
}
