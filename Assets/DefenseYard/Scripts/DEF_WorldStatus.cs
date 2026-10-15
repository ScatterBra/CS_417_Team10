using UnityEngine;
using UnityEngine.UI;

// Serialized under each gameplay prefab. The small canvas never accepts ray input.
public class DEF_WorldStatus : MonoBehaviour
{
    public CanvasGroup group;
    public Image healthFill;
    public Image damageTrail;
    public Text caption;
    public float height = 2f;
    public float worldScale = .01f;
    public float maxDistance = 40f;
    DEF_Zombie zombie;
    DEF_AmmoCrate crate;
    Transform subject;
    float previousRatio = 1f;
    float trailRatio = 1f;
    float damageHold;
    float nextVisibilityCheck;
    bool visible = true;
    readonly RaycastHit[] hits = new RaycastHit[24];
    static readonly Color Healthy = new Color(.35f, .84f, .72f);
    static readonly Color Worn = new Color(1f, .68f, .24f);
    static readonly Color Critical = new Color(1f, .28f, .22f);

    void Awake()
    {
        zombie = GetComponentInParent<DEF_Zombie>();
        crate = GetComponentInParent<DEF_AmmoCrate>();
        subject = transform.parent;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    void LateUpdate()
    {
        var camera = DEF_VRInput.Instance != null ? DEF_VRInput.Instance.xrCamera : Camera.main;
        if (camera == null || subject == null) return;
        transform.position = subject.position + Vector3.up * height;
        transform.rotation = camera.transform.rotation;
        // Compensate for a shrinking crate so its refill label keeps a stable size.
        Vector3 parentScale = subject.lossyScale;
        float distance = Vector3.Distance(transform.position, camera.transform.position);
        float size = worldScale * Mathf.Clamp(distance / 12f, 1f, 1.6f);
        transform.localScale = new Vector3(size / parentScale.x, size / parentScale.y, size / parentScale.z);
        if (Time.unscaledTime >= nextVisibilityCheck)
        {
            nextVisibilityCheck = Time.unscaledTime + .15f;
            var viewport = camera.WorldToViewportPoint(transform.position);
            visible = distance <= maxDistance && viewport.z > 0 && viewport.x > -.05f && viewport.x < 1.05f && viewport.y > -.05f && viewport.y < 1.05f;
            if (visible)
            {
                Vector3 direction = transform.position - camera.transform.position;
                int count = Physics.RaycastNonAlloc(camera.transform.position, direction.normalized, hits, distance, ~(1 << 2), QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var hit = hits[i].collider;
                    if (hit.transform.IsChildOf(subject) || hit.GetComponentInParent<DEF_PCXRPlayer>() != null || hit.GetComponentInParent<DEF_Zombie>() != null) continue;
                    visible = false; break;
                }
            }
        }
        group.alpha = visible && !DEF_Tutorials.IsShowingFor(subject) ? 1f : 0f;
        if (zombie != null)
        {
            float ratio = Mathf.Clamp01(zombie.health / Mathf.Max(1f, zombie.maxHealth));
            healthFill.fillAmount = ratio;
            healthFill.color = Color.Lerp(Critical, new Color(.9f, .39f, .34f), ratio);
            if (ratio < previousRatio) damageHold = Time.time + .3f;
            if (ratio > trailRatio) trailRatio = ratio;
            if (Time.time > damageHold) trailRatio = Mathf.MoveTowards(trailRatio, ratio, Time.deltaTime * .8f);
            damageTrail.fillAmount = trailRatio;
            previousRatio = ratio;
        }
        else if (crate != null)
        {
            caption.text = crate.IsRefilling ? "REFILL " + Mathf.CeilToInt(crate.RefillRemaining) + "s" : "AMMO  " + crate.supply + " / " + crate.maxSupply;
            caption.color = crate.IsRefilling ? Worn : Healthy;
            if (healthFill != null) { healthFill.fillAmount = crate.IsRefilling ? 1f - crate.RefillRemaining / crate.refillSeconds : (float)crate.supply / crate.maxSupply; healthFill.color = caption.color; }
        }
    }
}
