using System;
using System.Collections.Generic;
using UnityEngine;

// A unit carries the actual paid craft's identity. Null means initial/debug/unproven stock.
public sealed class MED_MedicineUnit
{
    public readonly MED_PlayerResources Patient;
    public readonly MED_PrepBench Bench;
    public readonly MED_Recipe Recipe;
    public readonly int Generation;
    public readonly string Id = Guid.NewGuid().ToString("N");
    internal MED_MedicineUnit(MED_PlayerResources patient, MED_PrepBench bench, MED_Recipe recipe, int generation)
    { Patient=patient; Bench=bench; Recipe=recipe; Generation=generation; }
}

internal sealed class MED_CraftTicket
{
    internal MED_PlayerResources Player;
    internal MED_PrepBench Bench;
    internal MED_Recipe Recipe;
    internal int Generation;
    internal bool Consumed;
    internal Dictionary<MED_ResourceType,int> Outputs;
    internal Dictionary<MED_ResourceType,int> Inputs;
}

// FIFO representation, including unproven units. External stat edits invalidate proof conservatively.
internal sealed class MED_MedicineStore
{
    readonly List<MED_MedicineUnit> units = new();
    internal void Sync(float value)
    {
        int count=Mathf.Max(0,Mathf.FloorToInt(value));
        if(count==units.Count)return;
        units.Clear();for(int i=0;i<count;i++)units.Add(null);
    }
    internal void Invalidate(float value) { units.Clear();for(int i=0;i<Mathf.FloorToInt(value);i++)units.Add(null); }
    internal void Add(MED_MedicineUnit unit) => units.Add(unit);
    internal MED_MedicineUnit Take() { if(units.Count==0)return null;var u=units[0];units.RemoveAt(0);return u; }
}

