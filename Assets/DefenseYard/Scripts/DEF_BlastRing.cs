using UnityEngine;

public class DEF_BlastRing : MonoBehaviour
{
    public float life = 0.5f;
    public float maxRadius = 4f;

    float t = 0f;
    Renderer rend;

    public static void Spawn(Vector3 pos, float radius)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "BlastRing";
        go.transform.position = pos + Vector3.up * 0.3f;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        DEF_BlastRing r = go.AddComponent<DEF_BlastRing>();
        r.maxRadius = radius;
    }

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Standard"));
        Texture2D tex = new Texture2D(64, 64);
        for (int x = 0; x < 64; x++)
            for (int y = 0; y < 64; y++)
            {
                float d = Mathf.Sqrt((x - 32) * (x - 32) + (y - 32) * (y - 32)) / 32f;
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.7f) * 4f);
                tex.SetPixel(x, y, new Color(1f, 0.5f, 0.1f, ring));
            }
        tex.Apply();
        rend.material.mainTexture = tex;
        rend.material.SetFloat("_Mode", 3f);
        rend.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        rend.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        rend.material.SetInt("_ZWrite", 0);
        rend.material.DisableKeyword("_ALPHATEST_ON");
        rend.material.EnableKeyword("_ALPHABLEND_ON");
        rend.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        rend.material.renderQueue = 3000;
        rend.material.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.05f));
        rend.material.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        t += Time.deltaTime;
        float f = Mathf.Clamp01(t / life);
        float rad = Mathf.Max(0.01f, maxRadius * f);
        transform.localScale = new Vector3(rad * 2f, rad * 2f, 1f);
        if (rend != null) rend.material.mainTextureOffset += Vector2.one * Time.deltaTime * 2f;
        if (t >= life) Destroy(gameObject);
    }
}
