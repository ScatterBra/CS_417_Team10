using UnityEngine;
using UnityEngine.InputSystem;

public class DEF_TileSelector : MonoBehaviour
{
    DEF_GridManager grid;
    Camera cam;
    public LineRenderer footprint;
    public Color validColor = new Color(0.25f, 0.85f, 0.65f);
    public Color blockedColor = new Color(1f, 0.35f, 0.2f);

    void Start()
    {
        grid = FindFirstObjectByType<DEF_GridManager>();
        cam = Camera.main;
        if (footprint != null) footprint.enabled = false;
    }

    void Update()
    {
        if (DEF_VRInput.Active || DEF_HUD.BlockingWorldInput) return;
        var ms = Mouse.current;
        if (ms != null && cam != null && grid != null && ms.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(ms.position.ReadValue());
            RaycastHit[] hits = Physics.RaycastAll(ray);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider == grid.groundCollider && grid.WorldToTile(hit.point, out int hitX, out int hitZ))
                {
                    Select(hitX, hitZ);
                    return;
                }
            }
            if (!DEF_Progression.Allows(DEF_Action.BasicSentry)) { Deselect(); return; }
        Deselect();
        }

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

    public void SelectTile(int x, int z)
    {
        if (grid == null) return;
        Select(x, z);
    }

    void Select(int x, int z)
    {
        if (!DEF_Progression.Allows(DEF_Action.BasicSentry)) { Deselect(); return; }
        Deselect();
        Preview(grid.TileCenter(x, z), true);
        var s = DEF_GameState.I;
        if (s != null)
        {
            s.selectedTile = new Vector2Int(x, z);
            s.selectedZone = grid.ZoneName(x, z);
        }
        DEF_Tutorials.Show("tile", grid.TileCenter(x, z), DEF_VRInput.Active ? "BUILD GRID\nAim at the outline. Right trigger places your defense. A opens the shop." : "BUILD GRID\nRight trigger: place | A: build shop");
    }

    void Deselect()
    {
        if (footprint != null) footprint.enabled = false;
        var s = DEF_GameState.I;
        if (s != null) s.selectedTile = new Vector2Int(-1, -1);
    }

    public void Preview(Vector3 point, bool hasGroundHit)
    {
        if (grid == null || footprint == null) return;
        if ((DEF_Progression.Current != null && !DEF_Progression.Current.IsAllowed(DEF_Action.BasicSentry)) || !hasGroundHit || !grid.WorldToTile(point, out int x, out int z))
        {
            footprint.enabled = false;
            return;
        }
        var pf = DEF_Pathfinder.I;
        Vector3 center = grid.TileCenter(x, z);
        bool allowed = pf != null && !pf.IsBlocked(x, z) && pf.OccupantAt(x, z) == null &&
            (DEF_BaseStructure.Instance == null || !DEF_BaseStructure.Instance.Contains(center, 0.5f));
        footprint.enabled = true;
        footprint.startColor = footprint.endColor = allowed ? validColor : blockedColor;
        float h = grid.tileSize * 0.46f;
        footprint.positionCount = 4;
        footprint.loop = true;
        footprint.SetPositions(new[] { center + new Vector3(-h, 0.035f, -h), center + new Vector3(-h, 0.035f, h),
            center + new Vector3(h, 0.035f, h), center + new Vector3(h, 0.035f, -h) });
    }

    void TryRepair(int x, int z)
    {
        var pf = DEF_Pathfinder.I;
        if (pf == null) return;
        GameObject occ = pf.OccupantAt(x, z);
        if (occ == null) return;
        var bar = occ.GetComponent<DEF_Barricade>();
        if (bar != null) bar.TryRepair();
        else { var turret = occ.GetComponent<DEF_Turret>(); if (turret != null) turret.TryRepair(); }
    }

    void TryHarvest(int x, int z)
    {
        var g = FindFirstObjectByType<DEF_GridManager>();
        if (g == null) return;
        Vector3 center = g.TileCenter(x, z);
        foreach (var c in FindObjectsByType<DEF_AmmoCrate>(FindObjectsSortMode.None))
        {
            if (Vector3.Distance(c.transform.position, center) < g.tileSize)
            {
                c.Harvest(10);
                return;
            }
        }
    }
}
