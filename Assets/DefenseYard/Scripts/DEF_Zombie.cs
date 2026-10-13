using UnityEngine;
using System.Collections.Generic;

public class DEF_Zombie : MonoBehaviour
{
    public static List<DEF_Zombie> all = new List<DEF_Zombie>();

    public float health = 50f;
    public float maxHealth = 50f;
    public float speed = 3f;
    public float damage = 8f;
    public float attackRange = 1.6f;
    public float attackTime = 1f;
    public int scrapReward = 5;
    public int threatReward = 2;

    List<Vector2Int> path = new List<Vector2Int>();
    int pathIndex = 0;
    float attackTimer = 0f;
    Vector3 jitter;
    Renderer bodyRenderer;
    DEF_GridManager grid;
    DEF_Pathfinder pf;

    void Awake()
    {
        all.Add(this);
    }

    void OnDestroy()
    {
        all.Remove(this);
    }

    public static void Spawn(Vector3 pos, float hpScale)
    {
        GameObject go = new GameObject("Zombie");
        go.transform.position = pos;
        DEF_Zombie z = go.AddComponent<DEF_Zombie>();
        z.maxHealth = 50f * hpScale;
        z.health = z.maxHealth;
    }

    void Start()
    {
        grid = FindObjectOfType<DEF_GridManager>();
        pf = DEF_Pathfinder.I;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(transform);
        body.transform.localPosition = Vector3.up * 0.9f;
        body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        bodyRenderer = body.GetComponent<Renderer>();
        bodyRenderer.material.color = new Color(0.3f, 0.6f, 0.25f);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(transform);
        head.transform.localPosition = Vector3.up * 1.9f;
        head.transform.localScale = Vector3.one * 0.55f;
        head.GetComponent<Renderer>().material.color = new Color(0.35f, 0.65f, 0.3f);

        jitter = new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));
        Repath();
    }

    public void Repath()
    {
        if (pf == null) pf = DEF_Pathfinder.I;
        if (grid == null) grid = FindObjectOfType<DEF_GridManager>();
        if (pf == null || grid == null) return;
        path = pf.FindPath(CellOf(transform.position), pf.BaseCell());
        pathIndex = 0;
    }

    Vector2Int CellOf(Vector3 world)
    {
        int x, z;
        if (grid != null && grid.WorldToTile(world, out x, out z)) return new Vector2Int(x, z);
        return new Vector2Int(0, 0);
    }

    Vector2Int StepToward(Vector2Int from, Vector2Int to)
    {
        int dx = to.x - from.x;
        int dz = to.y - from.y;
        if (Mathf.Abs(dx) > Mathf.Abs(dz)) return new Vector2Int(Mathf.Clamp(dx, -1, 1), 0);
        return new Vector2Int(0, Mathf.Clamp(dz, -1, 1));
    }

    void Update()
    {
        var s = DEF_GameState.I;
        if (pf == null || s == null) return;
        float dt = Time.deltaTime;
        attackTimer -= dt;

        if (pathIndex < path.Count)
        {
            Vector2Int next = path[pathIndex];
            GameObject occ = pf.OccupantAt(next.x, next.y);
            if (occ != null)
            {
                Attack(occ);
                return;
            }
            Vector3 targetPos = pf.CellCenter(next) + jitter;
            Vector3 to = targetPos - transform.position;
            to.y = 0f;
            if (to.magnitude < 0.25f)
            {
                pathIndex++;
            }
            else
            {
                transform.position += to.normalized * speed * dt;
                transform.rotation = Quaternion.LookRotation(to.normalized);
            }
            return;
        }

        Vector2Int cell = CellOf(transform.position);
        Vector2Int bc = pf.BaseCell();
        Vector3 basePos = pf.CellCenter(bc);
        Vector3 toBase = basePos - transform.position;
        toBase.y = 0f;
        if (toBase.magnitude < attackRange)
        {
            if (attackTimer <= 0f)
            {
                attackTimer = attackTime;
                s.playerHealth -= damage;
            }
            return;
        }
        Vector2Int ahead = cell + StepToward(cell, bc);
        GameObject block = pf.OccupantAt(ahead.x, ahead.y);
        if (block != null && (block.GetComponent<DEF_Barricade>() != null || block.GetComponent<DEF_Turret>() != null))
        {
            Attack(block);
            return;
        }
        transform.position += toBase.normalized * speed * dt;
        if (toBase.magnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toBase.normalized);
    }

    void Attack(GameObject occ)
    {
        Vector3 to = occ.transform.position - transform.position;
        to.y = 0f;
        if (to.magnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to.normalized);
        if (attackTimer > 0f) return;
        attackTimer = attackTime;
        var bar = occ.GetComponent<DEF_Barricade>();
        if (bar != null)
        {
            bar.TakeDamage(damage);
            return;
        }
        var tur = occ.GetComponent<DEF_Turret>();
        if (tur != null) tur.TakeDamage(damage);
    }

    public void TakeDamage(float d)
    {
        health -= d;
        if (bodyRenderer != null)
        {
            float f = Mathf.Clamp01(health / maxHealth);
            bodyRenderer.material.color = new Color(0.3f + 0.5f * (1f - f), 0.6f * f, 0.25f * f);
        }
        if (health <= 0f) Die();
    }

    void Die()
    {
        var s = DEF_GameState.I;
        if (s != null) s.scrap = Mathf.Min(s.maxScrap, s.scrap + scrapReward);
        var tm = DEF_ThreatMeter.I;
        if (tm != null) tm.AddThreat(threatReward);
        Destroy(gameObject);
    }
}
