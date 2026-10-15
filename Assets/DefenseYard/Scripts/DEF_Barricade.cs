using UnityEngine;
using UnityEngine.Serialization;

public class DEF_Barricade : MonoBehaviour
{
    public static int cost = 20;
    public static int repairCost = 10;

    [FormerlySerializedAs("health")] public float barricadeIntegrity = 80f;
    public float health { get => barricadeIntegrity; set => barricadeIntegrity = value; }
    public float maxHealth = 80f;
    public float repairAmount = 40f;

    Vector2Int cell;

    public static bool TryPlace(int x, int z)
    {
        var s = DEF_GameState.I;
        var pf = DEF_Pathfinder.I;
        var grid = Object.FindFirstObjectByType<DEF_GridManager>();
        if (s == null || pf == null || grid == null) return false;
        if (DEF_BaseStructure.Instance != null && DEF_BaseStructure.Instance.Contains(grid.TileCenter(x, z), 0.5f)) return false;
        if (x < 0 || z < 0 || x >= grid.width || z >= grid.depth) return false;
        if (pf.IsBlocked(x, z) || pf.OccupantAt(x, z) != null) return false;
        Vector2Int bc = pf.BaseCell();
        if (x == bc.x && z == bc.y) return false;
        if (s.scrap < cost) return false;
        var prefab = Resources.Load<GameObject>("Prefabs/DEF_Barricade");
        if (prefab == null) { Debug.LogError("Barricade gameplay prefab is missing."); return false; }
        s.scrap -= cost;
        GameObject go = Instantiate(prefab);
        go.transform.position = grid.TileCenter(x, z);
        Vector3 outward = go.transform.position - grid.TileCenter(bc.x, bc.y);
        go.transform.rotation = Quaternion.Euler(0f, Mathf.Abs(outward.x) > Mathf.Abs(outward.z) ? 90f : 0f, 0f);
        DEF_Barricade b = go.GetComponent<DEF_Barricade>();
        b.cell = new Vector2Int(x, z);
        pf.SetBlocked(x, z, true);
        pf.Register(new Vector2Int(x, z), go);
        pf.RepathAll();
        DEF_Tutorials.Show("barricade", go.transform, "BARRICADE\nBlocks this route. Infected must detour or break it.");
        return true;
    }

    public bool TryRepair()
    {
        var s = DEF_GameState.I;
        if (s == null || health >= maxHealth || s.scrap < repairCost) return false;
        DEF_HUD.Instance?.RecordRepair();
        s.scrap -= repairCost;
        health = Mathf.Min(maxHealth, health + repairAmount);
        DEF_Effects.ApplyDamage(gameObject, health / maxHealth);
        DEF_Audio.Play("repair", transform.position, 0.6f);
        return true;
    }

    public void TakeDamage(float d)
    {
        if (health >= maxHealth) DEF_Tutorials.Show("repair", transform,
            DEF_VRInput.Active ? "REPAIR\nAim here and squeeze the right grip: restore 40 integrity for 10 scrap." : "Right grip repairs for 10 scrap");
        health = Mathf.Max(0, health - d);
        DEF_Effects.ApplyDamage(gameObject, health / maxHealth);
        if (health <= 0f)
        {
            DEF_Audio.Play("barricade_break", transform.position, 0.8f);
            var pf = DEF_Pathfinder.I;
            if (pf != null)
            {
                pf.SetBlocked(cell.x, cell.y, false);
                pf.Unregister(cell);
                pf.RepathAll();
            }
            Destroy(gameObject);
        }
    }
    void OnDestroy()
    {
        var pf = DEF_Pathfinder.I;
        if (pf == null || pf.OccupantAt(cell.x, cell.y) != gameObject) return;
        pf.SetBlocked(cell.x, cell.y, false); pf.Unregister(cell); pf.RepathAll();
    }

}
