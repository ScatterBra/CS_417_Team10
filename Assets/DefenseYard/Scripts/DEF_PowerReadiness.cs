using Deadzone.Power;
using UnityEngine;

// Fresh source events are required: global StationComplete alone is never evidence.
public sealed class DEF_PowerReadiness : DEF_PrerequisiteSource
{
    public PWR_LevelDirector director;
    public PWR_CellCharger charger;
    public PWR_Floodlight floodlight;
    bool inserted, charged, broken, replaced, collected, published, armed, receivedPower;
    PWR_Grabbable output;
    PWR_LevelDirector sessionDirector;
    PWR_CellCharger sessionCharger;
    PWR_Floodlight sessionLight;
    bool subscribed;
    public string PendingReason { get; private set; } = "Power sources not armed.";
    public override DEF_ProgressionState.Prerequisite Kind => DEF_ProgressionState.Prerequisite.Power6;

    bool Configured => director != null && charger != null && floodlight != null &&
        director.charger == charger && director.floodlight == floodlight &&
        (Session<=0 || (director==sessionDirector && charger==sessionCharger && floodlight==sessionLight)) &&
        director.isActiveAndEnabled && charger.isActiveAndEnabled && floodlight.isActiveAndEnabled;

    public override void BeginSession(int session)
    {
        base.BeginSession(session);
        sessionDirector=director;sessionCharger=charger;sessionLight=floodlight;
        inserted = charged = broken = replaced = collected = published = receivedPower = false;
        output = null;
        armed = session > 0 && Configured && !charger.HasBattery && !charger.IsComplete &&
            !charger.IsCellCollected && !floodlight.BulbReplaced;
        PendingReason = armed ? "Complete this station's charge, bulb replacement and exact output pickup." :
            "Power needs configured fresh sources. Reload the full run; Defense retry retains earned readiness.";
    }
    void OnEnable()
    {
        if(subscribed){Check();return;}
        subscribed=true;
        PWR_CellCharger.BatteryInserted += Inserted;
        PWR_CellCharger.Completed += Charged;
        PWR_CellCharger.CellCollected += Collected;
        PWR_Floodlight.BulbBroken += Broken;
        PWR_Floodlight.BulbInstalled += Replaced;
        PWR_PowerNode.OnTransfer += Transfer;
        Check();
    }
    // Keep observing genuine source events while this presenter/adapter is temporarily disabled.
    // Publishing waits until enabled; new sessions still clear all evidence.
    void OnDestroy()
    {
        PWR_CellCharger.BatteryInserted -= Inserted;
        PWR_CellCharger.Completed -= Charged;
        PWR_CellCharger.CellCollected -= Collected;
        PWR_Floodlight.BulbBroken -= Broken;
        PWR_Floodlight.BulbInstalled -= Replaced;
        PWR_PowerNode.OnTransfer -= Transfer;
        subscribed=false;
    }
    bool Accept() => armed && !published && Session > 0 && Configured;
    void Transfer(PWR_PowerNode from, PWR_PowerNode to, float amount)
    {
        if (Accept() && inserted && from != null && from.role == PWR_PowerRole.Storage &&
            to == charger.GetComponent<PWR_PowerNode>() && amount > 0f && !float.IsInfinity(amount)) receivedPower = true;
    }
    void Inserted(PWR_CellCharger source) { if (Accept() && source == charger && source.HasBattery) inserted = true; }
    void Charged(PWR_CellCharger source)
    {
        if (!Accept() || source != charger || !inserted || !receivedPower || !source.IsComplete || source.ProgressPercent < 100f || source.ChargedCell == null) return;
        output = source.ChargedCell;
        charged = true;
        Check();
    }
    void Collected(PWR_CellCharger source)
    {
        if (!Accept() || source != charger || !charged || output == null || source.ChargedCell != output || !source.IsCellCollected) return;
        collected = true;
        Check();
    }
    void Broken(PWR_Floodlight source) { if (Accept() && source == floodlight && !source.HasBulb) broken = true; }
    void Replaced(PWR_Floodlight source)
    {
        if (!Accept() || source != floodlight || !broken || !source.BulbReplaced) return;
        replaced = true;
        Check();
    }
    void Check()
    {
        if (!isActiveAndEnabled || !Accept() || !charged || !collected || !broken || !replaced || !floodlight.BulbReplaced) return;
        published = true;
        PendingReason = "Power charge, replacement and pickup verified for this session.";
        Publish("power:" + charger.GetEntityId() + ":" + output.GetEntityId());
    }
}
