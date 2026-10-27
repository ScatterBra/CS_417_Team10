using System;
using System.Collections.Generic;
using UnityEngine;

public class SCV_ResourceManager : MonoBehaviour
{
    public int scrap, parts, ammo, repairKits;
    public event Action OnResourcesChanged;
    private bool inventoryTransaction;
    private sealed class InventoryUnit { public long id; public SCV_CraftedItem item; }
    private readonly List<InventoryUnit> ammoUnits = new(), repairUnits = new();
    private long nextUnitId;
    private void SyncUnits(List<InventoryUnit> units, int count)
    {
        if (units.Count == count) return;
        units.Clear();
        for (int i = 0; i < count; i++) units.Add(new InventoryUnit { id = ++nextUnitId });
    }
    internal long RegisterRepresentation(SCV_CraftedItem item)
    {
        if (inventoryTransaction || item == null) return 0;
        var units = item.Type == SCV_CraftedItem.ItemType.Ammo ? ammoUnits : repairUnits;
        int count = item.Type == SCV_CraftedItem.ItemType.Ammo ? ammo : repairKits;
        if (count < 1) return 0;
        SyncUnits(units,count);
        foreach (var unit in units) if (unit.item == null) { unit.item = item; return unit.id; }
        return 0;
    }
    public bool ResetAmmoInventory(int count) => ResetInventory(ammoUnits,ref ammo,count);
    public bool ResetRepairInventory(int count) => ResetInventory(repairUnits,ref repairKits,count);
    private bool ResetInventory(List<InventoryUnit> units,ref int quantity,int count)
    {
        if (inventoryTransaction || count < 0) return false;
        units.Clear(); quantity=count; SyncUnits(units,count);
        inventoryTransaction=true; try { NotifyResources(); } finally { inventoryTransaction=false; }
        return true;
    }
    private InventoryUnit PhysicalUnit(List<InventoryUnit> units,int count,SCV_CraftedItem item,SCV_CraftedItem.ItemType type)
    {
        if (item == null || item.SourceLedger != this || item.Type != type || item.Consumed) return null;
        SyncUnits(units,count);
        return units.Find(unit => unit.id == item.UnitId && unit.item == item);
    }
    private bool ValidAmmo(int value,int capacity,int amount) =>
        isActiveAndEnabled && !inventoryTransaction && ammo > 0 && value >= 0 && capacity > value && amount > 0;
    public bool TryConsumePhysicalAmmo(SCV_CraftedItem item, ref int destinationAmmo,int capacity,int roundsPerBox,out int delivered)
    {
        delivered=0; if (!ValidAmmo(destinationAmmo,capacity,roundsPerBox)) return false;
        var unit=PhysicalUnit(ammoUnits,ammo,item,SCV_CraftedItem.ItemType.Ammo); if(unit==null)return false;
        float ignored=0; int amount=Math.Min(roundsPerBox,capacity-destinationAmmo);
        bool ok=Commit(new List<InventoryUnit>{unit},ammoUnits,ref ammo,ref destinationAmmo,amount,ref ignored,0);
        if(ok)delivered=amount; return ok;
    }
    public bool TryTransferRemoteAmmo(ref int destinationAmmo,int capacity,int roundsPerBox,out int delivered)
    {
        delivered=0; if(!ValidAmmo(destinationAmmo,capacity,roundsPerBox))return false;
        SyncUnits(ammoUnits,ammo); var selected=SelectRemoteUnits(ammoUnits,1); if(selected==null)return false;
        float ignored=0; int amount=Math.Min(roundsPerBox,capacity-destinationAmmo);
        bool ok=Commit(selected,ammoUnits,ref ammo,ref destinationAmmo,amount,ref ignored,0);
        if(ok)delivered=amount; return ok;
    }
    private bool ValidRepair(MonoBehaviour target,float health,float maximum,float amount,bool allowBroken=false) =>
        isActiveAndEnabled && !inventoryTransaction && repairKits>0 && target!=null && target.isActiveAndEnabled &&
        float.IsFinite(health) && float.IsFinite(maximum) && float.IsFinite(amount) && (health>0 || (allowBroken && health==0)) && maximum>health && amount>0;
    public bool TryRepairPhysical(SCV_CraftedItem item,MonoBehaviour target,ref float health,float maximum,float amount,out float restored)
    {
        restored=0; if(!ValidRepair(target,health,maximum,amount) || item==null || item.IsHeld)return false;
        var unit=PhysicalUnit(repairUnits,repairKits,item,SCV_CraftedItem.ItemType.RepairKit); if(unit==null)return false;
        float delta=Math.Min(maximum,health+amount)-health; if(!float.IsFinite(delta) || delta<=0)return false; int ignored=0;
        bool ok=Commit(new List<InventoryUnit>{unit},repairUnits,ref repairKits,ref ignored,0,ref health,delta);
        if(ok)restored=delta; return ok;
    }
    public bool TryRepairRemote(MonoBehaviour target,ref float health,float maximum,float amount,out float restored)
    {
        restored=0; if(!ValidRepair(target,health,maximum,amount))return false;
        SyncUnits(repairUnits,repairKits);var selected=SelectRemoteUnits(repairUnits,1);if(selected==null)return false;
        float delta=Math.Min(maximum,health+amount)-health; if(!float.IsFinite(delta) || delta<=0)return false; int ignored=0;
        bool ok=Commit(selected,repairUnits,ref repairKits,ref ignored,0,ref health,delta);
        if(ok)restored=delta;return ok;
    }
    // Integer Workshop panel overload commits its real field before notifying, not a temporary float.
    public bool TryRepairPanel(SCV_CraftedItem item,bool physical,MonoBehaviour target,ref int condition,int maximum,int amount,out int restored,bool allowBroken=false)
    {
        restored=0;if(!ValidRepair(target,condition,maximum,amount,allowBroken) || (physical && (item==null || item.IsHeld)))return false;
        List<InventoryUnit> selected;
        if(physical)
        {var unit=PhysicalUnit(repairUnits,repairKits,item,SCV_CraftedItem.ItemType.RepairKit);if(unit==null)return false;selected=new List<InventoryUnit>{unit};}
        else {SyncUnits(repairUnits,repairKits);selected=SelectRemoteUnits(repairUnits,1);if(selected==null)return false;}
        int delta=Math.Min(amount,maximum-condition);float ignored=0;
        bool ok=Commit(selected,repairUnits,ref repairKits,ref condition,delta,ref ignored,0);
        if(ok)restored=delta;return ok;
    }
    private List<InventoryUnit> SelectRemoteUnits(List<InventoryUnit> units,int count)
    {
        var selected=new List<InventoryUnit>();
        foreach(var unit in units)if(unit.item==null){selected.Add(unit);if(selected.Count==count)return selected;}
        foreach(var unit in units)if(unit.item!=null && unit.item.CanRemoteConsume){selected.Add(unit);if(selected.Count==count)return selected;}
        return null;
    }
    // Shared commit for Ammo, repair health and Workshop integer condition; no caller callback inside commit.
    private bool Commit(List<InventoryUnit> units,List<InventoryUnit> inventory,ref int quantity,
        ref int integerDestination,int integerDelta,ref float healthDestination,float healthDelta)
    {
        inventoryTransaction=true;var claimed=new List<SCV_CraftedItem>();bool committed=false;
        try
        {
            foreach(var unit in units)if(unit.item!=null)
            {if(!unit.item.TryClaim(this))return false;claimed.Add(unit.item);}
            foreach(var item in claimed)item.Commit(this);
            foreach(var unit in units)inventory.Remove(unit);
            quantity-=units.Count;integerDestination+=integerDelta;healthDestination+=healthDelta;
            committed=true;NotifyResources();return true;
        }
        finally
        {
            try{foreach(var item in claimed)if(item!=null){if(committed)item.RemoveConsumedObject();else item.Release(this);}}
            finally{inventoryTransaction=false;}
        }
    }
    private void NotifyResources()
    {
        if(OnResourcesChanged==null)return;
        foreach(Action listener in OnResourcesChanged.GetInvocationList())
            try{listener();}catch(Exception error){Debug.LogWarning("Inventory notification failed: "+error.Message);}
    }
    private void AddInventory(List<InventoryUnit> units,ref int quantity,int amount)
    {
        if(inventoryTransaction || amount<=0 || quantity<0 || amount>int.MaxValue-quantity)return;
        SyncUnits(units,quantity);for(int i=0;i<amount;i++)units.Add(new InventoryUnit{id=++nextUnitId});quantity+=amount;
        inventoryTransaction=true;try{NotifyResources();}finally{inventoryTransaction=false;}
    }
    public void AddAmmo(int amount)=>AddInventory(ammoUnits,ref ammo,amount);
    public void AddRepairKit(int amount)=>AddInventory(repairUnits,ref repairKits,amount);
    private bool SpendInventory(List<InventoryUnit> units,ref int quantity,int amount)
    {
        if(!isActiveAndEnabled || inventoryTransaction || amount<=0 || quantity<amount)return false;
        SyncUnits(units,quantity);var selected=SelectRemoteUnits(units,amount);if(selected==null)return false;
        int unusedInt=0;float unusedFloat=0;return Commit(selected,units,ref quantity,ref unusedInt,0,ref unusedFloat,0);
    }
    public bool SpendAmmo(int amount)=>SpendInventory(ammoUnits,ref ammo,amount);
    public bool SpendRepairKit(int amount)=>SpendInventory(repairUnits,ref repairKits,amount);
    public void AddScrap(int amount)
    {
        scrap += amount;
        Debug.Log("Scrap: " + scrap);
        OnResourcesChanged?.Invoke();
    }

    public bool SpendScrap(int amount)
    {
        if (scrap < amount)
            return false;

        scrap -= amount;
        Debug.Log("Scrap: " + scrap);
        OnResourcesChanged?.Invoke();
        return true;
    }

    public void AddParts(int amount)
    {
        parts += amount;
        Debug.Log("Parts: " + parts);
        OnResourcesChanged?.Invoke();
    }

    public bool SpendParts(int amount)
    {
        if (parts < amount)
            return false;

        parts -= amount;
        Debug.Log("Parts: " + parts);
        OnResourcesChanged?.Invoke();
        return true;
    }

}
