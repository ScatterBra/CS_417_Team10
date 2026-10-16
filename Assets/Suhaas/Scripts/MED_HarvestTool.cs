using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// A grabbable tool. Harvests whatever its hit point touches when it is swung fast enough OR the
// activate (trigger) button is held while touching - so it works in the device simulator and a headset.
[RequireComponent(typeof(XRGrabInteractable))]
public class MED_HarvestTool : MonoBehaviour
{
    [SerializeField] MED_ToolType toolType;
    [SerializeField] Transform hitPoint;
    [SerializeField] float hitRadius = 0.15f;
    [SerializeField] float swingSpeedThreshold = 0.5f;
    [SerializeField] float ignoreAfterGrab = 0.25f;
    [SerializeField] float returnDelay = 3f;
    [SerializeField] float returnSpeed = 3f;

    XRGrabInteractable grab;
    IXRSelectInteractor holder;
    MED_PlayerResources player;
    Transform home;
    Vector3 lastPos;
    float grabTime;
    float idleTimer;
    bool activateHeld;
    readonly Collider[] hits = new Collider[16];
    readonly HashSet<MED_IToolTarget> touched = new HashSet<MED_IToolTarget>();

    public MED_ToolType ToolType => toolType;
    public bool IsHeld => holder != null;
    public float SwingSpeed { get; private set; }

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(args => { holder = args.interactorObject; grabTime = Time.time; lastPos = transform.position; });
        grab.selectExited.AddListener(args => { holder = null; idleTimer = 0f; activateHeld = false; });
        grab.activated.AddListener(_ => activateHeld = true);
        grab.deactivated.AddListener(_ => activateHeld = false);
    }

    /// <summary>The rack slot this tool eases back to when left alone.</summary>
    public void SetHome(Transform slot) => home = slot;

    void Update()
    {
        if (!IsHeld)
        {
            ReturnHomeWhenIdle();
            lastPos = transform.position;
            return;
        }

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        SwingSpeed = (transform.position - lastPos).magnitude / dt;
        lastPos = transform.position;
        if (Time.time - grabTime < ignoreAfterGrab) return;

        bool swinging = SwingSpeed > swingSpeedThreshold;
        if (!swinging && !activateHeld) return;

        FindTouched();
        if (touched.Count == 0) return;
        if (player == null) player = FindAnyObjectByType<MED_PlayerResources>();
        foreach (var h in touched)
            if (h != null && !(h is Object o && o == null)) h.TryHarvest(this, player);
    }

    void FindTouched()
    {
        touched.Clear();
        var p = hitPoint != null ? hitPoint.position : transform.position;
        int n = Physics.OverlapSphereNonAlloc(p, hitRadius, hits, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var h = hits[i].GetComponentInParent<MED_IToolTarget>();
            if (h != null) touched.Add(h);
        }
    }

    void ReturnHomeWhenIdle()
    {
        if (home == null) return;
        bool atHome = (transform.position - home.position).sqrMagnitude < 0.0001f &&
                      Quaternion.Angle(transform.rotation, home.rotation) < 0.5f;
        if (atHome) { idleTimer = 0f; return; }
        idleTimer += Time.deltaTime;
        if (idleTimer < returnDelay) return;

        float k = 1f - Mathf.Exp(-returnSpeed * Time.deltaTime);
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, home.position, k),
            Quaternion.Slerp(transform.rotation, home.rotation, k));
        if ((transform.position - home.position).sqrMagnitude < 0.0001f)
            transform.SetPositionAndRotation(home.position, home.rotation);
    }

    public void SendHaptic(float amplitude, float duration)
    {
        if (holder is XRBaseInputInteractor c) c.SendHapticImpulse(amplitude, duration);
    }
}
