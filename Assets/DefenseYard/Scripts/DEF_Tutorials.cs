using UnityEngine;
using System.Collections.Generic;

public class DEF_Tutorials : MonoBehaviour
{
    public static DEF_Tutorials I;
    static readonly HashSet<string> seen = new HashSet<string>();
    readonly Queue<Hint> pending = new Queue<Hint>();
    public readonly List<string> displayedKeys = new List<string>();
    DEF_SpatialHint active;
    string activeKey;
    struct Hint { public string key, text; public Transform anchor; public Vector3 point; public float created; public bool attached; }
    public static bool IsShowingFor(Transform target) => I != null && I.active != null && I.active.anchor == target;
    public static string CurrentHint => null; // Tutorials now live next to their objects in 3D.
    void Awake() { I = this; seen.Clear(); }
    void OnDestroy() { if (I == this) I = null; }
    public static void Show(string key, Vector3 point, string text) { Queue(key, null, point, text); }
    public static void Show(string key, Transform target, string text) { Queue(key, target, target.position, text); }
    public static string RepairAdvice => DEF_Progression.Current == null ? "Right grip: repair / 10 scrap" : "Release a crafted Workshop RepairKit inside this defense receiver.";
    public static bool Allowed(string key)
    {
        var p = DEF_Progression.Current;
        if (p == null) return true;
        if (p.State.IntroductionActive) return false;
        return key switch {
            "repair" or "repair-turret" or "training-repair" => p.IsAllowed(DEF_Action.Repair),
            "harvest" or "training-crate" => p.IsAllowed(DEF_Action.AmmoCrate),
            "boom" => p.IsAllowed(DEF_Action.Missile),
            "shock" => p.IsAllowed(DEF_Action.Shock),
            "barricade" => p.IsAllowed(DEF_Action.Barricade),
            "tile" or "turret" => p.IsAllowed(DEF_Action.BasicSentry),
            "wave" or "reload" or "radio" => p.IsAllowed(DEF_Action.Combat),
            _ => false
        };
    }
    static void Queue(string key, Transform target, Vector3 point, string text)
    {
        if (I == null || !Allowed(key) || !seen.Add(key)) return;
        if (DEF_Progression.Current != null && (key == "repair" || key == "repair-turret" || key == "training-repair")) text = "PHYSICAL REPAIR\n" + RepairAdvice;
        if (DEF_Progression.Current != null && key == "shock") text = "SHOCK TRAP\nStuns nearby live enemies, then recharges.\n" + RepairAdvice;
        I.pending.Enqueue(new Hint { key = key, text = text, anchor = target, point = point, created = Time.unscaledTime, attached = target != null });
    }
    public static void Dismiss(string key)
    {
        if (I == null) return;
        if (I.activeKey == key && I.active != null) Destroy(I.active.gameObject);
        int count = I.pending.Count;
        for (int i = 0; i < count; i++) { var hint = I.pending.Dequeue(); if (hint.key != key) I.pending.Enqueue(hint); }
    }
    void Update()
    {
        if (active != null && !Allowed(activeKey)) { Destroy(active.gameObject); active = null; }
        if (DEF_HUD.BlockingWorldInput || active != null || pending.Count == 0) return;
        var prefab = Resources.Load<GameObject>("Prefabs/DEF_SpatialHint");
        if (prefab == null) return;
        var hint = pending.Dequeue();
        if (!Allowed(hint.key)) { seen.Remove(hint.key); return; }
        if (Time.unscaledTime - hint.created > 15 || (hint.attached && hint.anchor == null)) return;
        var cam = Camera.main;
        if (cam != null)
        {
            var point = (hint.anchor != null ? hint.anchor.position : hint.point) + Vector3.up * 1.8f;
            var viewport = cam.WorldToViewportPoint(point);
            if (viewport.z <= 0 || viewport.x < 0.05f || viewport.x > 0.95f || viewport.y < 0.12f || viewport.y > 0.88f)
            { pending.Enqueue(hint); return; }
        }
        active = Instantiate(prefab).GetComponent<DEF_SpatialHint>();
        activeKey = hint.key;
        active.Show(hint.anchor, hint.point, hint.text);
        displayedKeys.Add(hint.key);
    }
}
