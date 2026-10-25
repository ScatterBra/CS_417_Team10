using UnityEngine;

public class SCV_AmmoReceiver : MonoBehaviour
{
    [SerializeField] private SCV_AmmoTest ammoTest;
    [SerializeField] private SCV_ResourceManager resourceManager;

    private void OnTriggerEnter(Collider other)
    {
        SCV_CraftedItem item =
            other.GetComponentInParent<SCV_CraftedItem>();

        if (item == null)
            return;

        if (item.Type != SCV_CraftedItem.ItemType.Ammo)
            return;

        if (item.SourceInventory != resourceManager)
            return;

        if (!item.TryReserve())
            return;

        if (ammoTest.LoadAmmo())
        {
            Debug.Log(
                "Physical ammo box loaded into test storage."
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