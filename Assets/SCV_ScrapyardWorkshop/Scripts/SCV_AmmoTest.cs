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
                ammoDisplay.ShowFeedback("AMMO STORAGE FULL");

            return false;
        }

        if (!resourceManager.SpendAmmo(1))
        {
            Debug.Log("No crafted ammo available.");

            if (ammoDisplay != null)
                ammoDisplay.ShowFeedback("NO AMMO AVAILABLE");

            return false;
        }

        storedAmmo++;

        Debug.Log(
            "Ammo loaded into storage: " +
            storedAmmo + "/" + maxAmmo
        );

        if (ammoDisplay != null)
            ammoDisplay.ShowFeedback("AMMO LOADED");

        return true;
    }

    [ContextMenu("Load Ammo")]
    private void TestLoadAmmo()
    {
        LoadAmmo();
    }
}