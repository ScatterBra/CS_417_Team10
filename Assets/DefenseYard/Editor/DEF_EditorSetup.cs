using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class DEF_EditorSetup
{
    [MenuItem("Deadzone/Build Defense Yard Scene")]
    static void BuildScene()
    {
        GameObject rig = new GameObject("CameraRig");
        Camera cam = Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            cam = new GameObject("MainCamera").AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        cam.transform.SetParent(rig.transform);
        rig.AddComponent<DEF_CameraRig>();

        new GameObject("GameState").AddComponent<DEF_GameState>();
        new GameObject("Grid").AddComponent<DEF_GridManager>();
        new GameObject("Selector").AddComponent<DEF_TileSelector>();
        new GameObject("HUD").AddComponent<DEF_HUD>();

        if (Object.FindObjectOfType<Light>() == null)
        {
            GameObject lightGo = new GameObject("Directional Light");
            lightGo.AddComponent<Light>().type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        EditorSceneManager.SaveScene(
            EditorSceneManager.GetActiveScene(),
            "Assets/DefenseYard/Scenes/DefenseYard.unity");

        Debug.Log("Defense Yard scene built. You can delete the Editor folder now.");
    }
}
