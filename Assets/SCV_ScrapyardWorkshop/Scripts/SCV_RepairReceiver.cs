using System.Collections.Generic;
using UnityEngine;

public class SCV_RepairReceiver : MonoBehaviour
{
    [SerializeField] private SCV_DamagedPanel damagedPanel;

    private HashSet<SCV_CraftedItem> processedItems =
        new HashSet<SCV_CraftedItem>();

    private void OnTriggerEnter(Collider other)
    {
        SCV_CraftedItem item =
            other.GetComponentInParent<SCV_CraftedItem>();

        if (item == null)
            return;

        if (item.Type != SCV_CraftedItem.ItemType.RepairKit)
            return;

        // Prevent multiple colliders on the same toolbox
        // from repairing more than once.
        if (processedItems.Contains(item))
            return;

        processedItems.Add(item);

        if (damagedPanel.TryRepair())
        {
            Debug.Log("Physical repair kit accepted.");
            Destroy(item.gameObject);
        }
        else
        {
            // Repair didn't happen, so allow this item
            // to be attempted again later.
            processedItems.Remove(item);
        }
    }
}