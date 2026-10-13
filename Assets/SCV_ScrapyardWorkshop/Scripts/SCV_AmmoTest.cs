using UnityEngine;

public class SCV_AmmoTest : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;

    [Header("Ammo Storage")]
    [SerializeField] private int storedAmmo = 0;
    [SerializeField] private int maxAmmo = 5;

    public int StoredAmmo => storedAmmo;
    public int MaxAmmo => maxAmmo;

    public void LoadAmmo()
    {
        if (storedAmmo >= maxAmmo)
        {
            Debug.Log("Ammo storage is full.");
            return;
        }

        if (!resourceManager.SpendAmmo(1))
        {
            Debug.Log("No crafted ammo available.");
            return;
        }

        storedAmmo++;
        Debug.Log("Ammo loaded into storage: " + storedAmmo + "/" + maxAmmo);
    }

    [ContextMenu("Load Ammo")]
    private void TestLoadAmmo()
    {
        LoadAmmo();
    }
}