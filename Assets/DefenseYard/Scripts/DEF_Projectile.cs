using UnityEngine;
using UnityEngine.Serialization;

public class DEF_Projectile : MonoBehaviour
{
    public Vector3 velocity;
    public float damage = 15f;
    [FormerlySerializedAs("life")] public float projectileLifetime = 3f;
    public float life { get => projectileLifetime; set => projectileLifetime = value; }
    public static int TriggerHitCount { get; private set; }
    public float speed = 25f;
    public float hitRadius = 0.9f;
    public bool explosive = false;
    public float blastRadius = 3f;

    [SerializeField] Renderer trail;
    MaterialPropertyBlock trailProperties;
    void Awake() { trailProperties = new MaterialPropertyBlock(); }


    bool spent = false;
    internal void Disarm(){spent=true;enabled=false;}

    public static void Fire(Vector3 from, Vector3 target, float dmg, bool boom)
    {
        if (!DEF_Progression.Allows(DEF_Action.Combat) || !DEF_Progression.Allows(boom ? DEF_Action.Missile : DEF_Action.BasicSentry)) return;
        var prefab = Resources.Load<GameObject>("Prefabs/DEF_Projectile");
        if (prefab == null) { Debug.LogError("Projectile gameplay prefab is missing."); return; }
        GameObject go = Instantiate(prefab);
        go.name = "Projectile";
        go.transform.position = from;
        go.GetComponent<Renderer>().material.color = boom
            ? new Color(1f, 0.3f, 0.1f)
            : new Color(1f, 0.7f, 0.2f);
        DEF_Projectile p = go.GetComponent<DEF_Projectile>();
        if (DEF_Progression.Current != null) DEF_WaveManager.I?.TrackProjectile(p);
        Vector3 dir = target - from;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        p.velocity = dir.normalized * p.speed;
        p.damage = dmg;
        p.explosive = boom;
        go.transform.rotation = Quaternion.LookRotation(dir.normalized);

    }

    void OnTriggerEnter(Collider other)
    {
        if(spent || !isActiveAndEnabled || (DEF_Progression.Current!=null && (!DEF_Progression.Current.IsAllowed(DEF_Action.Combat) || !DEF_Progression.Current.IsAllowed(explosive ? DEF_Action.Missile : DEF_Action.BasicSentry))))return;
        if (other.gameObject.name == "Melee") return;
        var z = other.GetComponentInParent<DEF_Zombie>();
        if (z != null && !spent) { TriggerHitCount++; Hit(z); }
    }

    void Update()
    {
        if(spent || !isActiveAndEnabled || (DEF_Progression.Current!=null && (!DEF_Progression.Current.IsAllowed(DEF_Action.Combat) || !DEF_Progression.Current.IsAllowed(explosive ? DEF_Action.Missile : DEF_Action.BasicSentry))))return;
        float dt = Time.deltaTime;
        Vector3 prev = transform.position;
        transform.position += velocity * dt;
        life -= dt;
        if (trail != null)
        {
            trail.GetPropertyBlock(trailProperties);
            trailProperties.SetVector("_MainTex_ST", new Vector4(1, 1, 0, Time.time * 6f));
            trail.SetPropertyBlock(trailProperties);
        }

        for (int i = DEF_Zombie.all.Count - 1; i >= 0; i--)
        {
            var z = DEF_Zombie.all[i];
            if (z == null) continue;
            Vector3 c = z.AimPoint;
            if (SegmentHits(prev, transform.position, c, hitRadius))
            {
                Hit(z);
                return;
            }
        }

        if (life <= 0f || transform.position.y < 0f)
        {
            if (explosive) Explode();
            Destroy(gameObject);
        }
    }

    bool SegmentHits(Vector3 a, Vector3 b, Vector3 p, float r)
    {
        Vector3 ab = b - a;
        float denom = ab.sqrMagnitude;
        float t = denom > 0.000001f ? Vector3.Dot(p - a, ab) / denom : 0f;
        t = Mathf.Clamp01(t);
        Vector3 q = a + ab * t;
        return (q - p).sqrMagnitude <= r * r;
    }

    void Hit(DEF_Zombie z)
    {
        if (spent) return;
        spent = true;
        if (explosive) Explode();
        else {float before=z.health;z.TakeDamage(damage);DEF_Progression.Current?.ProjectileDamage(this,z,before-z.health);}
        Destroy(gameObject);
    }

    void Explode()
    {
        var victims=new System.Collections.Generic.HashSet<string>();
        for (int i = DEF_Zombie.all.Count - 1; i >= 0; i--)
        {
            var z = DEF_Zombie.all[i];
            if (z == null) continue;
            float dist = Vector3.Distance(z.transform.position, transform.position);
            if(dist<blastRadius)
            {
                float before=z.health;
                bool valid=DEF_Progression.Current!=null && DEF_Progression.Current.ValidEnemy(z);
                z.TakeDamage(damage*(1f-dist/blastRadius*.5f));
                float delta=before-z.health;
                if(delta>0 && valid)victims.Add(z.GetEntityId().ToString());
                DEF_Progression.Current?.ProjectileDamage(this,z,delta);
            }
        }
        DEF_Progression.Current?.Splash(this,victims.Count);
        DEF_Effects.SpawnRing(transform.position, blastRadius);
        DEF_Audio.Play("explosion", transform.position, 0.8f);
    }
}
