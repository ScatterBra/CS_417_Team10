using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Shows spatial pop-up tutorials one at a time. Pop-ups (MED_PopupTutorial) ask for a slot; this manager keeps a
// queue, remembers which tutorial ids were already shown this session, and draws a single reusable world-space
// panel (dark rounded rectangle, "i" icon, title, body and a thin pointer line to the object).
public class MED_TutorialManager : MonoBehaviour
{
    struct Request
    {
        public MED_PopupTutorial popup;
        public Transform anchor;
    }

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    static MED_TutorialManager instance;
    static readonly HashSet<string> shown = new HashSet<string>();

    [SerializeField] Material panelMaterial;
    [SerializeField] float showSeconds = 8f;
    [SerializeField] float minSeconds = 3f;
    [SerializeField] float hideDistance = 6f;
    [SerializeField] float hideViewAngle = 95f;
    [SerializeField] float fadeSeconds = 0.4f;
    [SerializeField] float towardPlayer = 0.5f;
    [SerializeField] float belowEye = 0.35f;
    [SerializeField] float aboveEye = 0.4f;

    readonly Queue<Request> queue = new Queue<Request>();
    readonly HashSet<string> queuedIds = new HashSet<string>();

    GameObject panel;
    Renderer backgroundRenderer, iconRenderer;
    TextMeshPro iconText, titleText, bodyText;
    LineRenderer line;
    Material backgroundMat, iconMat, lineMat;

    bool active;
    MED_PopupTutorial current;
    Transform currentAnchor;
    float elapsed;
    float alpha;
    bool fadingOut;

    public static bool IsShown(string id) => shown.Contains(id);
    public bool IsBusy => active;
    public Material PanelMaterial => panelMaterial;

    public static MED_TutorialManager Instance
    {
        get
        {
            if (instance == null) instance = FindAnyObjectByType<MED_TutorialManager>();
            if (instance == null)
            {
                var go = new GameObject("MED_TutorialManager");
                instance = go.AddComponent<MED_TutorialManager>();
            }
            return instance;
        }
    }

    /// <summary>Clears the "already shown" set so every tutorial can appear again (used by the RESET TUTORIALS test button).</summary>
    public static void ReArmAll()
    {
        shown.Clear();
        if (instance != null)
        {
            instance.queue.Clear();
            instance.queuedIds.Clear();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        shown.Clear();
        instance = null;
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (backgroundMat != null) Destroy(backgroundMat);
        if (iconMat != null) Destroy(iconMat);
        if (lineMat != null) Destroy(lineMat);
    }

    /// <summary>Queues a pop-up. Ignored if its id was already shown or is already waiting.</summary>
    public void Enqueue(MED_PopupTutorial popup, Transform anchor)
    {
        if (popup == null || shown.Contains(popup.TutorialId) || queuedIds.Contains(popup.TutorialId)) return;
        if (active && current != null && current.TutorialId == popup.TutorialId) return;
        queuedIds.Add(popup.TutorialId);
        queue.Enqueue(new Request { popup = popup, anchor = anchor });
    }

    void Update()
    {
        if (!active)
        {
            if (MED_AreaGate.Outside) return;   // the player is in another area: hold the queue
            TryStartNext();
            return;
        }
        UpdateActive(Time.deltaTime);
    }

    void TryStartNext()
    {
        while (queue.Count > 0)
        {
            var r = queue.Dequeue();
            if (r.popup == null) continue;
            queuedIds.Remove(r.popup.TutorialId);
            if (shown.Contains(r.popup.TutorialId)) continue;
            // A proximity pop-up that waited too long is dropped if the player has moved off; it can fire again later.
            if (!r.popup.StillRelevant(r.anchor)) continue;
            Show(r.popup, r.anchor);
            return;
        }
    }

    void Show(MED_PopupTutorial popup, Transform anchor)
    {
        BuildPanel();
        current = popup;
        currentAnchor = anchor != null ? anchor : popup.Anchor;
        elapsed = 0f;
        alpha = 0f;
        fadingOut = false;
        active = true;
        shown.Add(popup.TutorialId);   // marked shown the moment it appears

        titleText.text = popup.Title;
        bodyText.text = popup.Body;
        panel.transform.localScale = Vector3.one * popup.PanelScale;
        panel.SetActive(true);
        Place();
        ApplyAlpha();
    }

    void UpdateActive(float dt)
    {
        if (current == null || currentAnchor == null) { fadingOut = true; }
        elapsed += dt;

        if (!fadingOut)
        {
            if (elapsed >= showSeconds) fadingOut = true;
            else if (elapsed >= minSeconds && PlayerMovedAway()) fadingOut = true;
        }

        alpha = Mathf.MoveTowards(alpha, fadingOut ? 0f : 1f, dt / Mathf.Max(0.01f, fadeSeconds));
        if (fadingOut && alpha <= 0f)
        {
            active = false;
            current = null;
            currentAnchor = null;
            panel.SetActive(false);
            return;
        }
        Place();
        ApplyAlpha();
    }

    bool PlayerMovedAway()
    {
        var cam = Camera.main;
        if (cam == null || current == null) return false;
        var target = current.PointTarget(currentAnchor);
        var to = target - cam.transform.position;
        if (to.magnitude > hideDistance) return true;
        return Vector3.Angle(cam.transform.forward, to) > hideViewAngle;
    }

    // ---------- Panel ----------

    void Place()
    {
        var cam = Camera.main;
        if (cam == null || current == null || currentAnchor == null) return;
        var camT = cam.transform;
        var anchorPos = currentAnchor.position;

        // The offset is "up" in world space and "side" along the player's right, so the panel stays beside the object.
        var right = camT.right; right.y = 0f;
        right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
        var off = current.Offset;
        var pos = anchorPos + Vector3.up * off.y + right * off.x;
        // Float the panel toward the player so it never sinks into the wall or shelf the object stands against.
        var toPlayer = camT.position - pos;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.0001f) pos += toPlayer.normalized * towardPlayer;
        // Keep the panel in a comfortable band around eye level so it is never cropped at the top of the view.
        pos.y = Mathf.Clamp(pos.y, camT.position.y - belowEye, camT.position.y + aboveEye);
        panel.transform.position = pos;
        panel.transform.rotation = Quaternion.LookRotation(pos - camT.position, camT.up);

        float halfHeight = 0.17f * current.PanelScale;
        line.SetPosition(0, pos + Vector3.down * halfHeight);
        line.SetPosition(1, current.PointTarget(currentAnchor));
    }

