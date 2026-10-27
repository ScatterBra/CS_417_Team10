using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MED_Feature
{
    CabinetModel,
    TreatmentModel,
    TreatButton,
    SterilizerModel,
    RecipeBandageKit,
    RecipeTonic,
    RecipePainkiller,
    PlanterModel,
}

/// <summary>
/// Rubric: Mastery Thresholding, Unlock Reveal, Thresholds on the Horizon, Error Tolerant Lessons (locked use).
/// The Clinic starts with only the Prep Bench model, the tools, herb patches/crates, the barrel and the cart.
/// Four thresholds release the rest, each behind a reveal card:
///   T1 craft the first Medicine         -> Medicine Cabinet + Treatment Station models
///   T2 stock the Treatment Station once -> TREAT button + Sterilizer model
///   T3 give the first treatment dose    -> BandageKit / Tonic / Painkiller recipes
///   T4 heal the player to 100           -> Herb Planter model, ClinicComplete
/// Same shape as PWR_Progression (Deadzone.Power). The Clinic project has no namespaces, so none is used here.
/// </summary>
public class MED_ProgressionManager : MonoBehaviour
{
    public const int ThresholdCount = 4;

    public static MED_ProgressionManager Instance { get; private set; }

    /// <summary>True once the last threshold is cleared (stays true until RESET PROGRESSION).</summary>
    public static bool ClinicComplete { get; private set; }
    /// <summary>Fires once when <see cref="ClinicComplete"/> becomes true.</summary>
    public static event Action ClinicCompleted;
    /// <summary>Raised whenever the set of unlocked features changes (models, button, recipes).</summary>
    public static event Action Changed;
    /// <summary>Raised whenever the goal text or its progress changes.</summary>
    public static event Action GoalChanged;

    [Tooltip("Seconds each reveal card stays up before its content unlocks.")]
    [SerializeField] float revealSeconds = 6f;
    [SerializeField] Vector3 signPosition = new Vector3(0f, 2.4f, -0.5f);
    [SerializeField] Vector3 trayPosition = new Vector3(-6.3f, 0f, -2.6f);

    struct RevealItem
    {
        public string name;
        public string metaphor;
        public RevealItem(string n, string m) { name = n; metaphor = m; }
    }

    struct Unlock
    {
        public int threshold;
        public RevealItem[] items;
        public MED_Feature[] features;
    }

    static readonly HashSet<string> shownReveals = new HashSet<string>();
    readonly HashSet<MED_Feature> unlocked = new HashSet<MED_Feature>();
    readonly Queue<Unlock> pending = new Queue<Unlock>();
    readonly List<MED_WorldPanel> cards = new List<MED_WorldPanel>();

    MED_PlayerResources player;
    MED_WorldPanel sign;
    GameObject tray;
    bool signVisible = true;
    bool revealing;
    bool completeRaised;
    int stocked, dosed, crafted;

    /// <summary>Number of thresholds cleared so far: 0 at the start, 4 when the Clinic is complete.</summary>
    public int CurrentThreshold { get; private set; }

    /// <summary>The next requirement with its progress, e.g. "Heal to 100: 60/100".</summary>
    public string NextGoal => CurrentThreshold >= ThresholdCount ? "Clinic complete - every station is unlocked" : GoalTitle(CurrentThreshold) + ": " + Progress(CurrentThreshold);

