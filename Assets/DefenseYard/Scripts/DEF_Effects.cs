using UnityEngine;

// Visual playback for the saved muzzle flash, blast ring and electrical arc prefabs.
public class DEF_Effects : MonoBehaviour
{
    public enum Kind { Flash = 0, Ring = 1, ElectricArc = 3 }
    public Kind kind;
    public float life = .12f;
    public float scrollSpeed = 8f;
    public float maxRadius = 4f;
    public bool keepMaterial;

    Renderer rend;
    LineRenderer line;
    MaterialPropertyBlock properties;
    Vector3 baseScale, from, to;
    float elapsed, textureOffset;
    bool active;

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        line = GetComponent<LineRenderer>();
        properties = new MaterialPropertyBlock();
        baseScale = transform.localScale;
    }
    void Start() { if (kind == Kind.Flash && !active) gameObject.SetActive(false); }

    public static void SpawnRing(Vector3 position, float radius)
    {
        var prefab = Resources.Load<DEF_Effects>("Prefabs/DEF_BlastRing");
        if (prefab == null) { Debug.LogError("Blast ring prefab is missing."); return; }
        var ring = Instantiate(prefab, position + Vector3.up * .3f, Quaternion.Euler(90, 0, 0));
        ring.maxRadius = radius;
    }
    public static void SpawnArc(Vector3 from, Vector3 to)
    {
        var prefab = Resources.Load<DEF_Effects>("Prefabs/DEF_ShockArc");
        if (prefab == null) { Debug.LogError("Electrical arc prefab is missing."); return; }
        var arc = Instantiate(prefab);
        // Explicit spawn initialization also supports Edit Mode validation, where Awake is not invoked.
        arc.line = arc.GetComponent<LineRenderer>();
        arc.from = from; arc.to = to;
        arc.ShapeArc();
    }
    void ShapeArc()
    {
        for (int i = 0; i < line.positionCount; i++)
            line.SetPosition(i, Vector3.Lerp(from, to, i / (float)(line.positionCount - 1)) +
                (i == 0 || i == line.positionCount - 1 ? Vector3.zero : Random.insideUnitSphere * .15f));
    }
    public void Flash()
    {
        gameObject.SetActive(true);
        active = true; elapsed = 0;
    }
    void Update()
    {
        if (kind == Kind.ElectricArc)
        {
            if (Time.deltaTime <= 0) return;
            elapsed += Time.deltaTime;
            if (elapsed >= life) { Destroy(gameObject); return; }
            ShapeArc(); line.widthMultiplier = .035f * Mathf.Clamp01((life - elapsed) / .15f);
        }
        else if (kind == Kind.Ring)
        {
            elapsed += Time.deltaTime;
            float radius = Mathf.Max(.01f, maxRadius * Mathf.Clamp01(elapsed / life));
            transform.localScale = new Vector3(radius * 2, radius * 2, 1);
            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
            rend.GetPropertyBlock(properties);
            properties.SetFloat("_Phase", Time.time * 3f);
            rend.SetPropertyBlock(properties);
            if (elapsed >= life) Destroy(gameObject);
        }
        else if (active)
        {
            elapsed += Time.deltaTime;
            if (rend != null && !keepMaterial)
            {
                textureOffset += scrollSpeed * Time.deltaTime;
                rend.GetPropertyBlock(properties);
                properties.SetVector("_MainTex_ST", new Vector4(1, 1, textureOffset, 0));
                rend.SetPropertyBlock(properties);
            }
            transform.localScale = baseScale * (.5f + .6f * (1 - elapsed / life));
            if (elapsed >= life) { active = false; gameObject.SetActive(false); }
        }
    }

    // Multiply each material's original color without replacing its texture or material slot.
    public static void ApplyDamage(GameObject target, float ratio)
    {
        var block = new MaterialPropertyBlock();
        Color tint = Color.Lerp(new Color(.28f, .21f, .17f), Color.white, Mathf.Clamp01(ratio));
        foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is LineRenderer || renderer.GetComponent<DEF_Effects>() != null || renderer.GetComponent<DEF_UVSurface>() != null) continue;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null || !material.HasProperty("_BaseColor")) continue;
                renderer.GetPropertyBlock(block, i);
                Color original = material.GetColor("_BaseColor");
                Color damaged = original * tint; damaged.a = original.a;
                block.SetColor("_BaseColor", damaged);
                renderer.SetPropertyBlock(block, i);
                block.Clear();
            }
        }
    }
}
