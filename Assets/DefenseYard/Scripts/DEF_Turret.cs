using UnityEngine;
using UnityEngine.Serialization;

public class DEF_Turret : MonoBehaviour
{
    public static int turretCost = 40;
    public static int boomTurretCost = 70;

    [FormerlySerializedAs("health")] public float turretIntegrity = 100f;
    public float health { get => turretIntegrity; set => turretIntegrity = value; }
    public float maxHealth = 100f;
    public float range = 14f;
    public float cooldown = 0.8f;
    public float damage = 20f;
    public bool explosive = false;
    public int cost = 40;
    [Header("Optional articulated weapon rig")]
    public Transform yawPivot;
    public Transform pitchPivot;
    public Transform muzzlePoint;
    public Transform secondaryMuzzlePoint;
    bool alternateMuzzle;
    Transform ActiveMuzzle => alternateMuzzle && secondaryMuzzlePoint != null ? secondaryMuzzlePoint : muzzlePoint;
    public bool triggerFireAnimation;

    [SerializeField] float shotCooldownRemaining;
    public float ShotCooldownRemaining => shotCooldownRemaining;
    bool placed;
    Vector2Int cell;
    [SerializeField] GameObject head;
    [SerializeField] DEF_Effects flash;
    [SerializeField] Animator mechanism;
    [SerializeField] Renderer muzzle;

