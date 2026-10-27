using UnityEngine;

public class SCV_CrusherRepairReceiver : MonoBehaviour
{
    [SerializeField] private SCV_Crusher crusher;
    [SerializeField] private SCV_ResourceManager resourceManager;
    private bool delivering;
    private void OnTriggerEnter(Collider other) { Receive(other); }
    private void OnTriggerStay(Collider other) { Receive(other); }
    private void Receive(Collider other)
    {
        var item = other != null ? other.GetComponentInParent<SCV_CraftedItem>() : null;
        if (!isActiveAndEnabled || delivering || crusher == null || item == null ||
            item.Type != SCV_CraftedItem.ItemType.RepairKit || item.Consumed ||
            (resourceManager != null && item.SourceInventory != resourceManager)) return;
        delivering = true;
        try { crusher.TryRepairPhysical(item); }
        finally { delivering = false; }
    }
}
