using System;
using System.Collections.Generic;
using UnityEngine;

public enum MED_Achievement
{
    Apothecary,
    SupplyRunner,
    GreenThumb,
    StockedUp,
    FullRecovery,
}

/// <summary>
/// Achievements: when a condition is first met, a gold trophy appears on the Clinic's trophy shelf with a label and a
/// floating "Achievement: name" text. Each trophy appears once per run (state lives in this scene object, so it is
/// lost when Play stops). Event-driven: the Clinic's own scripts raise static events, nothing polls.
///   Apothecary    - craft all 5 Prep Bench recipes at least once
///   Supply Runner - empty 3 supply carts by unloading them
///   Green Thumb   - harvest a ripe Herb Planter with the Sickle
///   Stocked Up    - fill the Medicine Cabinet to full
///   Full Recovery - reach 100 health 2 separate times (drop below 100 in between, e.g. RESET HEALTH)
/// None of these repeats a progression threshold condition.
/// </summary>
public class MED_AchievementManager : MonoBehaviour
{
    public const int Count = 5;
    const int RecipesNeeded = 5;
    const int CartsNeeded = 3;
    const int RecoveriesNeeded = 2;

    public static MED_AchievementManager Instance { get; private set; }
    /// <summary>Raised when an achievement is earned (a trophy appears).</summary>
    public static event Action<MED_Achievement> Earned;

    [SerializeField] MED_Trophy trophyPrefab;
    [SerializeField] Vector3 shelfPosition = new Vector3(7.2f, 0f, 3.5f);
    [SerializeField] float slotSpacing = 0.65f;

    readonly HashSet<MED_Achievement> earned = new HashSet<MED_Achievement>();
    readonly Dictionary<MED_Achievement, MED_Trophy> trophies = new Dictionary<MED_Achievement, MED_Trophy>();
    readonly HashSet<string> craftedRecipes = new HashSet<string>();
    GameObject shelf;
    MED_PlayerResources player;
    int cartsEmptied, recoveries, lastHealth;

    public int EarnedCount => earned.Count;
    public static bool IsEarned(MED_Achievement a) => Instance != null && Instance.earned.Contains(a);

    public static string TitleOf(MED_Achievement a)
    {
        switch (a)
        {
            case MED_Achievement.Apothecary: return "Apothecary";
            case MED_Achievement.SupplyRunner: return "Supply Runner";
            case MED_Achievement.GreenThumb: return "Green Thumb";
            case MED_Achievement.StockedUp: return "Stocked Up";
            default: return "Full Recovery";
        }
    }

