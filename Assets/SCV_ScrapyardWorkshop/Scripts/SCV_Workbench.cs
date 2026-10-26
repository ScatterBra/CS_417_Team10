using UnityEngine;

public class SCV_Workbench : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;

    [Header("Ammo Recipe")]
    [SerializeField] private int ammoPartsCost = 1;
    [SerializeField] private int ammoProduced = 1;

    [Header("Repair Kit Recipe")]
    [SerializeField] private int repairPartsCost = 2;
    [SerializeField] private int repairKitsProduced = 1;

    [SerializeField] private SCV_ProgressionManager progressionManager;

    [Header("Physical Crafted Items")]
    [SerializeField] private GameObject ammoBoxPrefab;
    [SerializeField] private GameObject repairKitPrefab;

    [SerializeField] private Transform ammoSpawnPoint;
    [SerializeField] private Transform repairKitSpawnPoint;

    public int AmmoPartsCost => ammoPartsCost;
    public int AmmoProduced => ammoProduced;
    public int RepairPartsCost => repairPartsCost;
    public int RepairKitsProduced => repairKitsProduced;

    [Header("Feedback")]
    [SerializeField] private float feedbackDuration = 2f;

    private string feedbackMessage = "";
    private float feedbackEndTime = -1f;

    public string LastMessage
    {
        get
        {
            if (Time.time <= feedbackEndTime)
                return feedbackMessage;

            return "READY";
        }
    }

    private void SetFeedback(string message)
    {
        feedbackMessage = message;
        feedbackEndTime = Time.time + feedbackDuration;
    }

    public void CraftAmmo()
    {
        if (!resourceManager.SpendParts(ammoPartsCost))
        {
            SetFeedback(
                "<color=#FF4444>CANNOT CRAFT AMMO\n" +
                "NEED 1 PART\n" +
                "USE THE CRUSHER TO MAKE PARTS</color>"
            );
            return;
        }

        resourceManager.AddAmmo(ammoProduced);

        for (int i = 0; i < ammoProduced; i++)
        {
            SpawnItem(ammoBoxPrefab, ammoSpawnPoint);
        }

        SetFeedback(
            "<color=#66FF66>AMMO CRAFTED</color>"
        );
        Debug.Log("Crafted " + ammoProduced + " ammo.");

        if (progressionManager != null)
        {
            progressionManager.AmmoCrafted();
        }
    }

    public void CraftRepairKit()
    {
        if (!resourceManager.SpendParts(repairPartsCost))
        {
            SetFeedback(
                "<color=#FF4444>CANNOT CRAFT REPAIR KIT\n" +
                "NEED 2 PARTS\n" +
                "USE THE CRUSHER TO MAKE PARTS</color>"
            );
            return;
        }

        for (int i = 0; i < repairKitsProduced; i++)
        {
            resourceManager.AddRepairKit(1);
            SpawnItem(repairKitPrefab, repairKitSpawnPoint);
        }

        SetFeedback(
            "<color=#66FF66>REPAIR KIT CRAFTED</color>"
        );
        Debug.Log("Crafted " + repairKitsProduced + " repair kit.");

        if (progressionManager != null)
        {
            progressionManager.RepairKitCrafted();
        }
    }

    private void SpawnItem(GameObject prefab,Transform spawnPoint)
    {
        if (prefab == null || spawnPoint == null)
        {
            Debug.LogWarning(
                "Crafted item prefab or spawn point is missing."
            );
            return;
        }

        GameObject spawned = Instantiate(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        SCV_CraftedItem craftedItem =
            spawned.GetComponentInChildren<SCV_CraftedItem>(true);

        if (craftedItem != null)
        {
            craftedItem.SetSourceInventory(
                resourceManager
            );
        }
    }

    [ContextMenu("Craft Ammo")]
    private void TestCraftAmmo()
    {
        CraftAmmo();
    }

    [ContextMenu("Craft Repair Kit")]
    private void TestCraftRepairKit()
    {
        CraftRepairKit();
    }
}