    void ApplyAlpha()
    {
        SetColorAlpha(backgroundMat, new Color(0.05f, 0.08f, 0.1f), 0.88f * alpha);
        SetColorAlpha(iconMat, new Color(0.35f, 0.75f, 1f), alpha);
        SetColorAlpha(lineMat, new Color(0.6f, 0.85f, 1f), 0.8f * alpha);
        titleText.alpha = alpha;
        bodyText.alpha = alpha;
        iconText.alpha = alpha;
    }

    static void SetColorAlpha(Material m, Color c, float a)
    {
        c.a = a;
        m.SetColor(BaseColorId, c);
    }

    void BuildPanel()
    {
        if (panel != null) return;

        var shader = panelMaterial != null ? panelMaterial.shader : Shader.Find("Universal Render Pipeline/Unlit");
        backgroundMat = NewMaterial(shader, MakeRoundedTexture(256, 88, 22));
        iconMat = NewMaterial(shader, MakeRoundedTexture(64, 64, 32));
        lineMat = NewMaterial(shader, null);
        backgroundMat.renderQueue = 2990;   // drawn before the text (3000)
        iconMat.renderQueue = 2995;
        lineMat.renderQueue = 2990;

        panel = new GameObject("MED_TutorialPanel");
        panel.transform.SetParent(transform, false);

        backgroundRenderer = MakeQuad("Background", new Vector3(0f, 0f, 0f), new Vector3(1f, 0.34f, 1f), backgroundMat);
        iconRenderer = MakeQuad("Icon", new Vector3(-0.42f, 0f, -0.002f), new Vector3(0.13f, 0.13f, 1f), iconMat);

        iconText = MakeText("IconText", new Vector3(-0.42f, 0.002f, -0.006f), new Vector2(0.13f, 0.13f), 0.95f, FontStyles.Bold | FontStyles.Italic, TextAlignmentOptions.Center);
        iconText.text = "i";
        iconText.color = new Color(0.04f, 0.1f, 0.15f);

        titleText = MakeText("Title", new Vector3(0.08f, 0.075f, -0.004f), new Vector2(0.78f, 0.1f), 0.62f, FontStyles.Bold, TextAlignmentOptions.Left);
        titleText.color = new Color(0.65f, 0.95f, 0.7f);
        bodyText = MakeText("Body", new Vector3(0.08f, -0.045f, -0.004f), new Vector2(0.78f, 0.17f), 0.43f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        bodyText.color = new Color(0.92f, 0.94f, 0.96f);

        var lineGo = new GameObject("PointerLine");
        lineGo.transform.SetParent(panel.transform, false);
        line = lineGo.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = 0.008f;
        line.endWidth = 0.003f;
        line.sharedMaterial = lineMat;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        panel.SetActive(false);
    }

    Material NewMaterial(Shader shader, Texture2D tex)
    {
        var m = panelMaterial != null ? new Material(panelMaterial) : new Material(shader);
        if (tex != null) m.SetTexture(BaseMapId, tex);
        else m.SetTexture(BaseMapId, Texture2D.whiteTexture);
        return m;
    }

    Renderer MakeQuad(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(q.GetComponent<Collider>());
        q.name = name;
        q.transform.SetParent(panel.transform, false);
        q.transform.localPosition = pos;
        q.transform.localScale = scale;
        var r = q.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return r;
    }

    TextMeshPro MakeText(string name, Vector3 pos, Vector2 size, float fontSize, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(panel.transform, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        t.rectTransform.sizeDelta = size;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    public static Texture2D MakeRoundedTexture(int w, int h, float radius)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "MED_TutorialRounded" };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Signed distance to a rounded rectangle, anti-aliased over one pixel.
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - w * 0.5f) - (w * 0.5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - h * 0.5f) - (h * 0.5f - radius), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255f);
                px[y * w + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false);
        return tex;
    }
}
