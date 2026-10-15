using UnityEngine;

// Extra dynamics for the Water Barrel (besides the bucket harvest done by MED_Harvestable):
// passive rain collection and a POUR button that tips 1 Water from the player into the barrel.
[RequireComponent(typeof(MED_Harvestable))]
public class MED_BarrelController : MonoBehaviour
{
    [SerializeField] float rainRate = 1f / 6f;   // WaterLevel units per second
    [SerializeField] float maxUseDistance = 3.5f;
    [SerializeField] MED_ActionButton pourButton;
    [SerializeField] Transform messageAnchor;

    MED_Harvestable barrel;
    MED_PlayerResources player;

    MED_Stat Level => barrel.Stat;
    Vector3 MessagePosition => messageAnchor != null ? messageAnchor.position : transform.position + Vector3.up * 1.2f;

    void Awake() => barrel = GetComponent<MED_Harvestable>();

    void OnEnable()
    {
        if (barrel == null) barrel = GetComponent<MED_Harvestable>();
        barrel.Stat.OnChanged += OnLevelChanged;
        if (pourButton != null) pourButton.Pressed += OnPourPressed;
        Bind();
    }

    void OnDisable()
    {
        barrel.Stat.OnChanged -= OnLevelChanged;
        if (pourButton != null) pourButton.Pressed -= OnPourPressed;
        if (player != null) player.OnResourceChanged -= OnResourceChanged;
        player = null;
    }

    void Start()
    {
        Bind();
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
        if (type == MED_ResourceType.Water) RefreshButtons();
    }

    void OnLevelChanged(MED_Stat _) => RefreshButtons();

    void Update()
    {
        // Euler integration of WaterLevel: level += rainRate * dt, only while the barrel is not full.
        if (Level.Value < Level.Max) Level.Add(rainRate * Time.deltaTime);
    }

    void OnPourPressed() => TryPour();

    public bool TryPour()
    {
        if (!MED_PlayerProximity.IsNear(transform.position, maxUseDistance)) return Fail("Too far");
        Bind();
        if (Level.Value >= Level.Max) return Fail("Barrel is full");
        if (player == null || player.Get(MED_ResourceType.Water) < 1) return Fail("No Water");
        player.TrySpend(MED_ResourceType.Water, 1);
        Level.Add(1f);
        MED_FloatingText.Spawn(MessagePosition, "+1 Poured", new Color(0.5f, 0.8f, 1f));
        return true;
    }

    void RefreshButtons()
    {
        if (pourButton == null) return;
        pourButton.SetAvailable(Level.Value < Level.Max && player != null && player.Get(MED_ResourceType.Water) >= 1);
    }

    bool Fail(string message)
    {
        MED_FloatingText.Spawn(MessagePosition, message, new Color(1f, 0.6f, 0.4f), 0.5f);
        return false;
    }
}