    /// <summary>True while a reveal card is on screen or waiting to be shown.</summary>
    public bool IsRevealing => revealing || pending.Count > 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        ClinicComplete = false;
        ClinicCompleted = null;
        Changed = null;
        GoalChanged = null;
        shownReveals.Clear();
    }

    // ---------- Queries used by the Clinic's components ----------

    public static bool IsLocked(MED_Feature feature) => Instance != null && !Instance.unlocked.Contains(feature);

    /// <summary>True when a sandbox for the given threshold (1-4) is open. Always true when no manager exists.</summary>
    public static bool SandboxActive(int threshold) => Instance == null || Instance.CurrentThreshold >= threshold;

    public static bool TryGetFeature(MED_BuildingDefinition def, out MED_Feature feature)
    {
        feature = default;
        if (def == null) return false;
        string n = def.name;
        if (n.Contains("MedicineCabinet")) feature = MED_Feature.CabinetModel;
        else if (n.Contains("TreatmentStation")) feature = MED_Feature.TreatmentModel;
        else if (n.Contains("Sterilizer")) feature = MED_Feature.SterilizerModel;
        else if (n.Contains("HerbPlanter")) feature = MED_Feature.PlanterModel;
        else return false;
        return true;
    }

    public static bool TryGetFeature(MED_Recipe recipe, out MED_Feature feature)
    {
        feature = default;
        if (recipe == null) return false;
        string n = recipe.name;
        if (n.Contains("BandageKit")) feature = MED_Feature.RecipeBandageKit;
        else if (n.Contains("Tonic")) feature = MED_Feature.RecipeTonic;
        else if (n.Contains("Painkiller")) feature = MED_Feature.RecipePainkiller;
        else return false;
        return true;
    }

    public static string RequirementFor(MED_Feature feature)
    {
        switch (feature)
        {
            case MED_Feature.CabinetModel:
            case MED_Feature.TreatmentModel: return "craft your first Medicine at the Prep Bench";
            case MED_Feature.TreatButton:
            case MED_Feature.SterilizerModel: return "stock the Treatment Station with Medicine once";
            case MED_Feature.RecipeBandageKit:
            case MED_Feature.RecipeTonic:
            case MED_Feature.RecipePainkiller: return "give one treatment dose with the TREAT button";
            default: return "heal yourself to 100 HP";
        }
    }

    // ---------- Lifecycle ----------

    void Awake()
    {
        Instance = this;
        if (GetComponent<MED_AreaGate>() == null) gameObject.AddComponent<MED_AreaGate>();
    }

    void Start()
    {
        BindPlayer();
        tray = MED_MedicineTray.Create(trayPosition);
        tray.SetActive(false);
        BuildSign();
        foreach (var b in FindObjectsByType<MED_Building>()) AttachSandbox(b);
        ApplyAll();
    }

    void OnEnable()
    {
        MED_PrepBench.Crafted += OnCrafted;
        MED_TreatmentStation.Stocked += OnStocked;
        MED_TreatmentStation.DoseGiven += OnDoseGiven;
        MED_Building.OnBuilt += AttachSandbox;
        MED_AreaGate.Changed += OnAreaChanged;
    }

    void OnDisable()
    {
        MED_PrepBench.Crafted -= OnCrafted;
        MED_TreatmentStation.Stocked -= OnStocked;
        MED_TreatmentStation.DoseGiven -= OnDoseGiven;
        MED_Building.OnBuilt -= AttachSandbox;
        MED_AreaGate.Changed -= OnAreaChanged;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (player != null) player.OnResourceChanged -= OnResourceChanged;
        if (sign != null) sign.Destroy();
        foreach (var c in cards) c.Destroy();
        if (tray != null) Destroy(tray);
    }

    void BindPlayer()
    {
        if (player != null) return;
        player = FindAnyObjectByType<MED_PlayerResources>();
        if (player != null) player.OnResourceChanged += OnResourceChanged;
    }

    void Update()
    {
        if (player == null) BindPlayer();
        if (sign != null && sign.Root != null)
        {
            if (signVisible != MED_AreaGate.PlayerInside)
            {
                signVisible = MED_AreaGate.PlayerInside;
                sign.Root.SetActive(signVisible);
            }
        }
    }

    void OnAreaChanged(bool inside)
    {
        if (sign != null && sign.Root != null) sign.Root.SetActive(inside);
        signVisible = inside;
    }

    // ---------- Threshold detection ----------

    void OnCrafted(MED_Recipe recipe)
    {
        if (recipe == null || CurrentThreshold != 0) return;
        foreach (var o in recipe.outputs)
            if (o.type == MED_ResourceType.Medicine) { crafted = 1; ClearThreshold(1); return; }
    }

    void OnStocked()
    {
        if (CurrentThreshold != 1) return;
        stocked = 1;
        ClearThreshold(2);
    }

    void OnDoseGiven()
    {
        if (CurrentThreshold != 2) return;
        dosed = 1;
        ClearThreshold(3);
    }

    void OnResourceChanged(MED_ResourceType type, int amount)
    {
        if (type != MED_ResourceType.Health) return;
        if (CurrentThreshold == 3 && player.Health >= player.MaxHealth) ClearThreshold(4);
        else if (CurrentThreshold == 3) RefreshSign();
    }

    string Progress(int threshold)
    {
        switch (threshold)
        {
            case 0: return crafted + "/1";
            case 1: return stocked + "/1";
            case 2: return dosed + "/1";
            default: return player != null ? player.Health + "/" + player.MaxHealth : "0/100";
        }
    }

    static string GoalTitle(int threshold)
    {
        switch (threshold)
        {
            case 0: return "Craft your first Medicine";
            case 1: return "Stock the Treatment Station";
            case 2: return "Give a treatment dose";
            default: return "Heal to 100";
        }
    }

    // ---------- Clearing a threshold ----------

    /// <summary>Marks the threshold (1-4) as cleared, queues its reveal card and unlock. Ignored if out of order.</summary>
    void ClearThreshold(int n)
    {
        if (n != CurrentThreshold + 1 || n > ThresholdCount) return;
        CurrentThreshold = n;
        pending.Enqueue(UnlockFor(n));
        if (!revealing) StartCoroutine(RunReveals());
        ApplySandboxes();
        RefreshSign();
        if (n == ThresholdCount && !completeRaised)
        {
            completeRaised = true;
            ClinicComplete = true;
            ClinicCompleted?.Invoke();
        }
    }

    /// <summary>Test hook (SKIP TO NEXT THRESHOLD): clears the next threshold as if the player had done it.</summary>
    public void SkipToNext()
    {
        if (CurrentThreshold >= ThresholdCount) return;
        int next = CurrentThreshold + 1;
        // Keep the progress text honest for the skipped threshold.
        if (next == 1) crafted = 1;
        else if (next == 2) stocked = 1;
        else if (next == 3) dosed = 1;
        ClearThreshold(next);
    }

    /// <summary>Test hook (RESET PROGRESSION): back to the start state, as if the scene had just loaded.</summary>
    public void ResetProgression()
    {
        StopAllCoroutines();
        foreach (var c in cards) c.Destroy();
        cards.Clear();
        pending.Clear();
        revealing = false;
        unlocked.Clear();
        shownReveals.Clear();
        CurrentThreshold = 0;
        crafted = stocked = dosed = 0;
        ClinicComplete = false;
        completeRaised = false;

        var placer = FindAnyObjectByType<MED_BuildingPlacer>();
        if (placer != null) placer.ClearAll();
        BindPlayer();
        if (player != null) player.ResetHealth();
        ApplyAll();
    }

    static Unlock UnlockFor(int n)
    {
        switch (n)
        {
            case 1:
                return new Unlock
                {
                    threshold = 1,
                    features = new[] { MED_Feature.CabinetModel, MED_Feature.TreatmentModel },
                    items = new[]
                    {
                        new RevealItem("Medicine Cabinet", "a pharmacy shelf: STOCK puts Medicine in, TAKE hands it back."),
                        new RevealItem("Treatment Station", "an IV stand: refill it with Medicine, stand on the pad, heal."),
                    },
                };
            case 2:
                return new Unlock
                {
                    threshold = 2,
                    features = new[] { MED_Feature.TreatButton, MED_Feature.SterilizerModel },
                    items = new[]
                    {
                        new RevealItem("TREAT button", "a nurse's call button: stand on the green pad and press to start a dose."),
                        new RevealItem("Sterilizer", "a pressure cooker: add gauze and water, press START, let the glow fade."),
                    },
                };
            case 3:
                return new Unlock
                {
                    threshold = 3,
                    features = new[] { MED_Feature.RecipeBandageKit, MED_Feature.RecipeTonic, MED_Feature.RecipePainkiller },
                    items = new[]
                    {
                        new RevealItem("BandageKit, Tonic, Painkiller recipes", "more pages in the recipe book: grey cards turn green when you can afford them."),
                    },
                };
            default:
                return new Unlock
                {
                    threshold = 4,
                    features = new[] { MED_Feature.PlanterModel },
                    items = new[]
                    {
                        new RevealItem("Herb Planter", "a window box: plant, water, wait, then cut the ripe herbs."),
                    },
                };
        }
    }

    // ---------- Reveal cards ----------

    IEnumerator RunReveals()
    {
        revealing = true;
        while (pending.Count > 0)
        {
            var u = pending.Dequeue();
            var fresh = new List<RevealItem>();
            foreach (var item in u.items)
                if (shownReveals.Add(item.name)) fresh.Add(item);   // shown once per run

            if (fresh.Count > 0) yield return ShowCards(fresh);

            // The reveal comes first; the content (and with it its existing pop-up) is released afterwards.
            foreach (var f in u.features) unlocked.Add(f);
            Changed?.Invoke();
        }
        revealing = false;
    }

    IEnumerator ShowCards(List<RevealItem> items)
    {
        var cam = Camera.main;
        if (cam == null) yield break;
        // A fixed spot in front of the player (not following the head) so it can be read calmly.
        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        Vector3 center = cam.transform.position + forward * 1.5f;
        float spacing = 0.5f;

        var shown = new List<MED_WorldPanel>();
        for (int i = 0; i < items.Count; i++)
        {
            var card = MED_WorldPanel.Create("MED_RevealCard", new Vector2(0.9f, 0.42f), 0.5f, 0.34f,
                new Color(0.65f, 0.95f, 0.7f), new Color(0.35f, 0.75f, 1f));
            card.SetText("NEW: " + items[i].name, items[i].metaphor);
            float y = (items.Count - 1) * 0.5f * spacing - i * spacing;
            card.Root.transform.position = center + Vector3.up * (y - 0.1f);
            card.Root.AddComponent<MED_Billboard>();
            card.SetAlpha(0f);
            cards.Add(card);
            shown.Add(card);
        }

        const float fade = 0.4f;
        for (float t = 0f; t < fade; t += Time.deltaTime) { SetAlpha(shown, t / fade); yield return null; }
        SetAlpha(shown, 1f);
        yield return new WaitForSeconds(revealSeconds);
        for (float t = 0f; t < fade; t += Time.deltaTime) { SetAlpha(shown, 1f - t / fade); yield return null; }

        foreach (var c in shown) { cards.Remove(c); c.Destroy(); }
    }

    static void SetAlpha(List<MED_WorldPanel> panels, float a)
    {
        foreach (var p in panels) if (p.Root != null) p.SetAlpha(Mathf.Clamp01(a));
    }

    // ---------- NEXT GOAL sign ----------

    void BuildSign()
    {
        sign = MED_WorldPanel.Create("MED_NextGoalSign", new Vector2(2.8f, 0.9f), 0.9f, 1.0f,
            new Color(1f, 0.85f, 0.4f), new Color(1f, 0.75f, 0.2f));
        sign.Root.transform.position = signPosition;
        sign.Root.AddComponent<MED_Billboard>();
        sign.Root.SetActive(MED_AreaGate.PlayerInside);
        RefreshSign();
    }

    void RefreshSign()
    {
        if (sign != null)
        {
            string head = CurrentThreshold >= ThresholdCount ? "CLINIC COMPLETE" : $"NEXT GOAL  ({CurrentThreshold + 1}/{ThresholdCount})";
            sign.SetText(head, NextGoal);
        }
        GoalChanged?.Invoke();
    }

    // ---------- Applying state ----------

    void ApplyAll()
    {
        ApplySandboxes();
        RefreshSign();
        Changed?.Invoke();
    }

    void ApplySandboxes()
    {
        if (tray != null) tray.SetActive(CurrentThreshold >= 1);
    }

    void AttachSandbox(MED_Building b)
    {
        if (b == null) return;
        var sterilizer = b.GetComponentInChildren<MED_Sterilizer>();
        if (sterilizer != null) MED_SterilizerShowcase.Attach(sterilizer);
        var planter = b.GetComponentInChildren<MED_HerbPlanter>();
        if (planter != null) MED_PlanterDemo.Attach(planter);
    }
}
