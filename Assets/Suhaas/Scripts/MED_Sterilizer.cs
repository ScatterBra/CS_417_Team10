using System;
using System.Collections.Generic;
using UnityEngine;

// Placed Sterilizer. START spends the recipe inputs (2 Gauze + 1 Water), heats up, sterilizes, then delivers a
// BandageKit. The unit stays hot afterwards and cools down on its own, so it cannot be restarted right away.
public class MED_Sterilizer : MonoBehaviour
{
    public enum SterilizerState { Idle, Heating, Sterilizing }

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] MED_Stat heat = new MED_Stat("Heat", 0f, 0f, 1f);
    [SerializeField] MED_Stat sterilizeProgress = new MED_Stat("SterilizeProgress", 0f, 0f, 1f);
    [SerializeField] MED_Recipe recipe;
    [SerializeField] float heatTime = 3f;
    [SerializeField] float sterilizeTime = 3f;
    [SerializeField] float coolTime = 8f;
    [SerializeField] float startMaxHeat = 0.2f;
    [SerializeField] float maxUseDistance = 3.5f;
    [SerializeField] MED_ActionButton startButton;
    [SerializeField] MED_StatBar heatBar;
    [SerializeField] MED_StatBar progressBar;
    [SerializeField] Renderer bodyRenderer;       // uses an emission-enabled material
    [SerializeField] Transform lid;
    [SerializeField] Transform lidAttachment;     // knob, lifts with the lid
    [SerializeField] Transform[] steamPuffs;
    [SerializeField] GameObject hotLabel;
    [SerializeField] Transform messageAnchor;

    MED_PlayerResources player;
    MaterialPropertyBlock bodyBlock;
    Vector3 lidRest, attachRest;
    bool lidCached;
    float lidLift;

    public SterilizerState State { get; private set; } = SterilizerState.Idle;
    public MED_Stat Heat => heat;
    public MED_Stat SterilizeProgress => sterilizeProgress;
    public MED_Recipe Recipe => recipe;
    public event Action StateChanged;
    /// <summary>Raised on any Sterilizer when a batch is delivered (used by the progression thresholds).</summary>
    public static event Action Sterilized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Sterilized = null;

    Vector3 MessagePosition => messageAnchor != null ? messageAnchor.position : transform.position + Vector3.up * 1.5f;

    void OnEnable()
    {
        heat.OnChanged += OnHeatChanged;
        if (startButton != null) startButton.Pressed += OnStartPressed;
        Bind();
    }

    void OnDisable()
    {
        heat.OnChanged -= OnHeatChanged;
        if (startButton != null) startButton.Pressed -= OnStartPressed;
        if (player != null) player.OnResourceChanged -= OnResourceChanged;
        player = null;
    }

    void Start()
    {
        bodyBlock = new MaterialPropertyBlock();
        if (lid != null)
        {
            lidRest = lid.localPosition;
            if (lidAttachment != null) attachRest = lidAttachment.localPosition;
            lidCached = true;
        }
        if (heatBar != null) { heatBar.SetFixedColor(new Color(0.95f, 0.25f, 0.15f)); heatBar.Bind(heat); }
        if (progressBar != null) { progressBar.SetFixedColor(new Color(0.4f, 0.8f, 1f)); progressBar.Bind(sterilizeProgress); progressBar.SetAlpha(0f); }
        foreach (var p in steamPuffs) if (p != null) p.gameObject.SetActive(false);
        Bind();
        RefreshVisuals();
        RefreshButtons();
    }

    void Bind()
    {
        if (player != null) return;
        player = FindAnyObjectByType<MED_PlayerResources>();
        if (player != null) player.OnResourceChanged += OnResourceChanged;
    }

    void OnResourceChanged(MED_ResourceType type, int amount)
    {
        foreach (var cost in recipe.inputs)
            if (cost.type == type) { RefreshButtons(); return; }
    }

    void OnHeatChanged(MED_Stat _)
    {
        RefreshVisuals();
        RefreshButtons();
    }

    // ---------- Passive simulation ----------

    void Update()
    {
        float dt = Time.deltaTime;

        // Euler integration of the Heat / SterilizeProgress stats: value += rate * dt each frame.
        switch (State)
        {
            case SterilizerState.Idle:
                // Cooling runs even when nobody is using the sterilizer.
                if (heat.Value > 0f) heat.Add(-(1f / coolTime) * dt);
                break;
            case SterilizerState.Heating:
                heat.Add((1f / heatTime) * dt);
                if (heat.Value >= 1f) SetState(SterilizerState.Sterilizing);
                break;
            case SterilizerState.Sterilizing:
                sterilizeProgress.Add((1f / sterilizeTime) * dt);
                if (sterilizeProgress.Value >= 1f) Complete();
                break;
        }

        AnimateActive(dt);
    }

    void Complete()
    {
        foreach (var output in recipe.outputs)
        {
            if (player != null) player.Add(output.type, output.amount);
            MED_FloatingText.Spawn(MessagePosition, $"+{output.amount} {output.type}", new Color(0.6f, 1f, 0.6f));
        }
        sterilizeProgress.Set(0f);
        SetState(SterilizerState.Idle);
        Sterilized?.Invoke();
    }

    // ---------- Actions ----------

    void OnStartPressed() => TryStart();

    public bool TryStart()
    {
        if (!MED_PlayerProximity.IsNear(transform.position, maxUseDistance)) return Fail(MED_Messages.SterilizerTooFar);
        Bind();
        if (State != SterilizerState.Idle) return Fail(MED_Messages.SterilizerBusy);
        if (heat.Value >= startMaxHeat) return Fail(MED_Messages.SterilizerHot);
        if (!HasInputs(out var missing)) return Fail(missing);

        // HasInputs checked every input, so none of these can fail halfway: spend atomically.
        foreach (var cost in recipe.inputs) player.TrySpend(cost.type, cost.amount);
        sterilizeProgress.Set(0f);
        SetState(SterilizerState.Heating);
        return true;
    }

    bool HasInputs(out string problem)
    {
        problem = null;
        foreach (var cost in recipe.inputs)
        {
            int have = player != null ? player.Get(cost.type) : 0;
            if (have >= cost.amount) continue;
            problem = MED_Messages.SterilizerMissing(cost.type, have, cost.amount);
            return false;
        }
        return true;
    }

    void SetState(SterilizerState state)
    {
        State = state;
        if (progressBar != null) progressBar.SetAlpha(state == SterilizerState.Sterilizing ? 0.9f : 0f);
        RefreshVisuals();
        RefreshButtons();
        StateChanged?.Invoke();
    }

    // ---------- Visuals ----------

    void RefreshButtons()
    {
        if (startButton == null) return;
        bool inputs = true;
        foreach (var cost in recipe.inputs)
            if (player == null || player.Get(cost.type) < cost.amount) inputs = false;
        startButton.SetAvailable(State == SterilizerState.Idle && heat.Value < startMaxHeat && inputs);
    }

    void RefreshVisuals()
    {
        if (bodyBlock == null) return;
        // Emissive glow from orange to red as Heat rises.
        if (bodyRenderer != null)
        {
            var glow = Color.Lerp(new Color(1f, 0.55f, 0.1f), new Color(1f, 0.1f, 0.05f), heat.Normalized) * (heat.Normalized * 2.2f);
            bodyRenderer.GetPropertyBlock(bodyBlock);
            bodyBlock.SetColor(EmissionColorId, glow);
            bodyRenderer.SetPropertyBlock(bodyBlock);
        }
        if (hotLabel != null) hotLabel.SetActive(heat.Value > 0.5f);
        if (heatBar != null) heatBar.SetAlpha(heat.Value > 0.01f || State != SterilizerState.Idle ? 0.9f : 0.3f);
    }

    void AnimateActive(float dt)
    {
        bool active = State != SterilizerState.Idle;
        lidLift = Mathf.MoveTowards(lidLift, active ? 1f : 0f, dt * 4f);
        if (lidCached)
        {
            var offset = Vector3.up * (0.05f * lidLift);
            lid.localPosition = lidRest + offset;
            if (lidAttachment != null) lidAttachment.localPosition = attachRest + offset;
        }

        // Steam: small spheres rise from the lid in a staggered loop while active.
        for (int i = 0; i < steamPuffs.Length; i++)
        {
            var p = steamPuffs[i];
            if (p == null) continue;
            if (!active) { if (p.gameObject.activeSelf) p.gameObject.SetActive(false); continue; }
            if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
            float t = Mathf.Repeat(Time.time * 0.6f + i / (float)steamPuffs.Length, 1f);
            float angle = i * 2.4f;
            p.localPosition = new Vector3(Mathf.Cos(angle) * 0.1f * t, 0.98f + t * 0.5f, Mathf.Sin(angle) * 0.1f * t);
            p.localScale = Vector3.one * Mathf.Lerp(0.05f, 0.14f, t) * Mathf.Sin(t * Mathf.PI);
        }
    }

    bool Fail(string message)
    {
        return MED_Messages.Fail(MessagePosition, message);
    }
}
