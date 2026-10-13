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

    public int AmmoPartsCost => ammoPartsCost;
    public int AmmoProduced => ammoProduced;
    public int RepairPartsCost => repairPartsCost;
    public int RepairKitsProduced => repairKitsProduced;

    public string LastMessage { get; private set; } = "READY";

    public void CraftAmmo()
    {
        if (!resourceManager.SpendParts(ammoPartsCost))
        {
            LastMessage = "NOT ENOUGH PARTS FOR AMMO";
            Debug.Log(LastMessage);
            return;
        }

        resourceManager.AddAmmo(ammoProduced);

        LastMessage = "AMMO CRAFTED";
        Debug.Log("Crafted " + ammoProduced + " ammo.");
    }

    public void CraftRepairKit()
    {
        if (!resourceManager.SpendParts(repairPartsCost))
        {
            LastMessage = "NOT ENOUGH PARTS FOR REPAIR KIT";
            Debug.Log(LastMessage);
            return;
        }

        for (int i = 0; i < repairKitsProduced; i++)
        {
            resourceManager.AddRepairKit(1);
        }

        LastMessage = "REPAIR KIT CRAFTED";
        Debug.Log("Crafted " + repairKitsProduced + " repair kit.");
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