using UnityEngine;

// Placed Medicine Cabinet: stock Medicine into it and take it back out.
public class MED_MedicineCabinet : MonoBehaviour
{
    [SerializeField] MED_Stat stock = new MED_Stat("CabinetStock", 0f, 0f, 10f);
    [SerializeField] MED_ActionButton stockButton;
    [SerializeField] MED_ActionButton takeButton;
    [SerializeField] GameObject[] bottles;
    [SerializeField] MED_StatBar statBar;
    [SerializeField] Transform messageAnchor;
    [SerializeField] float maxUseDistance = 3.5f;

    [SerializeField] MED_PlayerResources player;
    readonly MED_MedicineStore evidenceStock=new();
    bool supplyBusy,writingStock;
    bool wasFull;
    void SetStock(float value)
    {
        writingStock=true;
        try {stock.Set(value);}catch(System.Exception e){Debug.LogException(e);}
        finally {writingStock=false;evidenceStock.Sync(stock.Value);}
    }

    public MED_Stat Stock => stock;

    /// <summary>Raised when stocking fills a cabinet to its maximum (used by the Stocked Up achievement).</summary>
    public static event System.Action Filled;
    public static event System.Action<MED_MedicineCabinet,MED_PlayerResources> FilledCommitted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() {Filled=null;FilledCommitted=null;}

    Vector3 MessagePosition => messageAnchor != null ? messageAnchor.position : transform.position + Vector3.up * 2f;

    void OnEnable()
    {
        stock.OnChanged += OnStockChanged;
        if (stockButton != null) stockButton.Pressed += OnStockPressed;
        if (takeButton != null) takeButton.Pressed += OnTakePressed;
        Bind();
    }

    void OnDisable()
    {
        stock.OnChanged -= OnStockChanged;
        if (stockButton != null) stockButton.Pressed -= OnStockPressed;
        if (takeButton != null) takeButton.Pressed -= OnTakePressed;
        if (player != null) player.OnResourceChanged -= OnResourceChanged;
    }

    void Start()
    {
        if (statBar != null) statBar.Bind(stock);
        Bind();
        Refresh();
    }

    void Bind()
    {
        if (player == null) player = FindAnyObjectByType<MED_PlayerResources>();
        if (player != null) {player.OnResourceChanged -= OnResourceChanged;player.OnResourceChanged += OnResourceChanged;}
    }

    void OnResourceChanged(MED_ResourceType type, int amount)
    {
        if (type == MED_ResourceType.Medicine) Refresh();
    }

    void OnStockChanged(MED_Stat _) {if(!writingStock)evidenceStock.Invalidate(stock.Value);Refresh();}

    // T5 sandbox: once the Clinic is complete, packing the cabinet full makes the bottles gleam ("Fully stocked!").
    void Refresh()
    {
        bool full = stock.Value >= stock.Max;
        if (full && !wasFull && MED_ProgressionManager.SandboxActive(5)) StartCoroutine(Gleam());
        wasFull = full;
        int visible = Mathf.CeilToInt(stock.Value / 2f);
        for (int i = 0; bottles!=null && i < bottles.Length; i++)
            if (bottles[i] != null) bottles[i].SetActive(i < visible);
        if (stockButton != null) stockButton.SetAvailable(player != null && player.Get(MED_ResourceType.Medicine) >= 1 && stock.Value < stock.Max);
        if (takeButton != null) takeButton.SetAvailable(stock.Value >= 1f);
    }

    System.Collections.IEnumerator Gleam()
    {
        MED_FloatingText.Spawn(MessagePosition, "Fully stocked!", new Color(1f, 0.9f, 0.4f));
        var block = new MaterialPropertyBlock();
        var gold = new Color(1f, 0.85f, 0.3f);
        const float duration = 1.2f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = Mathf.Sin(t / duration * Mathf.PI);
            foreach (var b in bottles)
            {
                if (b == null) continue;
                foreach (var r in b.GetComponentsInChildren<Renderer>())
                {
                    r.GetPropertyBlock(block);
                    block.SetColor("_EmissionColor", gold * (2f * k));
                    block.SetColor("_BaseColor", Color.Lerp(new Color(0.8f, 0.9f, 0.9f), gold, k));
                    r.SetPropertyBlock(block);
                }
            }
            yield return null;
        }
        foreach (var b in bottles)
            if (b != null) foreach (var r in b.GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(null);
    }

    void OnStockPressed() => TryStock();
    void OnTakePressed() => TryTake();

    public bool TryStock()
    {
        if(supplyBusy)return false;
        if (!MED_PlayerProximity.IsNear(transform.position, maxUseDistance)) return Fail("Too far");
        Bind();
        if (stock.Value + 1f > stock.Max) return Fail("Cabinet full");
        if(player==null)return Fail("Your Clinic inventory is unavailable");
        if(player.Get(MED_ResourceType.Medicine)<1)return Fail("No Medicine");
        supplyBusy=true;bool accepted;
        try {evidenceStock.Sync(stock.Value);accepted=player.TransferMedicine(unit=>{evidenceStock.Add(unit);SetStock(stock.Value+1f);});}
        finally {supplyBusy=false;}
        if(!accepted)return Fail("Stock failed: Medicine unavailable. Gather Medicine and retry.");
        MED_FloatingText.Spawn(MessagePosition, "+1 Stocked", new Color(0.6f, 1f, 0.6f));
        if (stock.Value >= stock.Max) { FilledCommitted?.Invoke(this,player); Filled?.Invoke(); }
        return true;
    }

    public bool TryTake()
    {
        if(supplyBusy)return false;
        if (!MED_PlayerProximity.IsNear(transform.position, maxUseDistance)) return Fail("Too far");
        Bind();
        if (stock.Value < 1f) return Fail("Cabinet empty");
        if (player == null) return Fail("No player");
        supplyBusy=true;bool accepted;
        try {evidenceStock.Sync(stock.Value);accepted=player.ReceiveMedicine(()=>{var unit=evidenceStock.Take();SetStock(stock.Value-1f);return unit;});}
        finally {supplyBusy=false;}
        if(!accepted)return Fail("Take failed: inventory busy or full. Retry when available.");
        MED_FloatingText.Spawn(MessagePosition, "+1 Medicine", new Color(0.6f, 1f, 0.6f));
        return true;
    }

    bool Fail(string message)
    {
        return MED_Messages.Fail(MessagePosition, message);
    }
}
