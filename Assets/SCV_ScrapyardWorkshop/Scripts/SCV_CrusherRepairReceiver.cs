using UnityEngine;

public class SCV_CrusherRepairReceiver : MonoBehaviour
{
    [SerializeField] private SCV_Crusher crusher;

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

        if (crusher.TryRepair())
        {
            Debug.Log(
                "Physical repair kit used on crusher."
            );

            repairObject.SetActive(false);
            Destroy(repairObject);
        }
    }
}