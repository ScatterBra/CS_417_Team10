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
    }

    [MenuItem("Deadzone/Build Defense Yard Phase 2")]
    static void BuildPhase2()
    {
        if (Object.FindObjectOfType<DEF_GameState>() == null)
        {
            EditorUtility.DisplayDialog("Defense Yard",
                "Build Phase 1 first: Deadzone > Build Defense Yard Scene.", "OK");
            return;
        }

        new GameObject("WaveManager").AddComponent<DEF_WaveManager>();
        new GameObject("Pathfinder").AddComponent<DEF_Pathfinder>();
        new GameObject("ThreatMeter").AddComponent<DEF_ThreatMeter>();
        new GameObject("Placement").AddComponent<DEF_Placement>();

        var grid = Object.FindObjectOfType<DEF_GridManager>();
        if (grid != null)
        {
            DEF_AmmoCrate.SpawnAt(grid.TileCenter(3, 3) + Vector3.up * 0.1f, 40);
            DEF_AmmoCrate.SpawnAt(grid.TileCenter(16, 4) + Vector3.up * 0.1f, 40);
            DEF_AmmoCrate.SpawnAt(grid.TileCenter(4, 16) + Vector3.up * 0.1f, 40);

            for (int i = 2; i < grid.width - 2; i += 4)
            {
                MakePortal(grid.TileCenter(i, 1));
                MakePortal(grid.TileCenter(i, grid.depth - 2));
            }
        }

        EditorSceneManager.SaveScene(
            EditorSceneManager.GetActiveScene(),
            "Assets/DefenseYard/Scenes/DefenseYard.unity");
    }

    static void MakePortal(Vector3 pos)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "SpawnPortal";
        go.transform.position = pos + Vector3.up * 1f;
        go.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
        go.AddComponent<DEF_SpawnPortal>();
    }
}
