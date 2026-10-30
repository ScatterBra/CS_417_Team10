using System;
using System.Text;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    TMP_Text integratedStats, integratedGoal, integratedIntroText;
    GameObject integratedIntro;
    RectTransform integratedObjective;
    DEF_ClinicReadiness integratedReadiness;
    struct Notice { public string Text; public float MinimumSeconds; public bool IsIntroduction; }
    readonly Queue<Notice> introductionQueue = new();
    readonly HashSet<string> pendingFeedback = new();
    readonly HashSet<string> seenIntroductions = new();
    string currentIntroduction, completedDescription;
    bool currentIsIntroduction;
    float introductionRemaining, completedAt = -100f;
    int observedStep;
    bool previousIntroduction, clinicCompleted;
    string previousIntroductionText;
    public static bool IntegratedNoticeVisible => instance != null && instance.integratedTraining != null &&
        MED_AreaGate.PlayerInside && instance.integratedIntro != null && instance.integratedIntro.activeSelf;
    string displayedStats;
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
        if (integratedTraining != null && integratedGoal != null)
        {
            if (pendingFeedback.Add(text)) introductionQueue.Enqueue(new Notice { Text = text, MinimumSeconds = seconds });
            return;
        }
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
        if (integratedTraining != null)
        {
            observedStep = integratedTraining.Step;
            integratedReadiness = integratedTraining.GetComponent<DEF_ClinicReadiness>();
            if (integratedReadiness != null) integratedReadiness.Completed += OnClinicCompleted;
            BuildIntegratedHUD();
        }
        else BuildGoalLabel();
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
    }

    void BuildIntegratedHUD()
    {
        // Replace only this HUD's legacy presentation, not the area gate or resource state.
        foreach (Transform child in transform) child.gameObject.SetActive(false);
        var go = new GameObject("Clinic context HUD", typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(660, 300);
        rect.localScale = Vector3.one * .001f;
        Panel(rect, "Clinic status background", new Vector2(660, 190), Vector2.zero);
        integratedStats = Label(rect, "Clinic inventory", new Vector2(620, 158), Vector2.zero, 25);
        integratedObjective = (RectTransform)Panel(rect, "Clinic corner objective", new Vector2(600, 170), new Vector2(940, 550)).transform;
        integratedGoal = Label(integratedObjective, "Clinic next step", new Vector2(560, 145), Vector2.zero, 24);
        integratedIntro = Panel(rect, "Clinic timed introduction", new Vector2(600, 240), new Vector2(940, 210));
        integratedIntroText = Label(integratedIntro.transform, "Clinic introduction text", new Vector2(560, 215), Vector2.zero, 24);
        integratedIntro.SetActive(false);
    }

    static GameObject Panel(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition = position;
        var image = go.GetComponent<Image>();
        image.color = new Color(.015f, .02f, .025f, .94f);
        image.raycastTarget = false;
        return go;
    }

    static TMP_Text Label(Transform parent, string name, Vector2 size, Vector2 position, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(.5f, .5f);
        text.rectTransform.sizeDelta = size; text.rectTransform.anchoredPosition = position;
        text.fontSize = fontSize; text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    void RefreshGoal()
    {
        if (integratedTraining != null)
        {
            displayedInstruction = integratedTraining.CurrentInstruction;
            bool introducingHere = integratedTraining.IsIntroducing && MED_AreaGate.PlayerInside;
            if (introducingHere && (!previousIntroduction || previousIntroductionText != displayedInstruction) && seenIntroductions.Add(displayedInstruction))
                introductionQueue.Enqueue(new Notice { Text = displayedInstruction, IsIntroduction = true });
            previousIntroduction = introducingHere;
            previousIntroductionText = displayedInstruction;
            if (integratedTraining.Step > observedStep)
            {
                ClearStaleFeedback();
                completedAt = Time.unscaledTime;
                completedDescription = observedStep == 7 ? "Herbs, Water and supplies harvested." :
                    observedStep == 8 ? "Random resource harvested and cart unloaded." : "PrepBench placed and Medicine crafted.";
            }
            observedStep = integratedTraining.Step;
            float animation = Mathf.Clamp01((Time.unscaledTime - completedAt) / 1.6f);
            bool showCheck = Time.unscaledTime - completedAt < 2.2f;
            if (integratedObjective != null) integratedObjective.localScale = Vector3.one * (1f + .035f * Mathf.Sin(animation * Mathf.PI));
            if (integratedGoal != null)
            {
                string heading = clinicCompleted ? "CLINIC COMPLETE\n" : "CLINIC — NEXT\n";
                integratedGoal.color = showCheck ? new Color(.55f, 1f, .65f) : Color.white;
                DEF_UIPresentationPolicy.SetCompletionMark(integratedGoal, showCheck);
                integratedGoal.text = showCheck ? "COMPLETED\n" + completedDescription : heading + integratedTraining.CompactInstruction;
            }
            UpdateIntroduction();
            return;
        }
        var m = MED_ProgressionManager.Instance;
        if (goalLabel != null && m != null) goalLabel.text = "Goal: " + m.NextGoal;
    }

    void OnClinicCompleted(DEF_PrerequisiteSource source, int session, string receipt)
    {
        if (source != integratedReadiness || string.IsNullOrEmpty(receipt)) return;
        ClearStaleFeedback();
        clinicCompleted = true;
        completedDescription = "Fresh Medicine treated the Clinic patient to 100/100.";
        completedAt = Time.unscaledTime;
    }

    void ClearStaleFeedback()
    {
        // Old lock/failure text must not surface after the action has genuinely advanced.
        int queued = introductionQueue.Count;
        for (int i = 0; i < queued; i++)
        {
            var notice = introductionQueue.Dequeue();
            if (notice.IsIntroduction) introductionQueue.Enqueue(notice);
        }
        pendingFeedback.Clear();
        if (!currentIsIntroduction) { currentIntroduction = null; introductionRemaining = 0f; }
    }

    void UpdateIntroduction()
    {
        if (integratedIntro == null) return;
        if (!MED_AreaGate.PlayerInside) { integratedIntro.SetActive(false); return; }
        if (currentIntroduction != null) introductionRemaining -= Time.unscaledDeltaTime;
        if (currentIntroduction == null || introductionRemaining <= 0f)
        {
            if (currentIntroduction != null && !currentIsIntroduction) pendingFeedback.Remove(currentIntroduction);
            if (introductionQueue.Count > 0)
            {
                var notice = introductionQueue.Dequeue();
                currentIntroduction = notice.Text;
                currentIsIntroduction = notice.IsIntroduction;
                introductionRemaining = Mathf.Max(notice.MinimumSeconds, DEF_UIPresentationPolicy.ReadSeconds(notice.Text));
            }
            else currentIntroduction = null;
        }
        integratedIntro.SetActive(currentIntroduction != null);
        if (currentIntroduction != null) integratedIntroText.text = (currentIsIntroduction ? "CLINIC — INTRODUCTION\n" : "CLINIC — FEEDBACK\n") + currentIntroduction;
    }

    void RefreshIntegratedStats()
    {
        if (integratedStats == null || resources == null) return;
        var text = new StringBuilder($"CLINIC  Health {resources.Health}/{resources.MaxHealth}\n");
        text.Append($"Herbs {resources.Get(MED_ResourceType.Herbs)}   Water {resources.Get(MED_ResourceType.Water)}   Medicine {resources.Get(MED_ResourceType.Medicine)}\n");
        text.Append($"Alcohol {resources.Get(MED_ResourceType.Alcohol)}   Gauze {resources.Get(MED_ResourceType.Gauze)}");
        foreach (MED_ResourceType type in new[] { MED_ResourceType.BandageKit, MED_ResourceType.Antiseptic, MED_ResourceType.Painkiller, MED_ResourceType.Tonic })
            if (resources.Get(type) > 0) text.Append($"   {type} {resources.Get(type)}");
        var next = text.ToString();
        if (next != displayedStats) { integratedStats.text = next; displayedStats = next; }
    }

    // The whole HUD disappears while the player is in another area.
    void OnAreaChanged(bool inside) => transform.localScale = inside ? Vector3.one : Vector3.zero;

    void OnDestroy()
    {
        MED_ProgressionManager.GoalChanged -= RefreshGoal;
        MED_AreaGate.Changed -= OnAreaChanged;
        if (instance == this) instance = null;
        if (integratedReadiness != null) integratedReadiness.Completed -= OnClinicCompleted;
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
        if (integratedTraining != null)
        {
            // Explicitly apply current area state; do not depend on an event being caught at startup.
            OnAreaChanged(MED_AreaGate.PlayerInside);
            RefreshGoal();
            if (resources == null) TryBind();
            RefreshIntegratedStats();
            return; // No look-down gesture or legacy resource-panel visibility in the integrated scene.
        }
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
