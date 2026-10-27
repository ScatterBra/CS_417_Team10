using UnityEngine;

public class SCV_DamagedPanel : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;
    [SerializeField] private SCV_DamagedPanelDisplay panelDisplay;
    [Header("Condition")]
    [SerializeField] private int currentCondition = 40;
    [SerializeField] private int maxCondition = 100;
    [SerializeField] private int repairAmount = 30;
    public int CurrentCondition => currentCondition;
    public int MaxCondition => maxCondition;
    public string LastFeedback { get; private set; }
    public void Repair() { TryRepair(); }
    public bool TryRepair() => RepairInternal(null,false);
    public bool TryRepairPhysical(SCV_CraftedItem item) => RepairInternal(item,true);
    bool RepairInternal(SCV_CraftedItem item,bool physical)
    {
        if (!isActiveAndEnabled || currentCondition<=0) return Reject("the target is offline or destroyed. Choose a live damaged panel.");
        if (currentCondition>=maxCondition) return Reject("the panel is already full. Save the kit for a damaged panel.");
        if (repairAmount<=0) return Reject("the repair amount is invalid. Correct the panel configuration and retry.");
        if (resourceManager==null || !resourceManager.isActiveAndEnabled) return Reject("the Workshop ledger is offline. Restore its connection and retry.");
        if (physical && (item==null || item.Type!=SCV_CraftedItem.ItemType.RepairKit)) return Reject("this is not a RepairKit. Craft and deliver a RepairKit.");
        if (physical && item.SourceLedger!=resourceManager) return Reject("the kit has no matching Workshop provenance. Use a kit crafted by this Workshop.");
        if (physical && item.IsHeld) return Reject("the kit is still held. Release it inside the receiver.");
        if(resourceManager.repairKits<1)return Reject("no kit remains in inventory. Craft another RepairKit.");
        if(!resourceManager.TryRepairPanel(item,physical,this,ref currentCondition,maxCondition,repairAmount,out int restored))
            return Reject("the kit is busy, consumed or stale after reset. Finish the current transfer or bring a newly crafted kit.");
        Feedback("Panel repaired: +"+restored+" condition; one Workshop RepairKit consumed."); return true;
    }
    bool Reject(string reason){Feedback("Cannot repair panel: "+reason);return false;}
    void Feedback(string text){LastFeedback=text;if(panelDisplay!=null)panelDisplay.ShowFeedback(text);}
    [ContextMenu("Repair Panel")] private void TestRepair(){Repair();}
}
