using UnityEngine;

public class SCV_RepairReceiver : MonoBehaviour
{
    [SerializeField] private SCV_DamagedPanel damagedPanel;
    [SerializeField] private SCV_ResourceManager resourceManager;

    private void OnTriggerEnter(Collider other)
    {
        SCV_CraftedItem item =
            other.GetComponentInParent<SCV_CraftedItem>();

        if (item == null)
            return;

        if (item.Type !=
            SCV_CraftedItem.ItemType.RepairKit)
            return;

        if (item.SourceInventory != resourceManager)
            return;

        if (!item.TryReserve())
            return;

        if (damagedPanel.TryRepair())
        {
            Debug.Log(
                "Physical repair kit used on test panel."
            );

            DestroyPhysicalItem(item);
        }
        else
        {
            item.ReleaseReservation();
        }
    }

    private void DestroyPhysicalItem(SCV_CraftedItem item)
    {
        Rigidbody rb =
            item.GetComponentInParent<Rigidbody>();

        GameObject physicalObject =
            rb != null
                ? rb.gameObject
                : item.gameObject;

        physicalObject.SetActive(false);
        Destroy(physicalObject);
    }
}