    public static bool TryPlace(int x, int z, bool boom)
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
        int price = boom ? boomTurretCost : turretCost;
        if (s.scrap < price) return false;
        var prefab = Resources.Load<GameObject>(boom ? "Prefabs/DEF_BlastTurret" : "Prefabs/DEF_Turret");
        if (prefab == null) { Debug.LogError("Defense gameplay prefab is missing."); return false; }
        s.scrap -= price;
        GameObject go = Instantiate(prefab);
        go.transform.position = grid.TileCenter(x, z);
        DEF_Turret t = go.GetComponent<DEF_Turret>();
        t.placed = true;
        t.cell = new Vector2Int(x, z);
        t.explosive = boom;
        t.cost = price;
        pf.SetBlocked(x, z, true);
        pf.Register(new Vector2Int(x, z), go);
        pf.RepathAll();
        DEF_Tutorials.Show(boom ? "boom" : "turret", go.transform, boom ? "MISSILE SENTRY\nExplosive missiles damage nearby infected." : "SENTRY\nTracks infected automatically. Each shot uses 1 ammo.");
        DEF_Audio.Play("turret_place", go.transform.position, 0.6f);
        return true;
    }

    Transform yawJoint, gunJoint;
    Vector3 facing = Vector3.forward;
    public DEF_Zombie CurrentTarget { get; private set; }
    public Vector3 BarrelForward => muzzlePoint != null ? muzzlePoint.forward : muzzle != null ? muzzle.transform.forward : facing;
    public Vector3 MuzzlePosition => ActiveMuzzle != null ? ActiveMuzzle.position : muzzle != null ? muzzle.bounds.center + muzzle.transform.forward * 0.12f : transform.position + Vector3.up * 1.3f;

    public bool CanSee(DEF_Zombie zombie, Vector3 origin)
    {
        Vector3 direction = zombie.AimPoint - origin;
        foreach (var hit in Physics.RaycastAll(origin, direction.normalized, direction.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<DEF_Zombie>() == zombie) continue;
            if (hit.collider.gameObject.layer == 2) continue;
            return false;
        }
        return true;
    }

    // Animator evaluates before LateUpdate. Correct its aiming joints here so its
    // supplied mechanism animation cannot point the barrel away from the shot.
    void LateUpdate()
    {
        if (Time.deltaTime <= 0 || DEF_GameState.I == null || DEF_GameState.I.playerHealth <= 0) return;
        shotCooldownRemaining = Mathf.Max(0f, shotCooldownRemaining - Time.deltaTime);
        if (head == null) return;
        if (yawJoint == null)
        {
            foreach (var t in head.GetComponentsInChildren<Transform>()) if (t.name == "Bone") { yawJoint = t; break; }
            if (muzzle != null) gunJoint = muzzle.transform.parent;
        }
        CurrentTarget = null;
        float best = float.MaxValue;
        foreach (var zombie in DEF_Zombie.all)
        {
            if (zombie == null || zombie.health <= 0) continue;
            float distance = Vector3.Distance(transform.position, zombie.transform.position);
            if (distance > range || !CanSee(zombie, transform.position + Vector3.up * 1.35f)) continue;
            float priority = distance + (zombie.AttackingBase ? -range : 0);
            if (priority < best) { best = priority; CurrentTarget = zombie; }
        }
        if (mechanism != null) mechanism.speed = CurrentTarget == null ? 0.2f : 1f;
        Vector3 desired = CurrentTarget != null ? (CurrentTarget.AimPoint - MuzzlePosition).normalized : facing;
        facing = Vector3.RotateTowards(facing, desired, 480f * Mathf.Deg2Rad * Time.deltaTime, 0f).normalized;
        if (muzzlePoint != null && yawPivot != null && pitchPivot != null)
        {
            var flat = Vector3.ProjectOnPlane(muzzlePoint.forward, Vector3.up);
            var aimFlat = Vector3.ProjectOnPlane(facing, Vector3.up);
            yawPivot.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(flat, aimFlat, Vector3.up), Vector3.up) * yawPivot.rotation;
            pitchPivot.rotation = Quaternion.FromToRotation(muzzlePoint.forward, facing) * pitchPivot.rotation;
        }
        else if (muzzle != null && yawJoint != null && gunJoint != null)
        {
            var flat = Vector3.ProjectOnPlane(muzzle.transform.forward, Vector3.up);
            var aimFlat = Vector3.ProjectOnPlane(facing, Vector3.up);
            yawJoint.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(flat, aimFlat, Vector3.up), Vector3.up) * yawJoint.rotation;
            gunJoint.rotation = Quaternion.FromToRotation(muzzle.transform.forward, facing) * gunJoint.rotation;
        }
        else head.transform.rotation = Quaternion.LookRotation(facing);
        if (CurrentTarget == null || shotCooldownRemaining > 0) return;
        Vector3 from = MuzzlePosition;
        Vector3 aim = CurrentTarget.AimPoint;
        if (Vector3.Angle(BarrelForward, aim - from) > 6f || !CanSee(CurrentTarget, from)) return;
        var state = DEF_GameState.I;
        if (state.ammo <= 0) return;
        shotCooldownRemaining = cooldown;
        state.ammo--;
        DEF_Tutorials.Show("reload", transform, "RELOADING\nThe moving charge strip shows the firing cooldown.");
        if (flash != null)
        {
            flash.transform.position = from;
            flash.transform.rotation = Quaternion.LookRotation(aim - from);
            flash.Flash();
        }
        if (triggerFireAnimation && mechanism != null) mechanism.SetTrigger("Fire");
        DEF_Projectile.Fire(from, aim, damage, explosive);
        alternateMuzzle = !alternateMuzzle;
        DEF_Audio.Play("turret_shot", from, 0.35f);
    }

    public bool TryRepair()
    {
        var state = DEF_GameState.I;
        if (state == null || health >= maxHealth || state.scrap < 10) return false;
        DEF_HUD.Instance?.RecordRepair();
        state.scrap -= 10; health = Mathf.Min(maxHealth, health + 40);
        UpdateDamageTint(); DEF_Audio.Play("repair", transform.position, 0.6f);
        return true;
    }
    void UpdateDamageTint()
    {
        DEF_Effects.ApplyDamage(gameObject, health / maxHealth);
    }

    public void TakeDamage(float d)
    {
        health = Mathf.Max(0, health - d);
        UpdateDamageTint();
        if (health > 0) DEF_Tutorials.Show("repair-turret", transform, "SENTRY REPAIR\nAim here and squeeze the right grip to restore 40 integrity for 10 scrap.");
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
    void OnDestroy()
    {
        var pf = DEF_Pathfinder.I;
        if (!placed || pf == null || pf.OccupantAt(cell.x, cell.y) != gameObject) return;
        pf.SetBlocked(cell.x, cell.y, false); pf.Unregister(cell); pf.RepathAll();
    }

}
