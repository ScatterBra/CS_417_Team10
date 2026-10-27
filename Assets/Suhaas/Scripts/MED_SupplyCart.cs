using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// A supply cart that follows the MED_DeliveryPath: Arriving (path start to bay), Waiting at the bay (UNLOAD gives
// the player one unit of cargo per press), Leaving (turn around, then follow the path backward and disappear).
public class MED_SupplyCart : MonoBehaviour
{
    public static event Action<MED_SupplyCart,MED_PlayerResources,MED_ResourceType,bool> UnloadCommitted;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetEvidenceListeners()=>UnloadCommitted=null;
    public enum CartState { Arriving, Waiting, Leaving }

    static readonly MED_ResourceType[] CargoTypes =
        { MED_ResourceType.Herbs, MED_ResourceType.Water, MED_ResourceType.Alcohol, MED_ResourceType.Gauze };

    [SerializeField] MED_Stat routeProgress = new MED_Stat("RouteProgress", 0f, 0f, 1f);
    [SerializeField] MED_Stat cargoRemaining = new MED_Stat("CargoRemaining", 0f, 0f, 6f);
    [SerializeField] float speed = 1.2f;            // meters per second along the path
    [SerializeField] float waitSeconds = 90f;
    [SerializeField] float turnSeconds = 0.5f;
    [SerializeField] int minCargo = 4;
    [SerializeField] int maxCargo = 6;
    [SerializeField] float maxUseDistance = 3.5f;
    [SerializeField] MED_ActionButton[] unloadButtons;
    [SerializeField] Transform[] wheels;
    [SerializeField] float wheelRadius = 0.22f;
    [SerializeField] GameObject[] crates;           // visible crate i = i < CargoRemaining
    [SerializeField] MED_StatBar cargoBar;
    [SerializeField] TextMeshPro nextLabel;
    [SerializeField] Transform messageAnchor;

    MED_DeliveryPath path;
    MED_PlayerResources player;
    readonly List<MED_ResourceType> cargo = new List<MED_ResourceType>();
    float distance;
    float waitTimer;
    float turnTimer;
    float nearTimer;
    float wheelAngle;
    bool turning;
    bool playerNear;
    bool finished;
    Quaternion turnStart;

    public CartState State { get; private set; } = CartState.Arriving;
    public MED_Stat RouteProgress => routeProgress;
    public MED_Stat CargoRemaining => cargoRemaining;
    public event Action StateChanged;
    public event Action<MED_SupplyCart> Finished;

    Vector3 MessagePosition => messageAnchor != null ? messageAnchor.position : transform.position + Vector3.up * 1.5f;

    /// <summary>Called by the spawner right after instantiating: rolls the cargo and starts the arrival.</summary>
    public void Init(MED_DeliveryPath deliveryPath)
    {
        path = deliveryPath;
        RollCargo();
        distance = 0f;
        State = CartState.Arriving;
        Place();
        RefreshAll();
    }

    void RollCargo()
    {
        // 4-6 units made of random types; grouped by type so "Next: ..." stays stable while unloading.
        int total = UnityEngine.Random.Range(minCargo, maxCargo + 1);
        var counts = new int[CargoTypes.Length];
        for (int i = 0; i < total; i++) counts[UnityEngine.Random.Range(0, CargoTypes.Length)]++;
        cargo.Clear();
        for (int t = 0; t < CargoTypes.Length; t++)
            for (int k = 0; k < counts[t]; k++) cargo.Add(CargoTypes[t]);
        cargoRemaining.Max = total;
        cargoRemaining.Set(total);
    }

    void OnEnable()
    {
        cargoRemaining.OnChanged += OnCargoChanged;
        foreach (var b in unloadButtons) if (b != null) b.Pressed += OnUnloadPressed;
    }

    void OnDisable()
    {
        cargoRemaining.OnChanged -= OnCargoChanged;
        foreach (var b in unloadButtons) if (b != null) b.Pressed -= OnUnloadPressed;
    }

    void Start()
    {
        if (cargoBar != null) cargoBar.Bind(cargoRemaining);
        RefreshAll();
    }

    MED_PlayerResources Player
    {
        get
        {
            if (player == null) player = FindAnyObjectByType<MED_PlayerResources>();
            return player;
        }
    }

    // ---------- Simulation ----------

