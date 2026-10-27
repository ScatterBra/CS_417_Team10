using UnityEngine;

public class SCV_AmmoTest : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;
    [SerializeField] private SCV_AmmoTestDisplay ammoDisplay;

    [Header("Ammo Storage")]
    [SerializeField] private int storedAmmo = 0;
    [SerializeField] private int maxAmmo = 5;

    public int StoredAmmo => storedAmmo;
    public int MaxAmmo => maxAmmo;

    public bool LoadAmmo()
    {
        if (storedAmmo >= maxAmmo)
        {
            Debug.Log("Ammo storage is full.");

            if (ammoDisplay != null)
                ammoDisplay.ShowFeedback(
                    "<color=#FF4444>AMMO STORAGE FULL\n" +
                    "USE AMMO BEFORE LOADING MORE</color>"
                );

            return false;
        }

        if (resourceManager == null || !resourceManager.TryTransferRemoteAmmo(ref storedAmmo, maxAmmo, 1, out _))
        {
            Debug.Log("No crafted ammo available.");

            if (ammoDisplay != null)
                ammoDisplay.ShowFeedback(
                    "<color=#FF4444>CANNOT LOAD AMMO\n" +
                    "NO CRAFTED AMMO AVAILABLE\n" +
                    "CRAFT AMMO AT THE WORKBENCH</color>"
                );

            return false;
        }



        Debug.Log(
            "Ammo loaded into storage: " +
            storedAmmo + "/" + maxAmmo
        );

        if (ammoDisplay != null)
            ammoDisplay.ShowFeedback(
                "<color=#66FF66>AMMO TEST SUCCESSFUL\n" +
                "SUPPLY ACCEPTED</color>"
            );

        return true;
    }

    public bool LoadPhysicalAmmo(SCV_CraftedItem item)
    {
        string rejection = resourceManager == null ? "Workshop ledger is offline. Restore its connection and retry." :
            storedAmmo >= maxAmmo ? "Storage is full. Use stored ammo before retrying with this box." :
            item == null || item.Type != SCV_CraftedItem.ItemType.Ammo ? "Wrong item. Present a crafted Ammo Box." :
            item.SourceLedger != resourceManager ? "Box belongs to no matching ledger. Craft a box at this Workshop." :
            resourceManager.ammo < 1 ? "No ledger ammo remains. Craft a new Ammo Box." : null;
        if (rejection != null)
        {
            if (ammoDisplay != null) ammoDisplay.ShowFeedback("Cannot load ammo: " + rejection);
            return false;
        }
        bool accepted = resourceManager.TryConsumePhysicalAmmo(item, ref storedAmmo, maxAmmo, 1, out _);
        if (ammoDisplay != null) ammoDisplay.ShowFeedback(accepted ? "Ammo loaded: one box and one ledger unit consumed." :
            "Cannot load ammo: box is busy or consumed. Retry after delivery or bring a new crafted box.");
        return accepted;
    }
    [ContextMenu("Load Ammo")]
    private void TestLoadAmmo()
    {
        LoadAmmo();
    }
}
