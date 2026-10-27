using UnityEngine;

public class SCV_AmmoReceiver : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;
    public event System.Action<string> PhysicalAmmoAccepted;
    [SerializeField] private SCV_AmmoTest ammoTest;
    private void OnTriggerEnter(Collider other)
    {
        var item = other.GetComponentInParent<SCV_CraftedItem>();
        if (item == null || item.Consumed || item.Type != SCV_CraftedItem.ItemType.Ammo || ammoTest == null) return;
        if (resourceManager != null && item.SourceInventory != resourceManager) return;
        string identity = (item.SourceLedger != null ? item.SourceLedger.GetEntityId().ToString() : "unbound") + ":" + item.AmmoUnitId;
        if (ammoTest.LoadPhysicalAmmo(item)) PhysicalAmmoAccepted?.Invoke(identity);
    }
}
