using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class DEF_Zombie : MonoBehaviour
{
    public static List<DEF_Zombie> all = new List<DEF_Zombie>();

    [FormerlySerializedAs("health")] public float zombieVitality = 50f;
    public float health { get => zombieVitality; set => zombieVitality = value; }
    public float maxHealth = 50f;
    public enum Kind { Walker, Runner, Brute }
    public Kind kind;
    public Color variantTint = Color.white;
    public string DisplayName => kind == Kind.Brute ? "Juggernaut" : kind.ToString();
    public bool AttackingBase { get; private set; }
    public float aimHeight = 1f;
    public float navigationRadius = 0.35f;
    public float turnSpeed = 180f;
    public Vector3 AimPoint => transform.position + Vector3.up * aimHeight;
    public float gaitSpeed = 1.5f;
    float movingUntil;
    float stunnedUntil;
    public bool IsStunned => Time.time < stunnedUntil;
    public bool Stun(float seconds)
    {
        if (health <= 0 || IsStunned) return false;
        stunnedUntil = Time.time + seconds;
        attackingUntil = attackLockedUntil = movingUntil = 0;
        AttackingBase = false;
        return true;
    }
    public float speed = 1.15f;
    public float damage = 4f;
    public float attackRange = 1.6f;
    public float attackTime = 1.8f;
    public int scrapReward = 5;
    public int threatReward = 1;

    List<Vector2Int> path = new List<Vector2Int>();
    int pathIndex = 0;
    List<Vector3> trail;
    int trailIndex = 0;
    [SerializeField] float attackCooldownRemaining;
    public float AttackCooldownRemaining => attackCooldownRemaining;
    float repathTimer = 0f;
    Renderer bodyRenderer;
    DEF_GridManager grid;
    DEF_Pathfinder pf;
    float lastHitSfx = 0f;
    Animator animator;
    string animationState;
    Vector3 previousPosition;
    float attackingUntil;
    float attackLockedUntil;

    void Awake()
    {
        all.Add(this);
    }

    void OnDestroy()
    {
        all.Remove(this);
    }

    public static void Spawn(Vector3 pos, float hpScale, List<Vector3> walkTrail, Kind kind = Kind.Walker)
    {
        var prefab = Resources.Load<GameObject>(kind == Kind.Walker ? "Prefabs/Zombie" : "Prefabs/Zombie_" + kind);
        if (prefab == null) { Debug.LogError("Animated zombie prefab is missing."); return; }
        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        go.transform.position = pos;
        DEF_Zombie z = go.GetComponent<DEF_Zombie>();
        z.maxHealth *= hpScale;
        z.health = z.maxHealth;
        z.trail = walkTrail;
        z.trailIndex = 0;
    }

    void Start()
    {
        grid = FindFirstObjectByType<DEF_GridManager>();
        pf = DEF_Pathfinder.I;

        animator = GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogError("Zombie prefab requires its animated infected rig.", this);
            enabled = false;
            return;
        }
        bodyRenderer = animator.GetComponentInChildren<Renderer>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        previousPosition = transform.position;
        if (Random.value < 0.4f) DEF_Audio.Play("zombie_growl1", transform.position, 0.5f);

        Vector3 inward = pf.CellCenter(pf.BaseCell()) - transform.position;
        inward.y = 0;
        if (inward.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(inward);

        if (trail == null || trail.Count == 0) Repath();
    }

    public void Repath()
    {
        if (pf == null) pf = DEF_Pathfinder.I;
        if (grid == null) grid = FindFirstObjectByType<DEF_GridManager>();
        if (pf == null || grid == null || !pf.Ready) return;
        if (trail != null && TrailClear()) return;
        trail = null;
        path = pf.FindPath(CellOf(transform.position), pf.BaseCell());
        pathIndex = 0;
    }

    bool TrailClear()
    {
        Vector3 from = transform.position;
        for (int i = trailIndex; i < trail.Count; i++)
        {
            if (!pf.CorridorClear(from, trail[i], navigationRadius)) return false;
            from = trail[i];
        }
        return true;
    }

    void OnTriggerEnter(Collider other)
    {
        CheckMelee(other);
    }

    void OnTriggerStay(Collider other)
    {
        CheckMelee(other);
    }

    void CheckMelee(Collider other)
    {
        GameObject go = other.gameObject;
        var bar = go.GetComponentInParent<DEF_Barricade>();
        if (bar != null)
        {
            Attack(bar.gameObject);
            return;
        }
        var trap = go.GetComponentInParent<DEF_ShockTrap>();
        if (trap != null) { Attack(trap.gameObject); return; }
        var tur = go.GetComponentInParent<DEF_Turret>();
        if (tur != null) Attack(tur.gameObject);
    }

    Vector2Int CellOf(Vector3 world)
    {
        int x, z;
        if (grid != null && grid.WorldToTile(world, out x, out z)) return new Vector2Int(x, z);
        return new Vector2Int(0, 0);
    }

    float MoveToward(Vector3 targetPos, float dt)
    {
        Vector3 to = targetPos - transform.position; to.y = 0;
        float distance = to.magnitude;
        if (distance < 0.01f || dt <= 0) return distance;
        Vector3 desired = to.normalized;
        Vector3 separation = Vector3.zero;
        foreach (var other in all)
        {
            if (other == null || other == this || other.health <= 0) continue;
            Vector3 away = transform.position - other.transform.position; away.y = 0;
            float gap = away.magnitude, spacing = navigationRadius + other.navigationRadius + 0.15f;
            if (gap > 0.01f && gap < spacing) separation += away/gap * (1f-gap/spacing);
        }
        Vector3 heading = (desired + Vector3.ClampMagnitude(separation,0.65f)).normalized;
        Vector3 forward = Vector3.RotateTowards(transform.forward,heading,turnSpeed*Mathf.Deg2Rad*dt,0).normalized;
        float step = Mathf.Min(speed*dt,distance);
        Vector3 candidate = transform.position + forward*step;
        if (!pf.CorridorClear(transform.position,candidate,navigationRadius))
        {
            // Never let smoothing or crowd separation cut through a defense corner.
            forward = desired;
            candidate = transform.position + forward*step;
            if (!pf.CorridorClear(transform.position,candidate,navigationRadius)) { repathTimer=0; return distance; }
        }
        transform.position = candidate;
        transform.rotation = Quaternion.LookRotation(forward);
        return distance;
    }

    Vector3 SmoothPathTarget()
    {
        while (pathIndex < path.Count-1 && Vector3.Distance(transform.position,pf.CellCenter(path[pathIndex])) < 0.3f) pathIndex++;
        // Follow the furthest visible route point, not the grid's staircase.
        for (int i=path.Count-1; i>pathIndex; i--)
            if (pf.CorridorClear(transform.position,pf.CellCenter(path[i]),navigationRadius)) { pathIndex=i; break; }
        return pf.CellCenter(path[pathIndex]);
    }

    GameObject AdjacentFoe(Vector2Int cell)
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        GameObject best = null;
        float bestD = float.MaxValue;
        foreach (Vector2Int d in dirs)
        {
            GameObject o = pf.OccupantAt(cell.x + d.x, cell.y + d.y);
            if (o == null) continue;
            if (o.GetComponent<DEF_Barricade>() == null && o.GetComponent<DEF_Turret>() == null && o.GetComponent<DEF_ShockTrap>() == null) continue;
            float dist = Vector3.Distance(o.transform.position, transform.position);
            if (dist < bestD)
            {
                bestD = dist;
                best = o;
            }
        }
        return best;
    }

    void Update()
    {
        var s = DEF_GameState.I;
        if (pf == null || s == null || s.playerHealth <= 0f) return;
        float dt = Time.deltaTime;
        attackCooldownRemaining = Mathf.Max(0f, attackCooldownRemaining - dt);

        if (IsStunned || Time.time < attackLockedUntil) return;
        Vector2Int cell = CellOf(transform.position);
        Vector2Int bc = pf.BaseCell();

        AttackingBase = false;
        var home = DEF_BaseStructure.Instance;
        if (home != null ? home.DistanceToWall(transform.position) <= 0.7f : cell == bc)
        {
            AttackingBase = true;
            Vector3 basePos = pf.CellCenter(bc);
            Vector3 toBase = basePos - transform.position;
            toBase.y = 0f;
            if (toBase.magnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toBase.normalized);
            if ((home != null || toBase.magnitude <= attackRange) && attackCooldownRemaining <= 0f)
            {
                attackCooldownRemaining = attackTime;
                s.DamageBase(damage, transform.position);
                attackingUntil = Time.time + Mathf.Min(.75f, attackTime);
                DEF_Audio.Play("zombie_growl2", transform.position, 0.4f);
            }
            return;
        }

        if (trail != null && trailIndex < trail.Count)
        {
            Vector3 tp = trail[trailIndex];
            if (!pf.CorridorClear(transform.position, tp, navigationRadius))
            {
                trail = null;
                Repath();
            }
            else
            {
                // Blend into the next entry segment before stopping at a waypoint.
                if (trailIndex < trail.Count-1 && Vector3.Distance(transform.position,tp) < 0.8f &&
                    pf.CorridorClear(transform.position,trail[trailIndex+1],navigationRadius)) tp=trail[++trailIndex];
                if (MoveToward(tp, dt) < 0.18f) trailIndex++;
                if (trailIndex >= trail.Count)
                {
                    trail = null;
                    Repath();
                }
                return;
            }
        }

        if (pathIndex < path.Count)
        {
            Vector3 targetPos = SmoothPathTarget();
            Vector2Int next = path[pathIndex];
            GameObject occ = pf.OccupantAt(next.x, next.y);
            if (occ != null)
            {
                Vector3 toOcc = occ.transform.position - transform.position;
                toOcc.y = 0f;
                if (toOcc.magnitude <= attackRange)
                {
                    Attack(occ);
                    return;
                }
                MoveToward(occ.transform.position, dt);
                return;
            }
            if (MoveToward(targetPos, dt) < 0.2f) pathIndex++;
            repathTimer -= dt;
            if (repathTimer <= 0f) { repathTimer=0.75f; Repath(); }
            return;
        }

        repathTimer -= dt;
        if (repathTimer <= 0f)
        {
            repathTimer = 0.5f;
            Repath();
            if (pathIndex < path.Count) return;
        }

        GameObject foe = AdjacentFoe(cell);
        if (foe != null)
        {
            Vector3 toFoe = foe.transform.position - transform.position;
            toFoe.y = 0f;
            if (toFoe.magnitude <= attackRange) Attack(foe);
            else MoveToward(foe.transform.position, dt);
            return;
        }

        Vector3 toBc = pf.CellCenter(bc) - transform.position;
        toBc.y = 0f;
        if (toBc.magnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toBc.normalized);
    }

    void Attack(GameObject occ)
    {
        if (IsStunned || Time.deltaTime <= 0 || DEF_GameState.I == null || DEF_GameState.I.playerHealth <= 0) return;
        Vector3 to = occ.transform.position - transform.position;
        to.y = 0f;
        if (to.magnitude > attackRange) return;
        if (to.magnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to.normalized), 240f * Time.deltaTime);
        if (attackCooldownRemaining > 0f) return;
        attackCooldownRemaining = attackTime;
        attackingUntil = attackLockedUntil = Time.time + Mathf.Min(.7f, attackTime * .5f);
        var bar = occ.GetComponent<DEF_Barricade>();
        if (bar != null)
        {
            bar.TakeDamage(damage);
            DEF_Audio.Play("zombie_growl2", transform.position, 0.4f);
            return;
        }
        occ.GetComponent<DEF_ShockTrap>()?.TakeDamage(damage);
        var tur = occ.GetComponent<DEF_Turret>();
        if (tur != null) tur.TakeDamage(damage);
    }

    void LateUpdate()
    {
        float distance = Vector3.Distance(transform.position, previousPosition);
        previousPosition = transform.position;
        if (animator == null || animator.runtimeAnimatorController == null) return;
        bool alive = DEF_GameState.I != null && DEF_GameState.I.playerHealth > 0f;
        if (distance > 0.0005f) movingUntil = Time.time + 0.18f;
        string state = IsStunned ? "Idle" : alive && Time.time < attackingUntil ? "Attack" : alive && Time.time < movingUntil ? "Walk" : "Idle";
        // Match the gait to travel speed; keep attacks and idle at their authored rate.
        animator.speed = IsStunned ? 0f : state == "Walk" ? Mathf.Clamp(speed / Mathf.Max(0.1f, gaitSpeed), 0.1f, 2.5f) : 1f;
        if (state != animationState)
        {
            animator.CrossFadeInFixedTime(state, 0.15f);
            animationState = state;
        }
    }

    public void TakeDamage(float d)
    {
        if (health <= 0) return;
        health = Mathf.Max(0, health - d);
        if (Time.time - lastHitSfx > 0.2f)
        {
            lastHitSfx = Time.time;
            DEF_Audio.Play("zombie_hit", transform.position, 0.45f);
        }
        if (bodyRenderer != null)
        {
            float f = Mathf.Clamp01(health / maxHealth);
            // Preserve the character material under night lighting as damage accumulates.
            bodyRenderer.material.color = Color.Lerp(new Color(0.65f, 0.3f, 0.24f), variantTint, f);
        }
        if (health <= 0f) Die();
    }

    void Die()
    {
        DEF_Audio.Play("zombie_growl2", transform.position, 0.6f, 0.75f);
        var s = DEF_GameState.I;
        if (s != null) s.scrap = Mathf.Min(s.maxScrap, s.scrap + scrapReward);
        var wm = DEF_WaveManager.I;
        if (wm != null) wm.AddThreat(threatReward);
        Destroy(gameObject);
    }
}
