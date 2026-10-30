using UnityEngine;

// Keeps the HUD low in front of the player's head with a slight lag. Not parented to the camera.
// Also reports how strongly the player is looking down (used to fade in the resource panel).
public class MED_HudFollow : MonoBehaviour
{
    [SerializeField] float distance = 1.2f;
    [SerializeField] float heightBelowEyes = 0.5f;
    [SerializeField] float followSpeed = 4f;
    [SerializeField] float showPitch = 25f;   // degrees below horizontal
    [SerializeField] float hidePitch = 20f;   // hysteresis
    [SerializeField] float fadeSpeed = 6f;

    Camera cam;
    DEF_ClinicTraining integratedTraining;
    void Start() { integratedTraining = DEF_ClinicTraining.For(this); }
    Vector3 lastFlatForward = Vector3.forward;
    bool snapped;
    bool lookingDown;

    public float PanelVisibility { get; private set; }
    public float PitchBelowHorizontal { get; private set; }

    void LateUpdate()
    {
        var currentCamera = Camera.main;
        if (integratedTraining != null)
        {
            var input = DEF_VRInput.Instance;
            if (input != null && input.gameObject.scene == gameObject.scene && DEF_VRInput.Active && input.xrCamera != null)
                currentCamera = input.xrCamera;
        }
        if (currentCamera != cam)
        {
            cam = currentCamera;
            snapped = false; // Rebind after desktop-to-XR startup or camera replacement.
        }
        if (cam == null) return;
        var head = cam.transform;
        if (integratedTraining != null)
        {
            // Stable lower-left area HUD, in the actual tracked camera's coordinates.
            transform.SetPositionAndRotation(head.TransformPoint(new Vector3(-.39f, -.30f, 1.15f)), head.rotation);
            PanelVisibility = 1f;
            return;
        }

        var flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (flat.sqrMagnitude > 0.01f) lastFlatForward = flat.normalized;

        var target = head.position + lastFlatForward * distance + Vector3.down * heightBelowEyes;
        if (!snapped)
        {
            transform.position = target;
            snapped = true;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        }

        // Face the player, staying upright.
        var away = Vector3.ProjectOnPlane(transform.position - head.position, Vector3.up);
        if (away.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);

        PitchBelowHorizontal = -Mathf.Asin(Mathf.Clamp(head.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        if (!lookingDown && PitchBelowHorizontal > showPitch) lookingDown = true;
        else if (lookingDown && PitchBelowHorizontal < hidePitch) lookingDown = false;
        PanelVisibility = Mathf.MoveTowards(PanelVisibility, lookingDown ? 1f : 0f, fadeSpeed * Time.deltaTime);
    }
}