    static string HintOf(MED_Achievement a)
    {
        switch (a)
        {
            case MED_Achievement.Apothecary: return "all 5 recipes";
            case MED_Achievement.SupplyRunner: return "3 carts unloaded";
            case MED_Achievement.GreenThumb: return "ripe planter harvested";
            case MED_Achievement.StockedUp: return "cabinet full";
            default: return "healed to 100 twice";
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        Earned = null;
    }

    void Awake() => Instance = this;

    void Start()
    {
        BuildShelf();
        BindPlayer();
    }

    void OnEnable()
    {
        MED_PrepBench.Crafted += OnCrafted;
        MED_SupplyCart.CartEmptied += OnCartEmptied;
        MED_HerbPlanter.Harvested += OnPlanterHarvested;
        MED_MedicineCabinet.Filled += OnCabinetFilled;
    }

    void OnDisable()
    {
        MED_PrepBench.Crafted -= OnCrafted;
        MED_SupplyCart.CartEmptied -= OnCartEmptied;
        MED_HerbPlanter.Harvested -= OnPlanterHarvested;
        MED_MedicineCabinet.Filled -= OnCabinetFilled;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (player != null) player.OnResourceChanged -= OnResourceChanged;
        if (shelf != null) Destroy(shelf);
    }

    void Update()
    {
        if (player == null) BindPlayer();
    }

    void BindPlayer()
    {
        if (player != null) return;
        player = FindAnyObjectByType<MED_PlayerResources>();
        if (player == null) return;
        lastHealth = player.Health;
        player.OnResourceChanged += OnResourceChanged;
    }

    // ---------- Conditions ----------

    void OnCrafted(MED_Recipe recipe)
    {
        if (recipe == null) return;
        craftedRecipes.Add(recipe.name);
        if (craftedRecipes.Count >= RecipesNeeded) Grant(MED_Achievement.Apothecary);
    }

    void OnCartEmptied()
    {
        if (++cartsEmptied >= CartsNeeded) Grant(MED_Achievement.SupplyRunner);
    }

    void OnPlanterHarvested() => Grant(MED_Achievement.GreenThumb);
    void OnCabinetFilled() => Grant(MED_Achievement.StockedUp);

    void OnResourceChanged(MED_ResourceType type, int amount)
    {
        if (type != MED_ResourceType.Health) return;
        // A recovery counts when health climbs up to the maximum from below it.
        if (amount >= player.MaxHealth && lastHealth < player.MaxHealth && ++recoveries >= RecoveriesNeeded)
            Grant(MED_Achievement.FullRecovery);
        lastHealth = amount;
    }

    // ---------- Granting ----------

    /// <summary>Earns the achievement (once) and makes its trophy appear. Used by the conditions and the test button.</summary>
    public void Grant(MED_Achievement a)
    {
        if (!earned.Add(a)) return;
        string title = TitleOf(a);
        if (trophyPrefab != null)
        {
            var slot = SlotPosition((int)a);
            var t = Instantiate(trophyPrefab, slot, Quaternion.identity);
            t.Show(a, title, HintOf(a));
            trophies[a] = t;
        }
        MED_PlayerHUD.ShowMessage("Achievement: " + title, 4f);
        Earned?.Invoke(a);
    }

    /// <summary>Test hook (GRANT NEXT ACHIEVEMENT): earns the first achievement not yet earned.</summary>
    public void GrantNext()
    {
        for (int i = 0; i < Count; i++)
            if (!earned.Contains((MED_Achievement)i)) { Grant((MED_Achievement)i); return; }
    }

    /// <summary>Test hook (RESET ACHIEVEMENTS): removes every trophy and clears all counters.</summary>
    public void ResetAchievements()
    {
        foreach (var t in trophies.Values) if (t != null) Destroy(t.gameObject);
        trophies.Clear();
        earned.Clear();
        craftedRecipes.Clear();
        cartsEmptied = 0;
        recoveries = 0;
        lastHealth = player != null ? player.Health : 0;
    }

    // ---------- Trophy shelf (built from primitives, against the east wall) ----------

    Vector3 SlotPosition(int index) => shelfPosition + new Vector3(-0.02f, 1.08f, (index - (Count - 1) * 0.5f) * slotSpacing);

    void BuildShelf()
    {
        shelf = new GameObject("MED_TrophyShelf");
        shelf.transform.position = shelfPosition;
        var wood = new Color(0.45f, 0.32f, 0.2f);
        float length = Count * slotSpacing + 0.3f;
        MED_SandboxUtil.Prim(PrimitiveType.Cube, "Board", shelf.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.45f, 0.06f, length), wood, false);
        MED_SandboxUtil.Prim(PrimitiveType.Cube, "BackPanel", shelf.transform, new Vector3(0.2f, 1.5f, 0f), new Vector3(0.04f, 0.9f, length), new Color(0.3f, 0.22f, 0.15f), false);
        foreach (float z in new[] { -length * 0.45f, 0f, length * 0.45f })
            MED_SandboxUtil.Prim(PrimitiveType.Cube, "Support", shelf.transform, new Vector3(0f, 0.52f, z), new Vector3(0.4f, 1.04f, 0.06f), wood, false);
        MED_SandboxUtil.Label(shelf.transform, "TROPHY SHELF", new Vector3(-0.05f, 1.95f, 0f), 0.6f);
    }
}
