using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(DEF_ContentGate))]
public sealed class DEF_WorkshopRepairReceiver : MonoBehaviour
{
    public SCV_ResourceManager ledger;
    public MonoBehaviour target;
    public TMP_Text feedbackText;
    const float FeedbackSeconds = 4f;
    string lastMessage;
    float feedbackExpires;
    public string LastMessage => Time.unscaledTime < feedbackExpires ? lastMessage : null;
    readonly System.Collections.Generic.HashSet<Collider> contacts = new();
    readonly System.Collections.Generic.Dictionary<SCV_CraftedItem, string> reportedMessages = new();
    readonly System.Collections.Generic.List<SCV_CraftedItem> staleItems = new();
    SCV_CraftedItem automaticItem;
    public event Action<MonoBehaviour,float,float,string> PhysicalRepairAccepted;
    bool delivering;
    void Awake() { if (GetComponent<DEF_ContentGate>() == null) gameObject.AddComponent<DEF_ContentGate>(); GetComponent<BoxCollider>().isTrigger=true;GetComponent<DEF_ContentGate>().Apply();}
    void OnTriggerEnter(Collider other) { TryPresented(other); }
    void OnTriggerStay(Collider other) { TryPresented(other); }
    void OnTriggerExit(Collider other)
    {
        contacts.Remove(other);
        if (other == null) return;
        var item = other.GetComponentInParent<SCV_CraftedItem>();
        if (item == null) return;
        foreach (var contact in contacts)
            if (contact != null && contact.GetComponentInParent<SCV_CraftedItem>() == item) return;
        reportedMessages.Remove(item);
    }
    void TryPresented(Collider other)
    {
        if (other == null || delivering) return;
        var item = other.GetComponentInParent<SCV_CraftedItem>();
        if (item == null || item.Consumed || !item.gameObject.activeInHierarchy) return;
        contacts.Add(other);
        automaticItem = item;
        try { TryDeliver(item); }
        finally { automaticItem = null; }
    }
    void Update()
    {
        if (feedbackText != null && lastMessage != null && Time.unscaledTime >= feedbackExpires)
        { feedbackText.text = string.Empty; lastMessage = null; }
        contacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        staleItems.Clear();
        foreach (var pair in reportedMessages)
        {
            bool present = false;
            if (pair.Key != null && !pair.Key.Consumed)
                foreach (var contact in contacts)
                    if (contact.GetComponentInParent<SCV_CraftedItem>() == pair.Key) { present = true; break; }
            if (!present) staleItems.Add(pair.Key);
        }
        foreach (var item in staleItems) reportedMessages.Remove(item);
        staleItems.Clear();
    }
    void OnDisable()
    {
        contacts.Clear(); reportedMessages.Clear(); automaticItem = null; lastMessage = null;
        if (feedbackText != null) feedbackText.text = string.Empty;
    }
    public bool TryDeliver(SCV_CraftedItem item)
    {
        if(delivering)return false;
        if(!isActiveAndEnabled)return Reject("the receiver is disabled. Enable this target's receiver and retry.");
        if(!DEF_Progression.Allows(DEF_Action.Repair))return Reject(DEF_Progression.Current.LastFailure);
        if(item==null || item.Type!=SCV_CraftedItem.ItemType.RepairKit)return Reject("this is not a RepairKit. Craft a RepairKit and bring that kit here.");
        if(item.Consumed || !item.gameObject.activeInHierarchy)return Reject("this kit was consumed or is inactive. Bring an active newly crafted kit.");
        if(item.IsHeld)return Reject("the kit is still held. Release it inside this target's receiver.");
        if(ledger==null || !ledger.isActiveAndEnabled)return Reject("the Workshop ledger is offline. Restore the configured connection and retry.");
        if(item.SourceLedger!=ledger)return Reject("the kit belongs to a different or unassigned ledger. Use a kit from this Workshop.");
        if(target==null || !target.isActiveAndEnabled)return Reject("the target is missing or offline. Use a receiver on a live damaged defense.");
        float health,maximum;
        if(target is DEF_Turret t){health=t.health;maximum=t.maxHealth;}
        else if(target is DEF_Barricade b){health=b.health;maximum=b.maxHealth;}
        else if(target is DEF_ShockTrap s){health=s.health;maximum=s.maxHealth;}
        else return Reject("this receiver has no supported defense target. Connect a turret, barricade or shock trap.");
        if(!(health>0))return Reject("the defense is destroyed. Build another defense instead of spending a kit here.");
        if(health>=maximum)return Reject("the defense is at full integrity. Save this kit for a damaged defense.");
        if(ledger.repairKits<1)return Reject("no kit remains in the Workshop ledger. Craft a new RepairKit.");
        string identity=ledger.GetEntityId()+":repair:"+item.UnitId;
        var committedTarget=target;var committedLedger=ledger;
        var owner=DEF_Progression.Current;var delivery=owner!=null?owner.BeginDelivery():null;
        delivering=true;
        try
        {
            if(!DEF_RepairDelivery.TryRepair(committedLedger,item,committedTarget,DEF_RepairDelivery.AmountFor(committedTarget),true,out float before,out float after))
                return Reject("the kit is busy, stale after reset, or the repair configuration is invalid. Finish the active transfer or use a newly crafted kit on a valid target.");
            if(delivery!=null && delivery.Matches && target==committedTarget && ledger==committedLedger)
            {
                owner.PhysicallyRepaired(this,committedTarget,before,after,identity);
                owner.CombatRepair(this,committedTarget,before,after,identity);
            }
            if(PhysicalRepairAccepted!=null)foreach(Action<MonoBehaviour,float,float,string> listener in PhysicalRepairAccepted.GetInvocationList())
                try{listener(committedTarget,before,after,identity);}catch(Exception error){Debug.LogWarning("Defense repair receipt listener failed: "+error.Message);}
            Feedback("Repair delivered: +"+(after-before)+" integrity; exact kit and one Workshop unit consumed.");return true;
        }
        finally{delivering=false;delivery?.Dispose();}
    }
    bool Reject(string reason){Feedback("Cannot deliver repair: "+reason);return false;}
    void Feedback(string message)
    {
        if (automaticItem != null)
        {
            if (reportedMessages.TryGetValue(automaticItem, out var previous) && previous == message) return;
            reportedMessages[automaticItem] = message;
        }
        lastMessage = message;
        feedbackExpires = Time.unscaledTime + FeedbackSeconds;
        if (feedbackText != null) feedbackText.text = message;
    }
}
