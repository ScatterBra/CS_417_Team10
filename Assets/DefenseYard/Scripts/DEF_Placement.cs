using UnityEngine;
using UnityEngine.InputSystem;

public class DEF_Placement : MonoBehaviour
{
    void Update()
    {
        var kb = Keyboard.current;
        var s = DEF_GameState.I;
        if (kb == null || s == null) return;
        if (s.selectedTile.x < 0) return;
        int x = s.selectedTile.x;
        int z = s.selectedTile.y;

        if (kb.tKey.wasPressedThisFrame)
        {
            bool boom = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            DEF_Turret.TryPlace(x, z, boom);
        }
        if (kb.bKey.wasPressedThisFrame) DEF_Barricade.TryPlace(x, z);
        if (kb.rKey.wasPressedThisFrame) TryRepair(x, z);
        if (kb.hKey.wasPressedThisFrame) TryHarvest(x, z);
    }

    void TryRepair(int x, int z)
    {
        var pf = DEF_Pathfinder.I;
        if (pf == null) return;
        GameObject occ = pf.OccupantAt(x, z);
        if (occ == null) return;
        var bar = occ.GetComponent<DEF_Barricade>();
        if (bar != null) bar.TryRepair();
    }

    void TryHarvest(int x, int z)
    {
        var grid = FindObjectOfType<DEF_GridManager>();
        if (grid == null) return;
        Vector3 center = grid.TileCenter(x, z);
        foreach (var c in FindObjectsOfType<DEF_AmmoCrate>())
        {
            if (Vector3.Distance(c.transform.position, center) < grid.tileSize)
            {
                c.Harvest(10);
                return;
            }
        }
    }
}
