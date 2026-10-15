using UnityEngine;

// Small world-space bar: a background quad plus a fill quad (no UI canvas).
public class MED_StatBar : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] Transform fillPivot;       // left-anchored; its local X scale is the fill width
    [SerializeField] Renderer fillRenderer;
    [SerializeField] Renderer backgroundRenderer;
    [SerializeField] bool billboard = true;
    [SerializeField, Range(0f, 1f)] float normalized = 1f;
    [SerializeField, Range(0f, 1f)] float alpha = 0.85f;

    MaterialPropertyBlock fillBlock, bgBlock;
    float fullWidth = 1f;
    const float BackgroundAlphaFactor = 0.5f;
    bool initialized;
    Camera cam;

    public float Normalized => normalized;
    public Color FillColor => Evaluate(normalized);

    void Awake() => Init();

    void Init()
    {
        if (initialized) return;
        initialized = true;
        fillBlock = new MaterialPropertyBlock();
        bgBlock = new MaterialPropertyBlock();
        if (fillPivot != null) fullWidth = fillPivot.localScale.x;
        Apply();
    }

    public void SetNormalized(float value)
    {
        Init();
        normalized = Mathf.Clamp01(value);
        Apply();
    }

    public void Bind(MED_Stat stat)
    {
        stat.OnChanged += s => SetNormalized(s.Normalized);
        SetNormalized(stat.Normalized);
    }

    public void SetAlpha(float a)
    {
        Init();
        alpha = Mathf.Clamp01(a);
        Apply();
    }

    public static Color Evaluate(float n)
    {
        var green = new Color(0.25f, 0.8f, 0.35f);
        var yellow = new Color(0.95f, 0.85f, 0.2f);
        var red = new Color(0.9f, 0.2f, 0.2f);
        return n > 0.5f ? Color.Lerp(yellow, green, (n - 0.5f) * 2f) : Color.Lerp(red, yellow, n * 2f);
    }

    void Apply()
    {
        if (fillPivot != null)
        {
            var s = fillPivot.localScale;
            s.x = Mathf.Max(0.0001f, fullWidth * normalized);
            fillPivot.localScale = s;
        }
        if (fillRenderer != null)
        {
            var c = Evaluate(normalized);
            c.a = alpha;
            fillRenderer.GetPropertyBlock(fillBlock);
            fillBlock.SetColor(BaseColorId, c);
            fillRenderer.SetPropertyBlock(fillBlock);
            fillRenderer.enabled = normalized > 0f && alpha > 0.001f;
        }
        if (backgroundRenderer != null)
        {
            backgroundRenderer.GetPropertyBlock(bgBlock);
            bgBlock.SetColor(BaseColorId, new Color(0.05f, 0.05f, 0.05f, alpha * BackgroundAlphaFactor));
            backgroundRenderer.SetPropertyBlock(bgBlock);
            backgroundRenderer.enabled = alpha > 0.001f;
        }
    }

    void LateUpdate()
    {
        if (!billboard) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
    }
}
