using System;
using System.Text;
using TMPro;
using UnityEngine;

public class MED_PlayerHUD : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static MED_PlayerHUD instance;

    [SerializeField] MED_HudFollow follow;
    [SerializeField] MED_StatBar healthBar;
    [SerializeField] TextMeshPro healthLabel;
    [SerializeField] TextMeshPro messageLabel;
    [SerializeField] GameObject resourcePanel;
    [SerializeField] TextMeshPro leftColumn;
    [SerializeField] TextMeshPro rightColumn;
    [SerializeField] Renderer panelBackdrop;
    [SerializeField, Range(0f, 1f)] float panelBackdropAlpha = 0.55f;

    MED_PlayerResources resources;
    TextMeshPro goalLabel;
    DEF_ClinicTraining integratedTraining;
    Material goalBackdropMaterial;
    string displayedInstruction;
    bool displayedIntroduction;
    MaterialPropertyBlock backdropBlock;
    float lastVisibility = -1f;
    float messageTimer;

    /// <summary>Shows a short message under the health bar. Safe to call when no HUD exists.</summary>
    public static void ShowMessage(string text, float seconds = 2f)
    {
        if (instance == null) instance = FindAnyObjectByType<MED_PlayerHUD>();
        if (instance != null) instance.ShowMessageInternal(text, seconds);
        else Debug.Log("[HUD message] " + text);
    }

    void ShowMessageInternal(string text, float seconds)
    {
        if (messageLabel == null) return;
        messageLabel.text = text;
        messageLabel.alpha = 1f;
        messageTimer = seconds;
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        backdropBlock = new MaterialPropertyBlock();
        if (messageLabel != null) { messageLabel.text = ""; messageLabel.alpha = 0f; }
        TryBind();
        SetPanelVisibility(0f);
        integratedTraining = DEF_ClinicTraining.For(this);
        BuildGoalLabel();
        MED_ProgressionManager.GoalChanged += RefreshGoal;
        MED_AreaGate.Changed += OnAreaChanged;
        RefreshGoal();
        OnAreaChanged(MED_AreaGate.PlayerInside);
    }

    // NEXT GOAL line (Thresholds on the Horizon), shown under the health bar next to the in-world sign.
    void BuildGoalLabel()
    {
        var go = new GameObject("MED_GoalLabel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        goalLabel = go.AddComponent<TextMeshPro>();
        goalLabel.fontSize = 0.2f;
        goalLabel.alignment = TextAlignmentOptions.Center;
        goalLabel.textWrappingMode = TextWrappingModes.NoWrap;
        goalLabel.color = new Color(0.6f, 0.85f, 1f);
        goalLabel.rectTransform.sizeDelta = new Vector2(0.7f, 0.05f);
        if (integratedTraining != null)
        {
            // Upper peripheral card: keep the forward work area, health and inventory clear.
            go.transform.localPosition = new Vector3(0f, 0.85f, -0.01f);
            goalLabel.rectTransform.sizeDelta = new Vector2(1.05f, 0.4f);
            goalLabel.fontSize = 0.24f;
            goalLabel.textWrappingMode = TextWrappingModes.Normal;
            goalLabel.raycastTarget = false;
            var backing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backing.name = "MED_IntegratedGoalBackdrop";
            backing.transform.SetParent(transform, false);
            backing.transform.localPosition = new Vector3(0f, 0.85f, 0.005f);
            backing.transform.localScale = new Vector3(1.1f, 0.44f, 1f);
            var collider = backing.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            goalBackdropMaterial = MED_SandboxUtil.MakeMaterial(new Color(0.025f, 0.045f, 0.06f));
            backing.GetComponent<Renderer>().sharedMaterial = goalBackdropMaterial;
        }
    }

    void RefreshGoal()
    {
        if (goalLabel == null) return;
        if (integratedTraining != null)
        {
            displayedInstruction = integratedTraining.CurrentInstruction;
            displayedIntroduction = integratedTraining.IsIntroducing;
            goalLabel.text = (displayedIntroduction ? "CLINIC — READ FIRST (unlock follows)\n" : "CLINIC — NEXT\n") +
                displayedInstruction + "\nPhysical buttons: GRIP. Inventory: look down over 25°.";
            return;
        }
        var m = MED_ProgressionManager.Instance;
        if (m != null) goalLabel.text = "Goal: " + m.NextGoal;
    }

    // The whole HUD disappears while the player is in another area.
    void OnAreaChanged(bool inside) => transform.localScale = inside ? Vector3.one : Vector3.zero;

    void OnDestroy()
    {
        MED_ProgressionManager.GoalChanged -= RefreshGoal;
        MED_AreaGate.Changed -= OnAreaChanged;
        if (instance == this) instance = null;
        if (goalBackdropMaterial != null) Destroy(goalBackdropMaterial);
        if (resources != null) resources.OnResourceChanged -= OnResourceChanged;
    }

    void TryBind()
    {
        resources = FindAnyObjectByType<MED_PlayerResources>();
        if (resources == null) return;
        resources.OnResourceChanged += OnResourceChanged;
        Refresh();
    }

    void OnResourceChanged(MED_ResourceType type, int amount) => Refresh();

    void Update()
    {
        if (integratedTraining != null &&
            (displayedInstruction != integratedTraining.CurrentInstruction || displayedIntroduction != integratedTraining.IsIntroducing))
            RefreshGoal();
        UpdateMessage();
        if (resources == null) { TryBind(); return; }
        float v = follow != null ? follow.PanelVisibility : 0f;
        if (!Mathf.Approximately(v, lastVisibility)) SetPanelVisibility(v);
    }

    void UpdateMessage()
    {
        if (messageLabel == null || messageTimer <= 0f) return;
        messageTimer -= Time.deltaTime;
        messageLabel.alpha = Mathf.Clamp01(messageTimer / 0.5f);
        if (messageTimer <= 0f) messageLabel.text = "";
    }

    void Refresh()
    {
        if (resources == null) return;
        float n = resources.MaxHealth > 0 ? (float)resources.Health / resources.MaxHealth : 0f;
        if (healthBar != null) healthBar.SetNormalized(n);
        if (healthLabel != null) healthLabel.text = $"Health {resources.Health}/{resources.MaxHealth}";

        var types = (MED_ResourceType[])Enum.GetValues(typeof(MED_ResourceType));
        var left = new StringBuilder();
        var right = new StringBuilder();
        for (int i = 0; i < types.Length; i++)
        {
            var sb = i < (types.Length + 1) / 2 ? left : right;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(types[i]).Append("  ").Append(resources.Get(types[i]));
        }
        if (leftColumn != null) leftColumn.text = left.ToString();
        if (rightColumn != null) rightColumn.text = right.ToString();
    }

    void SetPanelVisibility(float v)
    {
        lastVisibility = v;
        bool on = v > 0.001f;
        if (resourcePanel != null && resourcePanel.activeSelf != on) resourcePanel.SetActive(on);
        if (!on) return;
        if (leftColumn != null) leftColumn.alpha = v;
        if (rightColumn != null) rightColumn.alpha = v;
        if (panelBackdrop != null)
        {
            panelBackdrop.GetPropertyBlock(backdropBlock);
            backdropBlock.SetColor(BaseColorId, new Color(0.05f, 0.08f, 0.1f, panelBackdropAlpha * v));
            panelBackdrop.SetPropertyBlock(backdropBlock);
        }
    }
}
