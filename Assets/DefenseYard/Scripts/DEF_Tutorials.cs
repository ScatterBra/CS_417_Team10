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
    static void Queue(string key, Transform target, Vector3 point, string text)
    {
        if (I == null || !seen.Add(key)) return;
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
        if (DEF_HUD.BlockingWorldInput || active != null || pending.Count == 0) return;
        var prefab = Resources.Load<GameObject>("Prefabs/DEF_SpatialHint");
        if (prefab == null) return;
        var hint = pending.Dequeue();
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
