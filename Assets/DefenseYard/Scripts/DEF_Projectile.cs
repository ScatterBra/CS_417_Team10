using UnityEngine;
using System.Collections.Generic;

public class DEF_Projectile : MonoBehaviour
{
    public Vector3 velocity;
    public float damage = 15f;
    public float life = 3f;
    public bool explosive = false;
    public float blastRadius = 3f;

    public static void Fire(Vector3 from, Vector3 target, float dmg, bool boom)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Projectile";
        go.transform.position = from;
        go.transform.localScale = Vector3.one * 0.3f;
        go.GetComponent<Renderer>().material.color = boom
            ? new Color(1f, 0.3f, 0.1f)
            : new Color(1f, 0.7f, 0.2f);
        DEF_Projectile p = go.AddComponent<DEF_Projectile>();
        Vector3 dir = target - from;
        dir.y = 0f;
        if (dir.magnitude < 0.01f) dir = Vector3.forward;
        p.velocity = dir.normalized * 25f;
        p.velocity.y = 1.5f;
        p.damage = dmg;
        p.explosive = boom;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        transform.position += velocity * dt;
        velocity.y -= 9.8f * dt;
        life -= dt;

        var copy = new List<DEF_Zombie>(DEF_Zombie.all);
        foreach (var z in copy)
        {
            if (z == null) continue;
            Vector3 d = z.transform.position + Vector3.up * 1f - transform.position;
            if (d.magnitude < 0.9f)
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

    void Hit(DEF_Zombie z)
    {
        if (explosive) Explode();
        else z.TakeDamage(damage);
        Destroy(gameObject);
    }

    void Explode()
    {
        var copy = new List<DEF_Zombie>(DEF_Zombie.all);
        foreach (var z in copy)
        {
            if (z == null) continue;
            float dist = Vector3.Distance(z.transform.position, transform.position);
            if (dist < blastRadius) z.TakeDamage(damage * (1f - dist / blastRadius * 0.5f));
        }
        DEF_BlastRing.Spawn(transform.position, blastRadius);
    }
}
