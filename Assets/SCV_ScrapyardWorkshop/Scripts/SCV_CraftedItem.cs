using UnityEngine;

public class SCV_CraftedItem : MonoBehaviour
{
    public enum ItemType
    {
        Ammo,
        RepairKit
    }

    [SerializeField] private ItemType itemType;

    public ItemType Type => itemType;
    [SerializeField] private SCV_ResourceManager explicitLegacyLedger;
    public SCV_ResourceManager SourceLedger { get; private set; }
    public long UnitId { get; private set; }
    // Felix's public source/reservation names share the same identity and claim.
    public SCV_ResourceManager SourceInventory => SourceLedger;
    private readonly object legacyReservation = new object();
    public bool IsReserved => Consumed || claimant != null;
    public void SetSourceInventory(SCV_ResourceManager sourceInventory) { BindLedger(sourceInventory); }
    public bool TryReserve() => !IsHeld && TryClaim(legacyReservation);
    public void ReleaseReservation() { Release(legacyReservation); }
    public long AmmoUnitId => UnitId; // Existing ammo callers retain their API.
    public bool IsHeld => GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>()?.isSelected ?? false;
    void Start() { if (explicitLegacyLedger != null && SourceLedger == null) BindLedger(explicitLegacyLedger); }
    internal bool CanRemoteConsume => !Consumed && claimant == null && gameObject.activeInHierarchy &&
        !IsHeld;
    public bool BindLedger(SCV_ResourceManager ledger)
    {
        if (ledger == null || SourceLedger != null || claimant != null || Consumed) return false;
        UnitId = ledger.RegisterRepresentation(this);
        if (UnitId == 0) return false;
        SourceLedger = ledger;
        return true;
    }

    // Per-instance claim survives disable/enable; only a newly crafted object resets it.
    private object claimant;
    public bool Consumed { get; private set; }
    internal bool TryClaim(object owner)
    {
        if (owner == null || Consumed || claimant != null || !gameObject.activeInHierarchy) return false;
        claimant = owner;
        return true;
    }
    internal void Release(object owner) { if (!Consumed && ReferenceEquals(claimant, owner)) claimant = null; }
    internal bool Commit(object owner)
    {
        if (Consumed || !ReferenceEquals(claimant, owner) || owner == null) return false;
        Consumed = true;
        return true;
    }
    internal void RemoveConsumedObject()
    {
        if (!Consumed) return;
        var body = GetComponentInParent<Rigidbody>();
        var physicalObject = body != null ? body.gameObject : gameObject;
        physicalObject.SetActive(false);
        if (Application.isPlaying) Destroy(physicalObject); else DestroyImmediate(physicalObject);
    }
}
