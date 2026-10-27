using System;
using UnityEngine;

/// <summary>
/// Tells the Clinic whether the player is inside the Clinic area, so its controls, menus and HUD only react
/// there once it shares a scene with the other areas. The area is the build grid plus a margin (walls and
/// doorway); a small extra margin on the way out stops it flickering at the edge. Same idea as PWR_AreaGate.
/// Added at runtime by MED_ProgressionManager.
/// </summary>
public class MED_AreaGate : MonoBehaviour
{
    [Tooltip("Metres around the build grid that still count as the Clinic.")]
    public float margin = 3f;
    [Tooltip("Extra metres the player must go past the edge before counting as outside.")]
    public float exitHysteresis = 1f;

    /// <summary>True while the player's head is in the Clinic area (true when there is no grid).</summary>
    public static bool PlayerInside { get; private set; } = true;
    public static event Action<bool> Changed;

    MED_GridManager grid;

    /// <summary>Clinic controls call this first: true means "ignore the input, the player is elsewhere".</summary>
    public static bool Outside => !PlayerInside;

    void Awake() => PlayerInside = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        PlayerInside = true;
        Changed = null;
    }

    void Update() => Evaluate();

    public void Evaluate()
    {
        if (grid == null) grid = FindAnyObjectByType<MED_GridManager>();
        var cam = Camera.main;
        if (grid == null || cam == null) return;

        float edge = margin + (PlayerInside ? exitHysteresis : 0f);
        Vector3 local = grid.transform.InverseTransformPoint(cam.transform.position);
        float halfW = grid.GridSize.x * grid.CellSize * 0.5f;
        float halfD = grid.GridSize.y * grid.CellSize * 0.5f;
        bool inside = Mathf.Abs(local.x) <= halfW + edge && Mathf.Abs(local.z) <= halfD + edge;

        if (inside == PlayerInside) return;
        PlayerInside = inside;
        Changed?.Invoke(inside);
    }
}
