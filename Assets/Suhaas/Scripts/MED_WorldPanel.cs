using TMPro;
using UnityEngine;

// A small world-space card (rounded dark background, title, body) built from code. Used by the unlock reveal
// cards and the NEXT GOAL sign. Reuses the tutorial panel's material so it renders like the pop-ups do.
public class MED_WorldPanel
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    public GameObject Root { get; private set; }
    public TextMeshPro Title { get; private set; }
    public TextMeshPro Body { get; private set; }

    Material backgroundMat;
    Material accentMat;
    Color accent;
    float alpha = 1f;

    public static MED_WorldPanel Create(string name, Vector2 size, float titleSize, float bodySize, Color titleColor, Color accent)
    {
        var p = new MED_WorldPanel { accent = accent };
        var tm = MED_TutorialManager.Instance;
        var baseMat = tm != null ? tm.PanelMaterial : null;
        var shader = baseMat != null ? baseMat.shader : Shader.Find("Universal Render Pipeline/Unlit");

        p.backgroundMat = NewMaterial(baseMat, shader, MED_TutorialManager.MakeRoundedTexture(256, 128, 22));
        p.accentMat = NewMaterial(baseMat, shader, MED_TutorialManager.MakeRoundedTexture(64, 16, 8));
        p.backgroundMat.renderQueue = 2990;
        p.accentMat.renderQueue = 2995;

        p.Root = new GameObject(name);
        MakeQuad(p.Root.transform, "Background", Vector3.zero, new Vector3(size.x, size.y, 1f), p.backgroundMat);
        MakeQuad(p.Root.transform, "Accent", new Vector3(0f, size.y * 0.5f - 0.02f * size.y - 0.01f, -0.002f),
            new Vector3(size.x * 0.9f, 0.02f * size.y + 0.006f, 1f), p.accentMat);

        p.Title = MakeText(p.Root.transform, "Title", new Vector3(0f, size.y * 0.24f, -0.004f),
            new Vector2(size.x * 0.9f, size.y * 0.3f), titleSize, FontStyles.Bold, titleColor);
        p.Body = MakeText(p.Root.transform, "Body", new Vector3(0f, -size.y * 0.12f, -0.004f),
            new Vector2(size.x * 0.9f, size.y * 0.6f), bodySize, FontStyles.Normal, new Color(0.92f, 0.94f, 0.96f));
        p.Body.alignment = TextAlignmentOptions.Top;
        p.SetAlpha(1f);
        return p;
    }

    public void SetText(string title, string body)
    {
        Title.text = title;
        Body.text = body;
    }

    public void SetAlpha(float a)
    {
        alpha = a;
        Apply(backgroundMat, new Color(0.05f, 0.08f, 0.1f), 0.9f * a);
        Apply(accentMat, accent, a);
        Title.alpha = a;
        Body.alpha = a;
    }

    public void Destroy()
    {
        if (Root != null) Object.Destroy(Root);
        if (backgroundMat != null) Object.Destroy(backgroundMat);
        if (accentMat != null) Object.Destroy(accentMat);
    }

    static void Apply(Material m, Color c, float a)
    {
        c.a = a;
        m.SetColor(BaseColorId, c);
    }

    static Material NewMaterial(Material baseMat, Shader shader, Texture2D tex)
    {
        var m = baseMat != null ? new Material(baseMat) : new Material(shader);
        m.SetTexture(BaseMapId, tex);
        return m;
    }

    static void MakeQuad(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(q.GetComponent<Collider>());
        q.name = name;
        q.transform.SetParent(parent, false);
        q.transform.localPosition = pos;
        q.transform.localScale = scale;
        var r = q.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    static TextMeshPro MakeText(Transform parent, string name, Vector3 pos, Vector2 size, float fontSize, FontStyles style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        t.rectTransform.sizeDelta = size;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        t.color = color;
        return t;
    }
}
