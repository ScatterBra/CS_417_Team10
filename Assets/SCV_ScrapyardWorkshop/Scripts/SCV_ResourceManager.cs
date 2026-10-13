using UnityEngine;

public class SCV_ResourceManager : MonoBehaviour
{
    public int scrap = 0;
    public int parts = 0;
    public int ammo = 0;
    public int repairKits = 0;

    public void AddScrap(int amount)
    {
        scrap += amount;
        Debug.Log("Scrap: " + scrap);
    }

    public bool SpendScrap(int amount)
    {
        if (scrap < amount)
            return false;

        scrap -= amount;
        Debug.Log("Scrap: " + scrap);
        return true;
    }

    public void AddParts(int amount)
    {
        parts += amount;
        Debug.Log("Parts: " + parts);
    }

    public bool SpendParts(int amount)
    {
        if (parts < amount)
            return false;

        parts -= amount;
        Debug.Log("Parts: " + parts);
        return true;
    }

    public void AddAmmo(int amount)
    {
        ammo += amount;
        Debug.Log("Ammo: " + ammo);
    }

    public void AddRepairKit(int amount)
    {
        repairKits += amount;
        Debug.Log("Repair Kits: " + repairKits);
    }

    public bool SpendAmmo(int amount)
    {
        if (ammo < amount)
            return false;

        ammo -= amount;
        Debug.Log("Ammo: " + ammo);
        return true;
    }
    public bool SpendRepairKit(int amount)
    {
        if (repairKits < amount)
            return false;

        repairKits -= amount;
        Debug.Log("Repair Kits: " + repairKits);
        return true;
    }
}
