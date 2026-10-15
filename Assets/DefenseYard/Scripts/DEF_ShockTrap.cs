using UnityEngine;

// A grid-mounted electrical unit. Zombies detour around it or attack it like other defenses.
public class DEF_ShockTrap : MonoBehaviour
{
    public const int Cost = 50;
    public float health = 80, maxHealth = 80;
    public float radius = 3, stunSeconds = 2, rechargeSeconds = 7;
    public Transform emitter;
    public LineRenderer coil;
    float rechargeRemaining;
    public float RechargeRemaining => rechargeRemaining;
    public int PulseCount { get; private set; }
    Vector2Int cell;
    bool placed;
    readonly RaycastHit[] dischargeHits = new RaycastHit[64];

    public static bool TryPlace(int x, int z)
    {
        var state = DEF_GameState.I; var pf = DEF_Pathfinder.I;
        var grid = FindFirstObjectByType<DEF_GridManager>();
        if (state == null || pf == null || grid == null || x < 0 || z < 0 || x >= grid.width || z >= grid.depth) return false;
        var position = grid.TileCenter(x, z);
        if (state.scrap < Cost || pf.IsBlocked(x,z) || pf.OccupantAt(x,z) != null ||
            (DEF_BaseStructure.Instance != null && DEF_BaseStructure.Instance.Contains(position, .5f))) return false;
        var prefab = Resources.Load<GameObject>("Prefabs/DEF_ShockTrap");
        if (prefab == null) return false;
        var go = Instantiate(prefab, position, Quaternion.identity);
        var trap = go.GetComponent<DEF_ShockTrap>(); trap.cell = new Vector2Int(x,z); trap.placed = true;
        state.scrap -= Cost; pf.SetBlocked(x,z,true); pf.Register(trap.cell,go); pf.RepathAll();
        DEF_Tutorials.Show("shock", go.transform, "SHOCK TRAP\nStuns nearby zombies for 2s.\n7s recharge. No ammo needed.\nRight grip repairs for 10 scrap.");
        return true;
    }
    void Update()
    {
        if (DEF_GameState.I == null || DEF_GameState.I.playerHealth <= 0 || Time.deltaTime <= 0) return;
        rechargeRemaining = Mathf.Max(0, rechargeRemaining - Time.deltaTime);
        if (coil != null)
        {
            Color color = rechargeRemaining > 0 ? new Color(.08f,.24f,.3f) : new Color(.25f,.9f,1);
            coil.startColor = coil.endColor = color;
        }
        if (rechargeRemaining > 0) return;
        bool fired = false;
        foreach (var zombie in DEF_Zombie.all)
        {
            if (zombie == null || zombie.health <= 0 || zombie.IsStunned) continue;
            if ((zombie.transform.position - transform.position).sqrMagnitude > radius * radius) continue;
            Vector3 from = emitter != null ? emitter.position : transform.position + Vector3.up * .75f;
            // The electrical discharge cannot reach through the outpost or another defense.
            if (!CanReach(from, zombie.AimPoint)) continue;
            if (!zombie.Stun(stunSeconds)) continue;
            DEF_Effects.SpawnArc(from, zombie.AimPoint);
            fired = true;
        }
        if (fired)
        {
            PulseCount++; rechargeRemaining = rechargeSeconds;
            DEF_Audio.Play("repair", transform.position, .4f, 1.7f);
        }
    }
    bool CanReach(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        int count = Physics.RaycastNonAlloc(from, direction.normalized, dischargeHits, direction.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        // Treat a saturated query conservatively; never discharge through unknown cover.
        if (count == dischargeHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var hit = dischargeHits[i].collider;
            if (hit.transform.IsChildOf(transform) || hit.GetComponentInParent<DEF_Zombie>() != null) continue;
            return false;
        }
        return true;
    }
    public bool TryRepair()
    {
        var state = DEF_GameState.I;
        if (state == null || state.scrap < 10 || health >= maxHealth) return false;
        state.scrap -= 10; health = Mathf.Min(maxHealth,health+40);
        DEF_Effects.ApplyDamage(gameObject, health/maxHealth);
        DEF_HUD.Instance?.RecordRepair(); DEF_Audio.Play("repair", transform.position, .6f); return true;
    }
    public void TakeDamage(float amount)
    {
        health = Mathf.Max(0,health-amount); DEF_Effects.ApplyDamage(gameObject,health/maxHealth);
        if (health <= 0) Destroy(gameObject);
    }
    void OnDestroy()
    {
        var pf = DEF_Pathfinder.I;
        if (!placed || pf == null || pf.OccupantAt(cell.x,cell.y) != gameObject) return;
        pf.SetBlocked(cell.x,cell.y,false); pf.Unregister(cell); pf.RepathAll();
    }
}
