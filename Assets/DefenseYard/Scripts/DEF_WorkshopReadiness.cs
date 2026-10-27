using UnityEngine;

// Teaching receiver remains outside Defense gates. Craft-ready alone never completes Step14.
public sealed class DEF_WorkshopReadiness : DEF_PrerequisiteSource
{
    [SerializeField] SCV_ProgressionManager crafting;
    [SerializeField] SCV_Workbench workbench;
    readonly System.Collections.Generic.HashSet<string> freshCrafts=new();
    bool ammoCrafted,repairCrafted;
    void Crafted(SCV_CraftedItem item)
    {
        if(Session<=0 || item==null || item.SourceLedger==null || item.UnitId==0)return;
        bool ammo=item.Type==SCV_CraftedItem.ItemType.Ammo;
        freshCrafts.Add(item.SourceLedger.GetEntityId()+(ammo?":":":repair:")+item.UnitId);
        if(ammo)ammoCrafted=true;else repairCrafted=true;
        Check();
    }
    [SerializeField] SCV_AmmoReceiver teachingReceiver;
    [SerializeField] SCV_RepairReceiver teachingRepairReceiver;
    string physicalReceipt;
    bool published;
    public override DEF_ProgressionState.Prerequisite Kind => DEF_ProgressionState.Prerequisite.Workshop14;
    public override void BeginSession(int session) { base.BeginSession(session); physicalReceipt = null; published = false; freshCrafts.Clear(); ammoCrafted=repairCrafted=false; }
    void OnEnable() { if(workbench!=null)workbench.PhysicalItemCrafted+=Crafted; if (teachingReceiver != null) teachingReceiver.PhysicalAmmoAccepted += Accepted; if(teachingRepairReceiver!=null)teachingRepairReceiver.PhysicalRepairAccepted+=Accepted; }
    void OnDisable() { if(workbench!=null)workbench.PhysicalItemCrafted-=Crafted; if (teachingReceiver != null) teachingReceiver.PhysicalAmmoAccepted -= Accepted; if(teachingRepairReceiver!=null)teachingRepairReceiver.PhysicalRepairAccepted-=Accepted; }
    void Accepted(string identity) { if(freshCrafts.Contains(identity)){physicalReceipt = identity; Check();} }
    void Update() { Check(); }
    void Check()
    {
        if (!published && Session > 0 && workbench != null && ammoCrafted && repairCrafted && crafting != null && crafting.WorkshopComplete && !string.IsNullOrEmpty(physicalReceipt))
        { published = true; Publish(physicalReceipt); }
    }
}
