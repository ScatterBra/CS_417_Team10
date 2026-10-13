using UnityEngine;

public class DEF_Barricade : MonoBehaviour
{
    public float health = 80f;
    public float maxHealth = 80f;
    public int cost = 20;
    public int repairCost = 10;
    public float repairAmount = 40f;

    Vector2Int cell;
    Renderer rend;

    public static bool TryPlace(int x, int z)
    {
        var s = DEF_GameState.I;
        var pf = DEF_Pathfinder.I;
        var grid = Object.FindObjectOfType<DEF_GridManager>();
        if (s == null || pf == null || grid == null) return false;
        if (x < 0 || z < 0 || x >= grid.width || z >= grid.depth) return false;
        if (pf.IsBlocked(x, z) || pf.OccupantAt(x, z) != null) return false;
        Vector2Int bc = pf.BaseCell();
        if (x == bc.x && z == bc.y) return false;
        if (s.scrap < 20) return false;
        s.scrap -= 20;

        GameObject go = new GameObject("Barricade");
        go.transform.position = grid.TileCenter(x, z);
        DEF_Barricade b = go.AddComponent<DEF_Barricade>();
        b.cell = new Vector2Int(x, z);
        b.BuildVisuals();
        pf.SetBlocked(x, z, true);
        pf.Register(new Vector2Int(x, z), go);
        pf.RepathAll();
        return true;
    }

    void BuildVisuals()
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.transform.SetParent(transform);
        box.transform.localPosition = Vector3.up * 0.6f;
        box.transform.localScale = new Vector3(1.6f, 1.2f, 0.5f);
        rend = box.GetComponent<Renderer>();
        rend.material.color = new Color(0.45f, 0.32f, 0.18f);
    }

    public bool TryRepair()
    {
        var s = DEF_GameState.I;
        if (s == null || health >= maxHealth || s.scrap < repairCost) return false;
        s.scrap -= repairCost;
        health = Mathf.Min(maxHealth, health + repairAmount);
        return true;
    }

    public void TakeDamage(float d)
    {
        health -= d;
        if (rend != null)
        {
            float f = Mathf.Clamp01(health / maxHealth);
            rend.material.color = new Color(0.45f, 0.32f * f, 0.18f * f);
        }
        if (health <= 0f)
        {
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
}
