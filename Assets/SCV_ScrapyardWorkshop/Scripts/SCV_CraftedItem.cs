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
}