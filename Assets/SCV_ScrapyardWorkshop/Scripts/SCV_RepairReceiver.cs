using System;
using UnityEngine;

public class SCV_RepairReceiver : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;
    [SerializeField] private SCV_DamagedPanel damagedPanel;
    public event Action<string> PhysicalRepairAccepted;
    bool delivering;
    private void OnTriggerEnter(Collider other){if(other!=null)TryReceive(other.GetComponentInParent<SCV_CraftedItem>());}
    private void OnTriggerStay(Collider other){if(other!=null)TryReceive(other.GetComponentInParent<SCV_CraftedItem>());}
    public bool TryReceive(SCV_CraftedItem item)
    {
        if(!isActiveAndEnabled || delivering || item==null || item.Consumed || item.Type!=SCV_CraftedItem.ItemType.RepairKit || damagedPanel==null)return false;
        if(resourceManager!=null && item.SourceInventory!=resourceManager)return false;
        string identity=(item.SourceLedger!=null ? item.SourceLedger.GetEntityId().ToString():"unbound")+":repair:"+item.UnitId;
        delivering=true;
        try
        {
            if(!damagedPanel.TryRepairPhysical(item))return false;
            if(PhysicalRepairAccepted!=null)foreach(Action<string> listener in PhysicalRepairAccepted.GetInvocationList())
                try{listener(identity);}catch(Exception error){Debug.LogWarning("Physical repair receipt listener failed: "+error.Message);}
            return true;
        }
        finally{delivering=false;}
    }
}
