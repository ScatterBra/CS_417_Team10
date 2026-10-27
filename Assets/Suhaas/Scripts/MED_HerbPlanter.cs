using System;
using UnityEngine;

// Placed Herb Planter. Caretaking: PLANT spends 1 Herbs, WATER spends 1 Water into the soil. The soil dries over
// time and the plant only grows while the soil is damp. A Sickle harvests the ripe plant (3 Herbs).
public class MED_HerbPlanter : MonoBehaviour, MED_IToolTarget
{
    public enum PlanterState { Empty, Growing, Ripe }

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] MED_Stat growth = new MED_Stat("Growth", 0f, 0f, 1f);
    [SerializeField] MED_Stat moisture = new MED_Stat("Moisture", 0f, 0f, 1f);
    [SerializeField] float growTime = 30f;
    [SerializeField] float dryRate = 0.04f;
    [SerializeField] float waterAmount = 0.5f;
    [SerializeField] int harvestYield = 3;
    [SerializeField] MED_ToolType requiredTool = MED_ToolType.Sickle;
    [SerializeField] float hitCooldown = 0.5f;
    [SerializeField] float maxUseDistance = 3.5f;
    [SerializeField] MED_ActionButton plantButton;
    [SerializeField] MED_ActionButton waterButton;
    [SerializeField] MED_StatBar moistureBar;
    [SerializeField] MED_StatBar growthBar;
    [SerializeField] Renderer soilRenderer;
    [SerializeField] Transform plantRoot;          // all herb leaves; scaled with Growth
    [SerializeField] Renderer[] leafRenderers;     // tinted when drooping
    [SerializeField] Renderer ripeGlow;
    [SerializeField] Transform messageAnchor;

    static readonly Color DrySoil = new Color(0.45f, 0.30f, 0.18f);
    static readonly Color WetSoil = new Color(0.10f, 0.06f, 0.04f);
    static readonly Color HealthyLeaf = new Color(0.2f, 0.65f, 0.25f);
    static readonly Color WiltedLeaf = new Color(0.55f, 0.5f, 0.2f);

    MED_PlayerResources player;
    MaterialPropertyBlock soilBlock, leafBlock, glowBlock;
    float nextHitTime;
    float nextMessageTime;
    float droop;   // 0 = upright, 1 = fully drooping (smoothed)

    public PlanterState State { get; private set; } = PlanterState.Empty;
    public MED_Stat Growth => growth;
    public MED_Stat Moisture => moisture;
    public event Action StateChanged;

    Vector3 MessagePosition => messageAnchor != null ? messageAnchor.position : transform.position + Vector3.up * 1.2f;

    void OnEnable()
    {
        moisture.OnChanged += OnStatChanged;
        growth.OnChanged += OnStatChanged;
        if (plantButton != null) plantButton.Pressed += OnPlantPressed;
        if (waterButton != null) waterButton.Pressed += OnWaterPressed;
        Bind();
    }

    void OnDisable()
    {
        moisture.OnChanged -= OnStatChanged;
        growth.OnChanged -= OnStatChanged;
        if (plantButton != null) plantButton.Pressed -= OnPlantPressed;
        if (waterButton != null) waterButton.Pressed -= OnWaterPressed;
        if (player != null) player.OnResourceChanged -= OnResourceChanged;
        player = null;
    }

    void Start()
    {
        soilBlock = new MaterialPropertyBlock();
        leafBlock = new MaterialPropertyBlock();
        glowBlock = new MaterialPropertyBlock();
        if (moistureBar != null) { moistureBar.SetFixedColor(new Color(0.25f, 0.55f, 1f)); moistureBar.Bind(moisture); }
        if (growthBar != null) { growthBar.SetFixedColor(new Color(0.3f, 0.85f, 0.35f)); growthBar.Bind(growth); }
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
        if (type == MED_ResourceType.Herbs || type == MED_ResourceType.Water) RefreshButtons();
    }

    void OnStatChanged(MED_Stat _)
    {
        RefreshVisuals();
        RefreshButtons();
    }

    // ---------- Passive simulation ----------

    void Update()
    {
        float dt = Time.deltaTime;

        // Euler integration: each frame the stats move by (rate * dt), i.e. value += d(value)/dt * dt.
        if (moisture.Value > 0f) moisture.Add(-dryRate * dt);

        if (State == PlanterState.Growing && moisture.Value > 0f)
        {
            growth.Add((1f / growTime) * dt);
            if (growth.Value >= 1f) SetState(PlanterState.Ripe);
        }

        // Drooping is a smoothed visual: a growing plant with dry soil sags until watered.
        float droopTarget = (State == PlanterState.Growing && moisture.Value <= 0f) ? 1f : 0f;
        float newDroop = Mathf.MoveTowards(droop, droopTarget, dt * 2f);
        if (!Mathf.Approximately(newDroop, droop) || State == PlanterState.Ripe)
        {
            droop = newDroop;
            AnimatePlant();
        }
    }

    // ---------- Actions ----------

    void OnPlantPressed() => TryPlant();
    void OnWaterPressed() => TryWater();

    public bool TryPlant()
    {
        if (!MED_PlayerProximity.IsNear(transform.position, maxUseDistance)) return Fail(MED_Messages.PlantTooFar);
        Bind();
        if (State != PlanterState.Empty) return Fail(MED_Messages.AlreadyPlanted);
        if (player == null || player.Get(MED_ResourceType.Herbs) < 1) return Fail(MED_Messages.PlantNoHerbs);
        player.TrySpend(MED_ResourceType.Herbs, 1);
        growth.Set(0f);
        SetState(PlanterState.Growing);
        MED_FloatingText.Spawn(MessagePosition, "Planted", new Color(0.6f, 1f, 0.6f));
        return true;
    }

    public bool TryWater()
    {
        if (!MED_PlayerProximity.IsNear(transform.position, maxUseDistance)) return Fail(MED_Messages.PlanterWaterTooFar);
        Bind();
        if (moisture.Value >= 0.95f) return Fail(MED_Messages.SoilWet);
        if (player == null || player.Get(MED_ResourceType.Water) < 1) return Fail(MED_Messages.PlanterNoWater);
        player.TrySpend(MED_ResourceType.Water, 1);
        moisture.Add(waterAmount);
        MED_FloatingText.Spawn(MessagePosition, "Watered", new Color(0.5f, 0.8f, 1f));
        return true;
    }

    /// <summary>Sickle hit (called by MED_HarvestTool).</summary>
    public bool TryHarvest(MED_HarvestTool tool, MED_PlayerResources harvester)
    {
        if (tool == null || tool.ToolType != requiredTool)
        {
            Message(MED_Messages.PlanterWrongTool, new Color(1f, 0.85f, 0.4f));
            return false;
        }
        if (State == PlanterState.Empty)
        {
            Message(MED_Messages.NothingPlanted, new Color(1f, 0.85f, 0.4f));
            return false;
        }
        if (State != PlanterState.Ripe)
        {
            Message(MED_Messages.NotRipe, new Color(1f, 0.85f, 0.4f));
            return false;
        }
        if (Time.time < nextHitTime) return false;
        nextHitTime = Time.time + hitCooldown;

        if (harvester != null) harvester.Add(MED_ResourceType.Herbs, harvestYield);
        MED_FloatingText.Spawn(MessagePosition, $"+{harvestYield} Herbs", new Color(0.6f, 1f, 0.6f));
        tool.SendHaptic(0.5f, 0.1f);
        growth.Set(0f);
        SetState(PlanterState.Empty);
        return true;
    }

    /// <summary>Test hook: jump straight to a ripe plant.</summary>
    public void Ripen()
    {
        growth.Set(1f);
        SetState(PlanterState.Ripe);
    }

    void SetState(PlanterState state)
    {
        State = state;
        RefreshVisuals();
        RefreshButtons();
        StateChanged?.Invoke();
    }

    // ---------- Visuals ----------

    void RefreshButtons()
    {
        if (plantButton != null)
            plantButton.SetAvailable(State == PlanterState.Empty && player != null && player.Get(MED_ResourceType.Herbs) >= 1);
        if (waterButton != null)
            waterButton.SetAvailable(moisture.Value < 0.95f && player != null && player.Get(MED_ResourceType.Water) >= 1);
    }

    void RefreshVisuals()
    {
        if (soilBlock == null) return;
        if (soilRenderer != null)
        {
            soilRenderer.GetPropertyBlock(soilBlock);
            soilBlock.SetColor(BaseColorId, Color.Lerp(DrySoil, WetSoil, moisture.Normalized));
            soilRenderer.SetPropertyBlock(soilBlock);
        }
        if (plantRoot != null) plantRoot.gameObject.SetActive(State != PlanterState.Empty);
        if (ripeGlow != null) ripeGlow.enabled = State == PlanterState.Ripe;
        if (growthBar != null) growthBar.SetAlpha(State == PlanterState.Empty ? 0.25f : 0.9f);
        AnimatePlant();
    }

    void AnimatePlant()
    {
        if (plantRoot == null || leafBlock == null) return;
        float g = State == PlanterState.Ripe ? 1f : growth.Normalized;
        float h = Mathf.Lerp(0.35f, 1f, g);
        // Drooping: squash the height and tip the plant over, and tint the leaves yellow-brown.
        plantRoot.localScale = new Vector3(Mathf.Lerp(0.5f, 1f, g) * (1f + 0.15f * droop), h * (1f - 0.45f * droop), Mathf.Lerp(0.5f, 1f, g) * (1f + 0.15f * droop));
        plantRoot.localRotation = Quaternion.Euler(18f * droop, 0f, 10f * droop);

        if (leafRenderers != null)
        {
            var leaf = Color.Lerp(HealthyLeaf, WiltedLeaf, droop);
            foreach (var r in leafRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(leafBlock);
                leafBlock.SetColor(BaseColorId, leaf);
                r.SetPropertyBlock(leafBlock);
            }
        }
        if (ripeGlow != null && State == PlanterState.Ripe)
        {
            float a = 0.18f + 0.14f * (0.5f + 0.5f * Mathf.Sin(Time.time * 4f));
            ripeGlow.GetPropertyBlock(glowBlock);
            glowBlock.SetColor(BaseColorId, new Color(1f, 0.95f, 0.5f, a));
            ripeGlow.SetPropertyBlock(glowBlock);
        }
    }

    // ---------- Messages ----------

    void Message(string text, Color c)
    {
        if (Time.time < nextMessageTime) return;
        nextMessageTime = Time.time + 1.5f;
        MED_Messages.Fail(MessagePosition, text);
    }

    bool Fail(string message)
    {
        return MED_Messages.Fail(MessagePosition, message);
    }
}
