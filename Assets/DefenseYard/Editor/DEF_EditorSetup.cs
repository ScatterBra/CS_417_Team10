using UnityEditor;
using UnityEditor.SceneManagement;

public static class DEF_EditorSetup
{
    [MenuItem("Defense Yard/Open Ready Scene", false, 0)]
    public static void OpenScene()
    {
        const string scene = "Assets/Development/Scenes/Defense/DefenseYard.unity";
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetActiveScene().path == scene) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(scene);
    }
}
