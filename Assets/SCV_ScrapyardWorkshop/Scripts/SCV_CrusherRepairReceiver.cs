using UnityEngine;
using System.Collections.Generic;

public class SCV_CrusherRepairReceiver : MonoBehaviour
{
    [SerializeField] private SCV_Crusher crusher;

    private HashSet<GameObject> processedItems =
        new HashSet<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        GameObject repairObject;

        if (other.attachedRigidbody != null)
        {
            repairObject =
                other.attachedRigidbody.gameObject;
        }
        else
        {
            repairObject =
                other.transform.root.gameObject;
        }

        if (!repairObject.CompareTag("RepairKit"))
            return;

        if (processedItems.Contains(repairObject))
            return;

        processedItems.Add(repairObject);

        if (crusher.TryRepair())
        {
            Debug.Log(
                "Physical repair kit used on crusher."
            );

            repairObject.SetActive(false);
            Destroy(repairObject);
        }
        else
        {
            processedItems.Remove(repairObject);
        }
    }
}