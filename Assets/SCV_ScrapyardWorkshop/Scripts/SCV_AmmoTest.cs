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

        if (!resourceManager.SpendAmmo(1))
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

        storedAmmo++;

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

    [ContextMenu("Load Ammo")]
    private void TestLoadAmmo()
    {
        LoadAmmo();
    }
}