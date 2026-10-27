using UnityEngine;

// Placed Treatment Station (2x2 footprint: bed in the back row, green treatment pad in the front row).
// Caretaking: REFILL +1 spends Medicine into StoredMedicine. TREAT heals the player 20 HP per dose while they
// stand on the pad.
public class MED_TreatmentStation : MonoBehaviour
{
    public enum StationState { Idle, Treating }

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] MED_Stat storedMedicine = new MED_Stat("StoredMedicine", 0f, 0f, 10f);
    [SerializeField] MED_Stat treatProgress = new MED_Stat("TreatProgress", 0f, 0f, 1f);
    [SerializeField] float doseDuration = 1.5f;
    [SerializeField] int healPerDose = 20;
    [SerializeField] float maxUseDistance = 3.5f;
    [Tooltip("Pad rectangle in this object's local XZ plane (center x, center z).")]
    [SerializeField] Vector2 padCenter = new Vector2(0f, -0.5f);
    [SerializeField] Vector2 padSize = new Vector2(2f, 1f);
    [SerializeField] MED_ActionButton refillButton;
    [SerializeField] MED_ActionButton treatButton;
    [SerializeField] MED_StatBar storedBar;
    [SerializeField] MED_StatBar progressBar;
    [SerializeField] Renderer padRenderer;
    [SerializeField] Renderer glowRenderer;
    [SerializeField] Transform ivLiquid;
    [SerializeField] float ivFullHeight = 0.24f;
    [SerializeField] Transform messageAnchor;

    [SerializeField] MED_PlayerResources player;
    readonly MED_MedicineStore evidenceStock=new();
    bool supplyBusy, writingStock;
    MED_PlayerResources dosePatient;
    int doseGeneration;
    public event System.Action<MED_TreatmentStation,MED_PlayerResources,MED_MedicineUnit,int,int> DoseCommitted;

    void SetStock(float value)
    {
        writingStock=true;
        try {storedMedicine.Set(value);}catch(System.Exception e){Debug.LogException(e);}
        finally {writingStock=false;evidenceStock.Sync(storedMedicine.Value);}
    }
    MaterialPropertyBlock padBlock, glowBlock;
    bool playerOnPad;

    public StationState State { get; private set; } = StationState.Idle;
    public MED_Stat StoredMedicine => storedMedicine;
    public MED_Stat TreatProgress => treatProgress;
    public bool PlayerOnPad => playerOnPad;

    /// <summary>Raised when any station is refilled / gives a dose (used by the progression thresholds).</summary>
    public static event System.Action Stocked;
    public static event System.Action DoseGiven;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Stocked = null;
        DoseGiven = null;
    }

    Vector3 MessagePosition => messageAnchor != null ? messageAnchor.position : transform.position + Vector3.up * 2f;

    void OnEnable()
    {
        storedMedicine.OnChanged += OnStoredChanged;
        MED_ProgressionManager.Changed += RefreshButtons;
        if (refillButton != null) refillButton.Pressed += OnRefillPressed;
        if (treatButton != null) treatButton.Pressed += OnTreatPressed;
        Bind();
    }

    void OnDisable()
    {
        StopTreating(null);
        storedMedicine.OnChanged -= OnStoredChanged;
        MED_ProgressionManager.Changed -= RefreshButtons;
        if (refillButton != null) refillButton.Pressed -= OnRefillPressed;
        if (treatButton != null) treatButton.Pressed -= OnTreatPressed;
        if (player != null) player.OnResourceChanged -= OnResourceChanged;
    }

    void Start()
    {
        padBlock = new MaterialPropertyBlock();
        glowBlock = new MaterialPropertyBlock();
        if (storedBar != null) { storedBar.Bind(storedMedicine); storedBar.SetAlpha(0.85f); }
        if (progressBar != null) { progressBar.Bind(treatProgress); progressBar.SetAlpha(0f); }
        if (glowRenderer != null) glowRenderer.enabled = false;
        Bind();
        UpdateIvLiquid();
        UpdatePadVisual();
        RefreshButtons();
    }

    void Bind()
    {
        if (player == null) player = FindAnyObjectByType<MED_PlayerResources>();
        if (player != null) {player.OnResourceChanged -= OnResourceChanged;player.OnResourceChanged += OnResourceChanged;}
    }

    void OnResourceChanged(MED_ResourceType type, int amount)
    {
        if (type == MED_ResourceType.Medicine || type == MED_ResourceType.Health) RefreshButtons();
    }

    void OnStoredChanged(MED_Stat _)
    {
        if(!writingStock)evidenceStock.Invalidate(storedMedicine.Value);
        UpdateIvLiquid();
        RefreshButtons();
    }

    // ---------- Per-frame: pad occupancy (rectangle math on the camera) and the dose timer ----------

    void Update()
    {
        bool on = IsPlayerOnPad();
        if (on != playerOnPad)
        {
            playerOnPad = on;
            UpdatePadVisual();
            RefreshButtons();
        }
        else if (SandboxGlowing) UpdatePadVisual();   // T2 sandbox: keep the glow pulsing

        if (State != StationState.Treating) return;
        if (!playerOnPad) { CancelDose(); return; }

        // Euler integration of the TreatProgress stat: progress += (1 / doseDuration) * dt.
        treatProgress.Add((1f / doseDuration) * Time.deltaTime);
        PulseGlow();
        if (treatProgress.Value >= 1f) CompleteDose();
    }

    bool IsPlayerOnPad()
    {
        var cam = Camera.main;
        if (cam == null) return false;
        var local = transform.InverseTransformPoint(cam.transform.position);
        return Mathf.Abs(local.x - padCenter.x) <= padSize.x * 0.5f && Mathf.Abs(local.z - padCenter.y) <= padSize.y * 0.5f;
    }

    // ---------- Actions ----------

    void OnRefillPressed() => TryRefill();
    void OnTreatPressed() => TryTreat();

    public bool TryRefill()
    {
        if(!DEF_ClinicTraining.Allow(this,10,"Clinic action"))return false;
        if(supplyBusy)return false;
        if (!MED_PlayerProximity.IsNear(transform.position, maxUseDistance)) return Fail("Too far");
        Bind();
        if (storedMedicine.Value + 1f > storedMedicine.Max) return Fail("Station is full");
        if (player == null || player.Get(MED_ResourceType.Medicine) < 1) return Fail("No Medicine");
        supplyBusy=true;
        bool accepted;
        try {evidenceStock.Sync(storedMedicine.Value);accepted=player.TransferMedicine(unit=>{evidenceStock.Add(unit);SetStock(storedMedicine.Value+1f);}, DEF_ClinicTraining.For(this)!=null);}
        finally {supplyBusy=false;}
        if(!accepted)return Fail("Refill requires fresh Medicine crafted this run. Craft another Medicine; stale inventory is skipped.");
        MED_FloatingText.Spawn(MessagePosition, "+1 Stored", new Color(0.6f, 1f, 0.6f));
        Stocked?.Invoke();
        return true;
    }

    public bool TryTreat()
    {
        if(!DEF_ClinicTraining.Allow(this,10,"Treatment"))return false;
        if (supplyBusy || State == StationState.Treating) return false;
        if (MED_ProgressionManager.IsLocked(MED_Feature.TreatButton)) return Fail(MED_Messages.Locked(MED_Feature.TreatButton));
        if (!MED_PlayerProximity.IsNear(transform.position, maxUseDistance)) return Fail("Too far");
        Bind();
        if (!playerOnPad) return Fail(MED_Messages.NotOnPad);
        if (player == null || player.IsHealthFull) return Fail(MED_Messages.HealthFull);
        if (storedMedicine.Value < 1f) return Fail(MED_Messages.StationEmpty);
        StartDose();
        return true;
    }

    void StartDose()
    {
        dosePatient=player;doseGeneration=player.EvidenceGeneration;
        State = StationState.Treating;
        treatProgress.Set(0f);
        if (progressBar != null) progressBar.SetAlpha(0.9f);
        if (glowRenderer != null) glowRenderer.enabled = true;
        RefreshButtons();
    }

    void CompleteDose()
    {
        if(supplyBusy || State!=StationState.Treating || !isActiveAndEnabled || !playerOnPad || dosePatient!=player)return;
        // Re-check at completion: never spend medicine when health is already full.
        if (player == null || player.IsHealthFull) { StopTreating("Fully healed"); return; }
        if (storedMedicine.Value < 1f) { StopTreating(MED_Messages.StationEmpty, true); return; }

        var patient=player;int generation=doseGeneration;MED_MedicineUnit unit=null;int before,after;
        supplyBusy=true;
        bool healed;
        try
        {
            evidenceStock.Sync(storedMedicine.Value);
            healed=patient.ApplyDose(healPerDose,()=>{unit=evidenceStock.Take();SetStock(storedMedicine.Value-1f);},out before,out after);
        }
        finally {supplyBusy=false;}
        if(!healed){StopTreating("Treatment needs positive healing and an available patient.");return;}
        if(generation==patient.EvidenceGeneration && patient.IsCurrent(unit) && DoseCommitted!=null)
            foreach(System.Action<MED_TreatmentStation,MED_PlayerResources,MED_MedicineUnit,int,int> listener in DoseCommitted.GetInvocationList())
                try {listener(this,patient,unit,before,after);}catch(System.Exception e){Debug.LogException(e);}
        MED_FloatingText.Spawn(MessagePosition, $"+{after - before} HP", new Color(0.6f, 1f, 0.6f));
        treatProgress.Set(0f);
        if(generation==patient.EvidenceGeneration && patient.IsCurrent(unit)) DoseGiven?.Invoke();

        if (!player.IsHealthFull && storedMedicine.Value >= 1f && playerOnPad) return; // next dose starts automatically

        if (player.IsHealthFull) StopTreating("Fully healed");
        else if (storedMedicine.Value < 1f) StopTreating(MED_Messages.RanOutMidTreatment, true);
        else StopTreating(null);
    }

    void CancelDose()
    {
        StopTreating(MED_Messages.TreatCancelled, true);
    }

    void StopTreating(string message, bool isCatch = false)
    {
        State = StationState.Idle;
        treatProgress.Set(0f);
        if (progressBar != null) progressBar.SetAlpha(0f);
        if (glowRenderer != null) glowRenderer.enabled = false;
        if (message != null && isCatch) MED_Messages.Fail(MessagePosition, message);
        else if (message != null) MED_FloatingText.Spawn(MessagePosition, message, new Color(1f, 0.85f, 0.4f), 0.5f);
        RefreshButtons();
    }

    // ---------- Visuals ----------

    void RefreshButtons()
    {
        if (refillButton != null)
            refillButton.SetAvailable(player != null && player.Get(MED_ResourceType.Medicine) >= 1 && storedMedicine.Value < storedMedicine.Max);
        if (treatButton != null)
            treatButton.SetAvailable(!MED_ProgressionManager.IsLocked(MED_Feature.TreatButton) && State == StationState.Idle && playerOnPad && player != null && !player.IsHealthFull && storedMedicine.Value >= 1f);
    }

    void UpdateIvLiquid()
    {
        if (ivLiquid == null) return;
        float h = Mathf.Max(0.001f, ivFullHeight * storedMedicine.Normalized);
        var s = ivLiquid.localScale; s.y = h; ivLiquid.localScale = s;
        var p = ivLiquid.localPosition; p.y = -ivFullHeight * 0.5f + h * 0.5f; ivLiquid.localPosition = p;
    }

    // T2 sandbox: once the Treatment Station threshold is cleared, the pad glows (pulses bright) only while the
    // player stands on it AND there is stock. Nothing else changes, so there is no stakes and no spam.
    bool SandboxGlowing => playerOnPad && storedMedicine.Value >= 1f && MED_ProgressionManager.SandboxActive(2);

    void UpdatePadVisual()
    {
        if (padRenderer == null) return;
        if (padBlock == null) padBlock = new MaterialPropertyBlock();
        padRenderer.GetPropertyBlock(padBlock);
        Color pad = playerOnPad ? new Color(0.45f, 1f, 0.6f) : new Color(0.15f, 0.55f, 0.3f);
        if (SandboxGlowing) pad = Color.Lerp(pad, new Color(0.8f, 1f, 0.9f), 0.5f + 0.5f * Mathf.Sin(Time.time * 5f));
        padBlock.SetColor(BaseColorId, pad);
        padRenderer.SetPropertyBlock(padBlock);
    }

    void PulseGlow()
    {
        if (glowRenderer == null) return;
        if (glowBlock == null) glowBlock = new MaterialPropertyBlock();
        float a = 0.2f + 0.2f * (0.5f + 0.5f * Mathf.Sin(Time.time * 6f));
        glowRenderer.GetPropertyBlock(glowBlock);
        glowBlock.SetColor(BaseColorId, new Color(0.5f, 1f, 0.7f, a));
        glowRenderer.SetPropertyBlock(glowBlock);
    }

    bool Fail(string message)
    {
        return MED_Messages.Fail(MessagePosition, message);
    }
}
