using UnityEngine;

public class SCV_AmmoReceiver : MonoBehaviour
{
    [SerializeField] private SCV_AmmoTest ammoTest;

    private void OnTriggerEnter(Collider other)
    {
        SCV_CraftedItem item =
            other.GetComponentInParent<SCV_CraftedItem>();

        if (item == null)
            return;

        if (item.Type != SCV_CraftedItem.ItemType.Ammo)
            return;

        if (ammoTest.LoadAmmo())
        {
            Debug.Log("Physical ammo accepted.");

            Destroy(item.gameObject);
        }
    }
}