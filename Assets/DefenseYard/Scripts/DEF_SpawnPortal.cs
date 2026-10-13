using UnityEngine;

public class DEF_SpawnPortal : MonoBehaviour
{
    public float scrollSpeed = 1.5f;
    public float pulseSpeed = 3f;

    Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Standard"));
        Texture2D tex = new Texture2D(64, 64);
        for (int x = 0; x < 64; x++)
            for (int y = 0; y < 64; y++)
            {
                float v = 0.5f + 0.5f * Mathf.Sin((x + y) * 0.4f);
                tex.SetPixel(x, y, new Color(0.5f * v, 0.1f, 0.8f * v));
            }
        tex.Apply();
        rend.material.mainTexture = tex;
        rend.material.SetColor("_EmissionColor", new Color(0.4f, 0.05f, 0.7f));
        rend.material.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        if (rend == null) return;
        rend.material.mainTextureOffset += new Vector2(scrollSpeed * Time.deltaTime, scrollSpeed * 0.7f * Time.deltaTime);
        rend.material.mainTextureScale = new Vector2(
            1f + 0.3f * Mathf.Sin(Time.time * pulseSpeed),
            1f + 0.3f * Mathf.Cos(Time.time * pulseSpeed));
    }
}
