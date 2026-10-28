using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// A physical trigger presents an exact item; release it or use the existing supply button.
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(DEF_ContentGate))]
public class DEF_WorkshopAmmoReceiver : MonoBehaviour
{
    public SCV_ResourceManager ledger;
    public DEF_GameState destination;
    public TMP_Text feedbackText;
    [Min(1)] public int roundsPerBox = 10;
    public string LastMessage { get; private set; } = "DELIVER AMMO: release a crafted Ammo Box inside this intake.";
    private readonly System.Collections.Generic.HashSet<Collider> contacts = new();
    private bool delivering;

    void Awake() { if (GetComponent<DEF_ContentGate>() == null) gameObject.AddComponent<DEF_ContentGate>();  GetComponent<BoxCollider>().isTrigger = true; GetComponent<DEF_ContentGate>().Apply(); }
    void OnDisable() { contacts.Clear(); }
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<SCV_CraftedItem>() != null) contacts.Add(other);
    }
    void OnTriggerExit(Collider other) { contacts.Remove(other); }
    void OnTriggerStay(Collider other)
    {
        var item = other.GetComponentInParent<SCV_CraftedItem>();
        if (item == null || item.Consumed) return;
        contacts.Add(other);
        var grab = item.GetComponent<XRGrabInteractable>();
        if (grab == null || !grab.isSelected) TryDeliver(item);
    }
    public void DeliverPresented()
    {
        contacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        foreach (var contact in contacts)
        {
            var item = contact.GetComponentInParent<SCV_CraftedItem>();
            if (item != null && !item.Consumed) { TryDeliver(item); return; }
        }
        Feedback("Cannot deliver ammo: no box in the intake. Bring a crafted Ammo Box into the marked intake.");
    }
    public bool TryDeliver(SCV_CraftedItem item)
    {
        if (delivering) return false;
        if (!DEF_Progression.Allows(DEF_Action.PhysicalAmmo)) return Reject(DEF_Progression.Current.LastFailure);
        if (!isActiveAndEnabled) return Reject("the intake is disabled. Enable the intake and retry with this box.");
        if (item == null || item.Type != SCV_CraftedItem.ItemType.Ammo)
            return Reject("this is not an Ammo Box. Craft ammo at the Workshop and bring that box here.");
        if (item.Consumed || !item.gameObject.activeInHierarchy)
            return Reject("this box is already consumed or inactive. Bring another crafted Ammo Box.");
        if (destination == null || !destination.isActiveAndEnabled)
            return Reject("Defense is offline. Restore the Defense state connection and retry with this box.");
        if (ledger == null || !ledger.isActiveAndEnabled)
            return Reject("the Workshop ledger is offline. Restore its connection and retry with this box.");
        if (item.SourceLedger != ledger)
            return Reject("this box is not linked to this Workshop ledger. Craft a box at this Workshop and deliver it here.");
        if (roundsPerBox <= 0 || destination.maxAmmo <= 0 || destination.ammo < 0)
            return Reject("the intake capacity is invalid. Correct its configuration and retry with this box.");
        if (destination.ammo >= destination.maxAmmo)
            return Reject("Defense ammo is full. Use Defense ammo, then retry with this box.");
        if (ledger.ammo < 1)
            return Reject("no crafted ammo remains in the Workshop ledger. Craft a new Ammo Box before delivery.");
        string receipt = ledger.GetEntityId().ToString() + ":" + item.AmmoUnitId;
        var committedLedger=ledger;
        var committedDestination=destination;
        var owner=committedDestination.GetComponent<DEF_Progression>();
        var delivery=owner!=null?owner.BeginDelivery():null;
        delivering=true;
        try
        {
            if(!committedLedger.TryConsumePhysicalAmmo(item,ref committedDestination.ammo,
                committedDestination.maxAmmo,roundsPerBox,out int added))
                return Reject("this box or ledger is busy. Release the box and retry after the current delivery.");
            if(delivery!=null && delivery.Matches && destination==committedDestination && ledger==committedLedger)
                owner.Refilled(added,receipt);
            Feedback("Ammo delivered: +"+added+" rounds. Read the wave introduction, then choose START WAVE on your HUD.");
            return true;
        }
        finally { delivering=false;delivery?.Dispose(); }
    }
    bool Reject(string reason) { Feedback("Cannot deliver ammo: " + reason); return false; }
    void Feedback(string message)
    {
        LastMessage = message;
        if (feedbackText != null) feedbackText.text = message;
    }
}