    void Update()
    {
        if (path == null || finished) return;
        float dt = Time.deltaTime;

        switch (State)
        {
            case CartState.Arriving:
                // Euler integration along the curve: distance += speed * dt.
                Advance(speed * dt);
                routeProgress.Set(distance / Mathf.Max(0.01f, path.TotalLength));
                if (distance >= path.TotalLength)
                {
                    distance = path.TotalLength;
                    Place();
                    waitTimer = 0f;
                    SetState(CartState.Waiting);
                }
                break;

            case CartState.Waiting:
                var learning=DEF_ClinicTraining.For(this);
                if(learning==null||!learning.ProtectLearningCart)waitTimer += dt;
                if (waitTimer >= waitSeconds) BeginLeaving();
                // Button color depends on the player's distance, so re-check a few times per second (not every frame).
                nearTimer -= dt;
                if (nearTimer <= 0f)
                {
                    nearTimer = 0.2f;
                    bool near = MED_PlayerProximity.IsNear(transform.position, maxUseDistance);
                    if (near != playerNear) { playerNear = near; RefreshButtons(); }
                }
                break;

            case CartState.Leaving:
                if (turning)
                {
                    turnTimer += dt;
                    float k = Mathf.Clamp01(turnTimer / turnSeconds);
                    transform.rotation = turnStart * Quaternion.Euler(0f, 180f * k, 0f);
                    if (k >= 1f) turning = false;
                    break;
                }
                Advance(-speed * dt);
                routeProgress.Set(distance / Mathf.Max(0.01f, path.TotalLength));
                if (distance <= 0f) Finish();
                break;
        }
    }

    void Advance(float step)
    {
        float prev = distance;
        distance = Mathf.Clamp(distance + step, 0f, path.TotalLength);
        SpinWheels(distance - prev);
        Place();
    }

    void Place()
    {
        var pos = path.GetPointAtDistance(distance);
        var heading = path.GetTangentAtDistance(distance);
        if (State == CartState.Leaving) heading = -heading;
        heading.y = 0f;
        transform.position = pos;
        if (heading.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(heading, Vector3.up);
    }

    void SpinWheels(float traveled)
    {
        // Wheels turn by (distance / radius) radians, so they roll without slipping.
        wheelAngle += traveled / wheelRadius * Mathf.Rad2Deg;
        foreach (var w in wheels)
            if (w != null) w.localRotation = Quaternion.Euler(wheelAngle, 0f, 0f);
    }

    void BeginLeaving()
    {
        if (State == CartState.Leaving) return;
        if (cargoRemaining.Value > 0.5f)
            MED_FloatingText.Spawn(MessagePosition, $"Cart left with {Mathf.RoundToInt(cargoRemaining.Value)} supplies", new Color(1f, 0.85f, 0.4f), 0.5f);
        turning = true;
        turnTimer = 0f;
        turnStart = transform.rotation;
        SetState(CartState.Leaving);
    }

    void Finish()
    {
        if (finished) return;
        finished = true;
        Finished?.Invoke(this);
        Destroy(gameObject);
    }

    void SetState(CartState state)
    {
        State = state;
        playerNear = state == CartState.Waiting && MED_PlayerProximity.IsNear(transform.position, maxUseDistance);
        RefreshButtons();
        StateChanged?.Invoke();
    }

    // ---------- Unloading ----------

    void OnUnloadPressed() => TryUnload();

    public bool TryUnload()
    {
        if(!DEF_ClinicTraining.Allow(this,8,"Clinic action"))return false;
        if (!MED_PlayerProximity.IsNear(transform.position, maxUseDistance)) return Fail("Too far");
        if (State != CartState.Waiting) return Fail(State == CartState.Arriving ? "Cart is still arriving" : "Cart is leaving");
        if (cargo.Count == 0 || cargoRemaining.Value < 0.5f) return Fail("Cart is empty");
        if (Player == null) return Fail("No player");

        var type = cargo[0];
        cargo.RemoveAt(0);
        Player.Add(type, 1);
        cargoRemaining.Add(-1f);
        if(UnloadCommitted!=null)foreach(Action<MED_SupplyCart,MED_PlayerResources,MED_ResourceType,bool> listener in UnloadCommitted.GetInvocationList())try{listener(this,Player,type,cargo.Count==0);}catch(Exception e){Debug.LogException(e);}
        MED_FloatingText.Spawn(MessagePosition, $"+1 {type}", new Color(0.6f, 1f, 0.6f));
        if (cargo.Count == 0) BeginLeaving();
        return true;
    }

    void OnCargoChanged(MED_Stat _) => RefreshAll();

    // ---------- Visuals ----------

    void RefreshAll()
    {
        int remaining = Mathf.RoundToInt(cargoRemaining.Value);
        for (int i = 0; i < crates.Length; i++)
            if (crates[i] != null) crates[i].SetActive(i < remaining);
        if (nextLabel != null)
            nextLabel.text = cargo.Count > 0 ? $"Next: {cargo[0]}" : "Empty";
        RefreshButtons();
    }

    void RefreshButtons()
    {
        bool available = State == CartState.Waiting && cargo.Count > 0 && playerNear;
        foreach (var b in unloadButtons) if (b != null) b.SetAvailable(available);
    }

    bool Fail(string message)
    {
        MED_FloatingText.Spawn(MessagePosition, message, new Color(1f, 0.6f, 0.4f), 0.5f);
        return false;
    }
}
