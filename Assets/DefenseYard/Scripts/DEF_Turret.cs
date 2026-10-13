using UnityEngine;
using System.Collections.Generic;

public class DEF_Turret : MonoBehaviour
{
    public float health = 100f;
    public float maxHealth = 100f;
    public float range = 9f;
    public float cooldown = 0.8f;
    public float damage = 15f;
    public bool explosive = false;
    public int cost = 40;
    public int boomCost = 70;

    float timer = 0f;
    Vector2Int cell;
    GameObject head;
    DEF_MuzzleFlash flash;

    public static bool TryPlace(int x, int z, bool boom)
    {
        var s = DEF_GameState.I;
        var pf = DEF_Pathfinder.I;
        var grid = Object.FindObjectOfType<DEF_GridManager>();
        if (s == null || pf == null || grid == null) return false;
        if (x < 0 || z < 0 || x >= grid.width || z >= grid.depth) return false;
        if (pf.IsBlocked(x, z) || pf.OccupantAt(x, z) != null) return false;
        Vector2Int bc = pf.BaseCell();
        if (x == bc.x && z == bc.y) return false;
        int price = boom ? 70 : 40;
        if (s.scrap < price) return false;
        s.scrap -= price;

        GameObject go = new GameObject(boom ? "BoomTurret" : "Turret");
        go.transform.position = grid.TileCenter(x, z);
        DEF_Turret t = go.AddComponent<DEF_Turret>();
        t.cell = new Vector2Int(x, z);
        t.explosive = boom;
        t.cost = price;
        t.BuildVisuals();
        pf.SetBlocked(x, z, true);
        pf.Register(new Vector2Int(x, z), go);
        pf.RepathAll();
        return true;
    }

    void BuildVisuals()
    {
        GameObject baseGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseGo.transform.SetParent(transform);
        baseGo.transform.localPosition = Vector3.up * 0.4f;
        baseGo.transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);
        baseGo.GetComponent<Renderer>().material.color = new Color(0.25f, 0.27f, 0.32f);

        head = new GameObject("Head");
        head.transform.SetParent(transform);
        head.transform.localPosition = Vector3.up * 1f;

        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrel.transform.SetParent(head.transform);
        barrel.transform.localPosition = new Vector3(0f, 0f, 0.6f);
        barrel.transform.localScale = new Vector3(0.3f, 0.3f, 1.6f);
        barrel.GetComponent<Renderer>().material.color = explosive
            ? new Color(0.5f, 0.2f, 0.1f)
            : new Color(0.15f, 0.15f, 0.18f);

        GameObject fq = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fq.transform.SetParent(head.transform);
        fq.transform.localPosition = new Vector3(0f, 0f, 1.5f);
        fq.transform.localScale = Vector3.one * 0.8f;
        fq.name = "MuzzleFlash";
        flash = fq.AddComponent<DEF_MuzzleFlash>();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        DEF_Zombie target = null;
        float best = range;
        var copy = new List<DEF_Zombie>(DEF_Zombie.all);
        foreach (var z in copy)
        {
            if (z == null) continue;
            float d = Vector3.Distance(z.transform.position, transform.position);
            if (d < best)
            {
                best = d;
                target = z;
            }
        }
        if (target == null) return;
        Vector3 look = target.transform.position - head.transform.position;
        look.y = 0f;
        if (look.magnitude > 0.01f) head.transform.rotation = Quaternion.LookRotation(look.normalized);
        if (timer > 0f) return;
        timer = cooldown;
        if (flash != null) flash.Flash();
        Vector3 from = head.transform.position + head.transform.forward * 1.2f;
        DEF_Projectile.Fire(from, target.transform.position + Vector3.up * 1f, damage, explosive);
    }

    public void TakeDamage(float d)
    {
        health -= d;
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
