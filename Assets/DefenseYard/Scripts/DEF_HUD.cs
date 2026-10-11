using UnityEngine;

public class DEF_HUD : MonoBehaviour
{
    DEF_CameraRig rig;

    void Start()
    {
        rig = FindObjectOfType<DEF_CameraRig>();
    }

    void OnGUI()
    {
        var s = DEF_GameState.I;
        if (s == null) return;

        GUI.Box(new Rect(10, 10, 230, 120), "Defense Yard");
        GUI.Label(new Rect(20, 35, 210, 20), "Health: " + s.playerHealth + " / " + s.maxHealth);
        GUI.Label(new Rect(20, 55, 210, 20), "Ammo: " + s.ammo + " / " + s.maxAmmo);
        GUI.Label(new Rect(20, 75, 210, 20), "Wave: " + s.waveNumber);
        GUI.Label(new Rect(20, 95, 210, 20), "Time: " + s.ClockText() + "  (x" + Time.timeScale + ")");

        if (GUI.Button(new Rect(10, 140, 70, 30), "Pause")) Time.timeScale = 0f;
        if (GUI.Button(new Rect(85, 140, 70, 30), "Play")) Time.timeScale = 1f;
        if (GUI.Button(new Rect(160, 140, 70, 30), "Fast")) Time.timeScale = 3f;

        if (rig != null)
        {
            string mode = rig.IsOrtho() ? "Ortho" : "Persp";
            if (GUI.Button(new Rect(10, 175, 110, 30), "Camera: " + mode)) rig.ToggleProjection();
        }

        if (s.selectedTile.x >= 0)
        {
            GUI.Box(new Rect(10, 215, 250, 60), "Selected Tile");
            GUI.Label(new Rect(20, 240, 230, 20),
                "Tile (" + s.selectedTile.x + ", " + s.selectedTile.y + ") - " + s.selectedZone);
        }

        GUI.Label(new Rect(10, Screen.height - 30, 700, 20),
            "WASD/arrows pan | Wheel or +/- zoom | Q/E rotate | P perspective | Click tile to select | Right-drag pan");
    }
}
