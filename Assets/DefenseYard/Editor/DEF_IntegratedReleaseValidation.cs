using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DEF_IntegratedReleaseValidation
{
    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new Exception("Use the isolated batch project.");
        DEF_AuditValidation.RunBatch();
        DEF_PowerValidation.RunPower();
        DEF_ClinicTrainingValidation.Run();
        DEF_ClearanceEscapeValidation.Run();
        DEF_IntegratedPresentationValidation.Run();
        DEF_CellDockValidation.Run();
        DEF_QuestXRValidation.Validate();
        var scene = EditorSceneManager.OpenScene("Assets/DefenseYard/Scenes/DEF_Clinic_Integration_Quest.unity");
        int rigs = 0;
        foreach (var root in scene.GetRootGameObjects()) rigs += root.GetComponentsInChildren<Unity.XR.CoreUtils.XROrigin>(true).Length;
        if (rigs != 1) throw new Exception("Integrated scene must have exactly one XR Origin.");
        var buildScenes = Array.FindAll(EditorBuildSettings.scenes, s => s.enabled);
        if (buildScenes.Length != 1 || buildScenes[0].path != scene.path) throw new Exception("Production build entry changed.");
        File.WriteAllText("../integrated-release-validation.txt", "PASS existing eight audit suites, clearance escape, Android XR settings, single rig and production build entry. Static/Edit Mode only.\n");
    }
}
