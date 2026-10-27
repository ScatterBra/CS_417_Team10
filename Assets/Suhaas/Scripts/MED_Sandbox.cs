using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Sandboxes: after each threshold a safe showcase lets the player try the new idea with no stakes. Feedback (text +
// glow) appears only when the showcase reaches its own target state, which is never the threshold's own condition.
//   T1 MED_MedicineTray      - place a Medicine on a spare tray: pulses green, "Well made!"
//   T2 (in MED_TreatmentStation) - the pad glows when you stand on it with stock present
//   T3 MED_SterilizerShowcase - an instrument heated into the target band: "Perfectly sterile"
//   T4 MED_PlanterDemo       - a demo plant ripens; harvesting it says "Fresh herbs!"
public static class MED_SandboxUtil
{
    public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public static Material MakeMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var m = new Material(shader);
        m.SetColor(BaseColorId, color);
        return m;
    }

    public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Color color, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color);
        if (!keepCollider) Object.Destroy(go.GetComponent<Collider>());
        return go;
    }

    public static void Tint(Renderer r, MaterialPropertyBlock block, Color c)
    {
        r.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        r.SetPropertyBlock(block);
    }

    public static TMPro.TextMeshPro Label(Transform parent, string text, Vector3 localPos, float size)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var t = go.AddComponent<TMPro.TextMeshPro>();
        t.text = text;
        t.fontSize = size;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        t.color = new Color(0.95f, 0.95f, 0.95f);
        t.rectTransform.sizeDelta = new Vector2(1.6f, 0.4f);
        go.AddComponent<MED_Billboard>();
        return t;
    }
}

// ---------- T1: spare tray ----------

public class MED_MedicineTray : MonoBehaviour
{
    static readonly Color Idle = new Color(0.82f, 0.84f, 0.86f);
    static readonly Color Good = new Color(0.35f, 1f, 0.5f);

    Renderer trayRenderer;
    MaterialPropertyBlock block;
    Coroutine pulse;
    readonly List<GameObject> bottles = new List<GameObject>();
    int nextSlot;

    public static GameObject Create(Vector3 position)
    {
        var root = new GameObject("MED_MedicineTray");
        root.transform.position = position;
        MED_SandboxUtil.Prim(PrimitiveType.Cube, "Pedestal", root.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.6f, 0.7f, 0.5f), new Color(0.45f, 0.32f, 0.2f), false);
        var tray = MED_SandboxUtil.Prim(PrimitiveType.Cube, "Tray", root.transform, new Vector3(0f, 0.725f, 0f), new Vector3(0.56f, 0.05f, 0.42f), Idle, true);
        MED_SandboxUtil.Label(root.transform, "SPARE TRAY\nplace a Medicine here", new Vector3(0f, 1.15f, 0f), 0.3f);
        var t = root.AddComponent<MED_MedicineTray>();
        t.trayRenderer = tray.GetComponent<Renderer>();
        tray.AddComponent<XRSimpleInteractable>().selectEntered.AddListener(_ => t.OnSelect());
        return root;
    }

    void Awake() => block = new MaterialPropertyBlock();

    void OnSelect()
    {
        if (MED_AreaGate.Outside) return;
        Vector3 top = transform.position + Vector3.up * 1.0f;
        var p = FindAnyObjectByType<MED_PlayerResources>();
        if (p == null || p.Get(MED_ResourceType.Medicine) < 1)
        {
            MED_Messages.Fail(top, MED_Messages.TrayEmpty);
            return;
        }
        PlaceBottle();
        if (pulse != null) StopCoroutine(pulse);
        pulse = StartCoroutine(Pulse());
        MED_FloatingText.Spawn(top, "Well made!", new Color(0.6f, 1f, 0.6f));
    }

    void PlaceBottle()
    {
        const int slots = 4;
        float x = -0.2f + 0.133f * (nextSlot % slots);
        if (bottles.Count < slots)
        {
            var b = MED_SandboxUtil.Prim(PrimitiveType.Cylinder, "Bottle", transform, new Vector3(x, 0.82f, 0f), new Vector3(0.07f, 0.07f, 0.07f), new Color(0.3f, 0.8f, 0.5f), false);
            bottles.Add(b);
        }
        nextSlot = (nextSlot + 1) % slots;
    }

    IEnumerator Pulse()
    {
        const float duration = 0.9f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            MED_SandboxUtil.Tint(trayRenderer, block, Color.Lerp(Idle, Good, Mathf.Sin(t / duration * Mathf.PI)));
            yield return null;
        }
        MED_SandboxUtil.Tint(trayRenderer, block, Idle);
        pulse = null;
    }
}

// ---------- T3: sterilizer showcase ----------

public class MED_SterilizerShowcase : MonoBehaviour
{
    const float BandMin = 0.55f, BandMax = 0.85f;
    static readonly Color Cold = new Color(0.55f, 0.57f, 0.6f);
    static readonly Color Sterile = new Color(0.4f, 0.9f, 1f);

    MED_Sterilizer sterilizer;
    Renderer instrument;
    MaterialPropertyBlock block;
    bool inBand, announced;

    public static void Attach(MED_Sterilizer s)
    {
        if (s.transform.Find("MED_Showcase") != null) return;
        var root = new GameObject("MED_Showcase");
        root.transform.SetParent(s.transform, false);
        var inst = MED_SandboxUtil.Prim(PrimitiveType.Capsule, "Instrument", root.transform, new Vector3(-0.42f, 0.55f, 0f), new Vector3(0.05f, 0.14f, 0.05f), Cold, true);
        var show = root.AddComponent<MED_SterilizerShowcase>();
        show.sterilizer = s;
        show.instrument = inst.GetComponent<Renderer>();
        inst.AddComponent<XRSimpleInteractable>().selectEntered.AddListener(_ => show.OnSelect());
    }

