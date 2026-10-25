using UnityEngine;

public class SCV_CraftedItem : MonoBehaviour
{
    public enum ItemType
    {
        Ammo,
        RepairKit
    }

    [SerializeField] private ItemType itemType;

    public ItemType Type => itemType;

    public SCV_ResourceManager SourceInventory { get; private set; }

    private bool reserved = false;

    public bool IsReserved => reserved;

    public void SetSourceInventory(
        SCV_ResourceManager sourceInventory)
    {
        SourceInventory = sourceInventory;
    }

    public bool TryReserve()
    {
        if (reserved)
            return false;

        reserved = true;
        return true;
    }

    public void ReleaseReservation()
    {
        reserved = false;
    }
}