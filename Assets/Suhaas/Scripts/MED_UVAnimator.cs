using UnityEngine;

// Scrolls the UVs of a renderer that uses the MED_UVScroll shader. Other scripts change the speed with
// SetSpeedMultiplier (e.g. 0 = stopped, 1 = normal), so the motion of the texture signifies what the object is doing.
// Values go through a MaterialPropertyBlock, so no material is instantiated.
public class MED_UVAnimator : MonoBehaviour
{
    static readonly int UvOffsetId = Shader.PropertyToID("_UVOffset");
    static readonly int EmissionBoostId = Shader.PropertyToID("_EmissionBoost");
    static readonly int AlphaId = Shader.PropertyToID("_Alpha");

    [SerializeField] Renderer target;
    [Tooltip("UV units per second at multiplier 1.")]
    [SerializeField] Vector2 uvSpeed = new Vector2(0f, 0.5f);
    [SerializeField] float speedMultiplier = 1f;
    [Tooltip("How fast the current multiplier follows the requested one (per second). 0 = jump instantly.")]
    [SerializeField] float multiplierResponse = 6f;
    [Header("Optional tint intensity (drives _EmissionBoost) and alpha")]
    [SerializeField] float baseEmissionBoost = 1f;
    [SerializeField] float tintIntensity = 1f;
    [SerializeField, Range(0f, 1f)] float alpha = 1f;

    MaterialPropertyBlock block;
    Vector2 offset;
    float currentMultiplier;
    float targetMultiplier;
    bool dirty = true;

    public float SpeedMultiplier => targetMultiplier;

    void Awake()
    {
        if (target == null) target = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        targetMultiplier = speedMultiplier;
        currentMultiplier = speedMultiplier;
    }

    void OnEnable() => dirty = true;

    /// <summary>Requests a new speed multiplier; the animator eases toward it so the scroll never jumps.</summary>
    public void SetSpeedMultiplier(float value) => targetMultiplier = value;

    public void SetUvSpeed(Vector2 speed) => uvSpeed = speed;

    /// <summary>Brightness of the scrolling texture (multiplies the base emission boost).</summary>
    public void SetTintIntensity(float value)
    {
        if (Mathf.Approximately(tintIntensity, value)) return;
        tintIntensity = value;
        dirty = true;
    }

    public void SetAlpha(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(alpha, value)) return;
        alpha = value;
        dirty = true;
    }

    void Update()
    {
        if (target == null) return;

        float dt = Time.deltaTime;
        currentMultiplier = multiplierResponse > 0f
            ? Mathf.Lerp(currentMultiplier, targetMultiplier, 1f - Mathf.Exp(-multiplierResponse * dt))
            : targetMultiplier;
        if (Mathf.Abs(currentMultiplier - targetMultiplier) < 0.0005f) currentMultiplier = targetMultiplier;

        if (Mathf.Abs(currentMultiplier) > 0.0001f && uvSpeed != Vector2.zero)
        {
            // Euler integration of the UV offset: offset += speed * multiplier * dt.
            offset += uvSpeed * (currentMultiplier * dt);
            // Wrap to [0,1) so the value never grows large enough to lose float precision (the texture repeats anyway).
            offset.x -= Mathf.Floor(offset.x);
            offset.y -= Mathf.Floor(offset.y);
            dirty = true;
        }

        if (!dirty) return;
        dirty = false;
        target.GetPropertyBlock(block);
        block.SetVector(UvOffsetId, new Vector4(offset.x, offset.y, 0f, 0f));
        block.SetFloat(EmissionBoostId, baseEmissionBoost * tintIntensity);
        block.SetFloat(AlphaId, alpha);
        target.SetPropertyBlock(block);
    }
}