    void Awake() => block = new MaterialPropertyBlock();

    Vector3 TextPosition => transform.position + Vector3.up * 1.3f;

    void Update()
    {
        bool active = MED_ProgressionManager.SandboxActive(3);
        if (instrument.gameObject.activeSelf != active) instrument.gameObject.SetActive(active);
        if (!active || sterilizer == null) return;

        float h = sterilizer.Heat.Value;
        if (h < 0.1f) announced = false;   // a new cycle may announce again
        bool now = h >= BandMin && h <= BandMax;
        if (now == inBand) return;
        inBand = now;
        MED_SandboxUtil.Tint(instrument, block, inBand ? Sterile : Cold);
        if (inBand && !announced)
        {
            announced = true;
            MED_FloatingText.Spawn(TextPosition, "Perfectly sterile", new Color(0.5f, 0.95f, 1f));
        }
    }

    void OnSelect()
    {
        if (MED_AreaGate.Outside || sterilizer == null) return;
        float h = sterilizer.Heat.Value;
        if (h < BandMin) MED_Messages.Fail(TextPosition, MED_Messages.InstrumentTooCold);
        else if (h > BandMax) MED_Messages.Fail(TextPosition, MED_Messages.InstrumentTooHot);
        else MED_FloatingText.Spawn(TextPosition, "Perfectly sterile", new Color(0.5f, 0.95f, 1f));
    }
}

// ---------- T4: planter demo plant ----------

public class MED_PlanterDemo : MonoBehaviour, MED_IToolTarget
{
    [SerializeField] float ripenSeconds = 6f;
    [SerializeField] float regrowDelay = 4f;

    static readonly Color Young = new Color(0.25f, 0.55f, 0.25f);
    static readonly Color Ripe = new Color(0.9f, 0.95f, 0.3f);

    Renderer leaf;
    Transform leafT;
    MaterialPropertyBlock block;
    float growth;
    float regrowAt = -1f;
    float nextMessageTime;

    public static void Attach(MED_HerbPlanter p)
    {
        if (p.transform.Find("MED_DemoPlant") != null) return;
        var root = new GameObject("MED_DemoPlant");
        root.transform.SetParent(p.transform, false);
        root.transform.localPosition = new Vector3(0f, 0.55f, 0.05f);
        MED_SandboxUtil.Prim(PrimitiveType.Cylinder, "Stand", root.transform, new Vector3(0f, -0.15f, 0f), new Vector3(0.03f, 0.15f, 0.03f), new Color(0.4f, 0.3f, 0.2f), false);
        MED_SandboxUtil.Prim(PrimitiveType.Cylinder, "Pot", root.transform, Vector3.zero, new Vector3(0.18f, 0.04f, 0.18f), new Color(0.65f, 0.35f, 0.25f), false);
        var l = MED_SandboxUtil.Prim(PrimitiveType.Sphere, "Leaf", root.transform, new Vector3(0f, 0.06f, 0f), Vector3.one * 0.04f, Young, false);
        var col = root.AddComponent<SphereCollider>();   // sickle target
        col.isTrigger = true;
        col.center = new Vector3(0f, 0.05f, 0f);
        col.radius = 0.12f;
        var demo = root.AddComponent<MED_PlanterDemo>();
        demo.leaf = l.GetComponent<Renderer>();
        demo.leafT = l.transform;
    }

    void Awake() => block = new MaterialPropertyBlock();

    void Update()
    {
        bool active = MED_ProgressionManager.SandboxActive(4);
        if (leafT.gameObject.activeSelf != active) leafT.gameObject.SetActive(active);
        if (!active) return;

        if (regrowAt >= 0f)
        {
            if (Time.time < regrowAt) return;
            regrowAt = -1f;
            growth = 0f;
        }
        if (growth < 1f) growth = Mathf.Min(1f, growth + Time.deltaTime / ripenSeconds);   // Euler step, like the real planter
        leafT.localScale = Vector3.one * Mathf.Lerp(0.04f, 0.13f, growth);
        Color c = Color.Lerp(Young, Ripe, growth);
        if (growth >= 1f) c = Color.Lerp(Ripe, Color.white, 0.25f + 0.25f * Mathf.Sin(Time.time * 5f));   // ripe glow
        MED_SandboxUtil.Tint(leaf, block, c);
    }

    public bool TryHarvest(MED_HarvestTool tool, MED_PlayerResources player)
    {
        if (MED_AreaGate.Outside || !MED_ProgressionManager.SandboxActive(4) || regrowAt >= 0f) return false;
        Vector3 pos = transform.position + Vector3.up * 0.4f;
        bool wrongTool = tool == null || tool.ToolType != MED_ToolType.Sickle;
        if (wrongTool || growth < 1f)
        {
            // The tool calls this every frame it touches; explain at most every 1.5 s.
            if (Time.time < nextMessageTime) return false;
            nextMessageTime = Time.time + 1.5f;
            return MED_Messages.Fail(pos, wrongTool ? MED_Messages.DemoWrongTool : MED_Messages.DemoGrowing);
        }
        MED_FloatingText.Spawn(pos, "Fresh herbs!", new Color(0.6f, 1f, 0.6f));
        tool.SendHaptic(0.5f, 0.1f);
        leafT.localScale = Vector3.one * 0.04f;
        growth = 0f;
        regrowAt = Time.time + regrowDelay;
        return true;
    }
}