public partial class MED_PlayerResources
{
    readonly List<MED_MedicineUnit> medicineUnits = new();
    bool evidenceTransaction;
    public int EvidenceGeneration { get; private set; }
    // A new full session invalidates old/in-flight provenance, without changing actual inventory/health.
    public void BeginEvidenceSession() { EvidenceGeneration++;for(int i=0;i<medicineUnits.Count;i++)medicineUnits[i]=null; }
    public bool IsCurrent(MED_MedicineUnit unit) => unit!=null && unit.Patient==this && unit.Generation==EvidenceGeneration;
    void AdjustUnprovenMedicine(int value)
    {
        while(medicineUnits.Count>value)medicineUnits.RemoveAt(0);
        while(medicineUnits.Count<value)medicineUnits.Add(null);
    }
    void NotifyResource(MED_ResourceType type,int value)
    {
        if(OnResourceChanged==null)return;
        foreach(Action<MED_ResourceType,int> listener in OnResourceChanged.GetInvocationList())
            try {listener(type,value);}catch(Exception e){Debug.LogException(e);}
    }
    static bool Costs(List<MED_ResourceCost> source,out Dictionary<MED_ResourceType,int> values)
    {
        values=new();if(source==null||source.Count==0)return false;
        foreach(var c in source)
        {
            if(c.amount<=0||!Enum.IsDefined(typeof(MED_ResourceType),c.type)||c.type==MED_ResourceType.Health)return false;
            long sum=(long)(values.TryGetValue(c.type,out int n)?n:0)+c.amount;
            if(sum>int.MaxValue)return false;values[c.type]=(int)sum;
        }
        return true;
    }
    internal bool TryPayRecipe(MED_PrepBench bench,MED_Recipe recipe,out MED_CraftTicket ticket)
    {
        ticket=null;EnsureInitialized();
        if(evidenceTransaction||bench==null||recipe==null||!Costs(recipe.inputs,out var costs)||!Costs(recipe.outputs,out var outputs))return false;
        foreach(var cost in costs)if(Get(cost.Key)<cost.Value)return false;
        ticket=new MED_CraftTicket{Player=this,Bench=bench,Recipe=recipe,Generation=EvidenceGeneration,Outputs=outputs,Inputs=costs};
        evidenceTransaction=true;
        try
        {
            // All debits occur before any notification. Reentrant inventory actions cannot partially spend.
            foreach(var cost in costs){amounts[(int)cost.Key]-=cost.Value;if(cost.Key==MED_ResourceType.Medicine)AdjustUnprovenMedicine(amounts[(int)cost.Key]);}
            foreach(var cost in costs)NotifyResource(cost.Key,Get(cost.Key));
        }
        finally {evidenceTransaction=false;}
        return true;
    }
    internal bool CommitRecipe(MED_CraftTicket ticket)
    {
        if(evidenceTransaction||ticket==null||ticket.Player!=this||ticket.Consumed)return false;
        ticket.Consumed=true;evidenceTransaction=true;
        try
        {
            foreach(var output in ticket.Outputs)
            {
                int actual=(int)Math.Min(output.Value,(long)int.MaxValue-Get(output.Key));
                amounts[(int)output.Key]+=actual;
                if(output.Key==MED_ResourceType.Medicine)for(int i=0;i<actual;i++)medicineUnits.Add(ticket.Generation==EvidenceGeneration
                    ?new MED_MedicineUnit(this,ticket.Bench,ticket.Recipe,ticket.Generation):null);
            }
            foreach(var output in ticket.Outputs)NotifyResource(output.Key,Get(output.Key));
        }
        finally {evidenceTransaction=false;}
        return true;
    }
    internal bool RefundRecipe(MED_CraftTicket ticket)
    {
        if(evidenceTransaction||ticket==null||ticket.Player!=this||ticket.Consumed)return false;
        foreach(var input in ticket.Inputs)if((long)Get(input.Key)+input.Value>int.MaxValue)return false;
        ticket.Consumed=true; evidenceTransaction=true;
        try {foreach(var input in ticket.Inputs){amounts[(int)input.Key]=(int)Math.Min(int.MaxValue,(long)Get(input.Key)+input.Value);if(input.Key==MED_ResourceType.Medicine)AdjustUnprovenMedicine(Get(input.Key));}
        foreach(var input in ticket.Inputs)NotifyResource(input.Key,Get(input.Key));}
        finally{evidenceTransaction=false;} return true;
    }
    internal bool TransferMedicine(Action<MED_MedicineUnit> commit, bool requireCurrent=false)
    {
        EnsureInitialized();if(evidenceTransaction||Get(MED_ResourceType.Medicine)<1)return false;
        evidenceTransaction=true;
        try
        {
            AdjustUnprovenMedicine(Get(MED_ResourceType.Medicine));
            int index=requireCurrent?medicineUnits.FindIndex(IsCurrent):0;
            if(index<0)return false;
            var unit=medicineUnits[index];medicineUnits.RemoveAt(index);amounts[(int)MED_ResourceType.Medicine]--;
            commit(unit);NotifyResource(MED_ResourceType.Medicine,Get(MED_ResourceType.Medicine));
        }
        finally {evidenceTransaction=false;}
        return true;
    }
    internal bool ReceiveMedicine(Func<MED_MedicineUnit> take)
    {
        EnsureInitialized();if(evidenceTransaction||Get(MED_ResourceType.Medicine)==int.MaxValue)return false;
        evidenceTransaction=true;
        try {var unit=take();amounts[(int)MED_ResourceType.Medicine]++;medicineUnits.Add(IsCurrent(unit)?unit:null);NotifyResource(MED_ResourceType.Medicine,Get(MED_ResourceType.Medicine));}
        finally {evidenceTransaction=false;}
        return true;
    }
    internal bool ApplyDose(int healing,Action consume,out int before,out int after)
    {
        before=after=Health;if(evidenceTransaction||healing<=0||IsHealthFull)return false;
        evidenceTransaction=true;
        try {after=(int)Math.Min(maxHealth,(long)before+healing);amounts[(int)MED_ResourceType.Health]=after;consume();NotifyResource(MED_ResourceType.Health,after);}
        finally {evidenceTransaction=false;}
        return after>before;
    }
}
