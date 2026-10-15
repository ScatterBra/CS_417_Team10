using System.Collections.Generic;
using UnityEngine;

public enum MED_TutorialTrigger { Proximity, OnEvent, TimeAfterStart }

// Marks a mechanic with a pop-up tutorial. The id is shared by every instance of the same kind of object, so a second
// herb patch or a second planter never repeats the tutorial. The MED_TutorialManager shows at most one at a time.
public class MED_PopupTutorial : MonoBehaviour
{
    static readonly List<MED_PopupTutorial> registry = new List<MED_PopupTutorial>();

    [SerializeField] string tutorialId;
    [SerializeField] string title;
    [SerializeField, TextArea(1, 2)] string body;
    [SerializeField] Transform anchor;
    [Tooltip("x = sideways (along the player's right), y = up, relative to the anchor.")]
    [SerializeField] Vector3 offset = new Vector3(0.5f, 0.9f, 0f);
    [Tooltip("The pointer line ends this far above the anchor.")]
    [SerializeField] float pointHeight = 0.3f;
    [SerializeField] MED_TutorialTrigger trigger = MED_TutorialTrigger.Proximity;
    [SerializeField] float proximityDistance = 3f;
    [SerializeField] float viewConeAngle = 55f;
    [SerializeField] float delaySeconds = 6f;
    [SerializeField] float panelScale = 1f;

    public string TutorialId => tutorialId;
    public string Title => title;
    public string Body => body;
    public Transform Anchor => anchor != null ? anchor : transform;
    public Vector3 Offset => offset;
    public float PanelScale => panelScale;
    public MED_TutorialTrigger TriggerType => trigger;

    void OnEnable() => registry.Add(this);
    void OnDisable() => registry.Remove(this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => registry.Clear();

    public Vector3 PointTarget(Transform a) => (a != null ? a : Anchor).position + Vector3.up * pointHeight;

    void Update()
    {
        if (MED_TutorialManager.IsShown(tutorialId)) return;
        switch (trigger)
        {
            case MED_TutorialTrigger.Proximity:
                if (IsPlayerNear(Anchor, proximityDistance) && IsInViewCone(Anchor))
                    MED_TutorialManager.Instance.Enqueue(this, Anchor);
                break;
            case MED_TutorialTrigger.TimeAfterStart:
                if (Time.timeSinceLevelLoad >= delaySeconds)
                    MED_TutorialManager.Instance.Enqueue(this, Anchor);
                break;
        }
    }

    /// <summary>OnEvent trigger: called from code. The anchor can be overridden (e.g. the grabbed model).</summary>
    public void Fire(Transform anchorOverride = null)
    {
        if (!isActiveAndEnabled || MED_TutorialManager.IsShown(tutorialId)) return;
        MED_TutorialManager.Instance.Enqueue(this, anchorOverride != null ? anchorOverride : Anchor);
    }

    /// <summary>Static helper for code that only knows the id (e.g. grabbing a shelf model fires "grid").</summary>
    public static void FireById(string id, Transform anchorOverride = null)
    {
        foreach (var p in registry)
            if (p != null && p.tutorialId == id) { p.Fire(anchorOverride); return; }
    }

    /// <summary>Checked when a queued pop-up gets its turn: proximity pop-ups are dropped if the player has left.</summary>
    public bool StillRelevant(Transform a)
    {
        if (trigger != MED_TutorialTrigger.Proximity) return true;
        return a != null && IsPlayerNear(a, proximityDistance * 1.5f);
    }

    static bool IsPlayerNear(Transform t, float distance) => MED_PlayerProximity.IsNear(t.position, distance);

    bool IsInViewCone(Transform t)
    {
        var cam = Camera.main;
        if (cam == null) return false;
        var to = PointTarget(t) - cam.transform.position;
        return Vector3.Angle(cam.transform.forward, to) <= viewConeAngle;
    }
}
