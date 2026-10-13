using UnityEngine;

public class DEF_MuzzleFlash : MonoBehaviour
{
    public float life = 0.12f;
    public float scrollSpeed = 8f;

    float t = 0f;
    Renderer rend;
    bool active = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Standard"));
        Texture2D tex = new Texture2D(64, 16);
        for (int x = 0; x < 64; x++)
            for (int y = 0; y < 16; y++)
            {
                float v = 0.5f + 0.5f * Mathf.Sin(x * 0.5f);
                tex.SetPixel(x, y, new Color(1f, 0.7f * v + 0.2f, 0.1f));
            }
        tex.Apply();
        rend.material.mainTexture = tex;
        rend.material.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.1f));
        rend.material.EnableKeyword("_EMISSION");
        gameObject.SetActive(false);
    }

    public void Flash()
    {
        gameObject.SetActive(true);
        active = true;
        t = 0f;
    }

    void Update()
    {
        if (!active) return;
        t += Time.deltaTime;
        if (rend != null) rend.material.mainTextureOffset += Vector2.right * scrollSpeed * Time.deltaTime;
        float f = 1f - t / life;
        transform.localScale = Vector3.one * (0.5f + 0.6f * f);
        if (t >= life)
        {
            active = false;
            gameObject.SetActive(false);
        }
    }
}
