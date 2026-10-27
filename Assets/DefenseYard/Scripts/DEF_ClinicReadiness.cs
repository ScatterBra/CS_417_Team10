using System.Collections.Generic;
using UnityEngine;

// Clinic patient health is separate from DEF_GameState's base health.
public sealed class DEF_ClinicReadiness : DEF_PrerequisiteSource
{
    public MED_PlayerResources patient;
    public MED_PrepBench bench;
    public MED_Recipe medicineRecipe;
    public MED_TreatmentStation station;
    [Tooltip("Optional production binding: accept fresh OnBuilt events only inside this configured Clinic container.")]
    public Transform buildingsRoot;
    public MED_BuildingDefinition benchDefinition,stationDefinition;
    Transform sessionRoot;
    MED_BuildingDefinition sessionBenchDefinition,sessionStationDefinition;
    bool subscribed;
    MED_PlayerResources sessionPatient;
    MED_PrepBench sessionBench;
    MED_Recipe sessionRecipe;
    MED_TreatmentStation observedStation;
    readonly HashSet<MED_PrepBench> eligibleBenches=new();
    readonly HashSet<MED_TreatmentStation> eligibleStations=new();
    readonly HashSet<string> receipts=new();
    int generation,expectedHealth;
    bool armed,complete,published;
    public string PendingReason {get;private set;}="Clinic sources not armed.";
    public override DEF_ProgressionState.Prerequisite Kind=>DEF_ProgressionState.Prerequisite.Clinic10;
    public override void BeginSession(int session)
    {
        base.BeginSession(session);
        foreach(var source in eligibleStations)if(source!=null)source.DoseCommitted-=Dose;
        eligibleBenches.Clear();eligibleStations.Clear();
        sessionPatient=patient;sessionBench=bench;sessionRecipe=medicineRecipe;observedStation=station;
        sessionRoot=buildingsRoot;sessionBenchDefinition=benchDefinition;sessionStationDefinition=stationDefinition;
        if(sessionRoot!=null){bench=sessionBench=null;station=observedStation=null;}
        receipts.Clear();complete=published=false;
        if(patient!=null)patient.BeginEvidenceSession();
        generation=patient!=null?patient.EvidenceGeneration:0;
        expectedHealth=patient!=null?patient.Health:0;
        armed=session>0 && patient!=null && ((bench!=null && station!=null)||(sessionRoot!=null && benchDefinition!=null && stationDefinition!=null)) && medicineRecipe!=null &&
            patient.MaxHealth==100 && expectedHealth==MED_PlayerResources.DefaultStartingHealth;
        if(sessionBench!=null)eligibleBenches.Add(sessionBench);
        if(observedStation!=null){eligibleStations.Add(observedStation);observedStation.DoseCommitted+=Dose;}
        PendingReason=armed?"Craft Medicine, stock this treatment station and heal the Clinic patient from 50 to 100 on its pad.":
            "Clinic needs configured sources and a fresh patient at 50/100. Reload the full run; Defense retry preserves readiness.";
    }
    bool Current=>armed && Session>0 && patient==sessionPatient && medicineRecipe==sessionRecipe &&
        (sessionRoot!=null ? buildingsRoot==sessionRoot && benchDefinition==sessionBenchDefinition && stationDefinition==sessionStationDefinition :
            bench==sessionBench && station==observedStation && bench!=null && station!=null) &&
        patient!=null && medicineRecipe!=null && patient.EvidenceGeneration==generation;
    void Built(MED_Building building)
    {
        if(!armed||Session<=0||building==null||sessionRoot==null||buildingsRoot!=sessionRoot||
            benchDefinition!=sessionBenchDefinition||stationDefinition!=sessionStationDefinition||!building.transform.IsChildOf(sessionRoot)||building.Cells.Length==0)return;
        if(building.Definition==sessionBenchDefinition)
        {
            var source=building.GetComponentInChildren<MED_PrepBench>();
            if(source!=null){eligibleBenches.Add(source);if(bench==null)bench=sessionBench=source;}
        }
        if(building.Definition==sessionStationDefinition)
        {
            var source=building.GetComponentInChildren<MED_TreatmentStation>();
            if(source!=null && eligibleStations.Add(source))source.DoseCommitted+=Dose;
            if(station==null)station=observedStation=source;
        }
    }
    void Removed(MED_Building building)
    {
        // Crafted units remain legitimate after their bench is demolished. Stop listening to removed stations.
        var remove=new List<MED_TreatmentStation>();
        foreach(var source in eligibleStations)if(source==null || (building!=null && source.transform.IsChildOf(building.transform)))remove.Add(source);
        foreach(var source in remove){if(source!=null)source.DoseCommitted-=Dose;eligibleStations.Remove(source);}
    }
    void Dose(MED_TreatmentStation source,MED_PlayerResources target,MED_MedicineUnit unit,int before,int after)
    {
        if(!Current||published||!eligibleStations.Contains(source)||target!=sessionPatient||!target.IsCurrent(unit)||
            !eligibleBenches.Contains(unit.Bench)||unit.Recipe!=sessionRecipe||receipts.Contains(unit.Id))return;
        if(before!=expectedHealth || after<=before || after>100){armed=false;PendingReason="Patient health changed outside the verified Medicine chain. Reload the full run.";return;}
        receipts.Add(unit.Id);expectedHealth=after;complete=after==100;Check();
    }
    void Check()
    {
        if(!isActiveAndEnabled||!Current||published||!complete||patient.Health!=100)return;
        var training=DEF_ClinicTraining.For(this);if(training!=null&&!training.ReadyForTreatment)return;
        published=true;PendingReason="Fresh crafted Medicine was stocked and successfully treated this patient to 100/100.";
        Publish("clinic:"+patient.GetEntityId()+":"+generation);
    }
    void OnEnable(){if(!subscribed){MED_Building.OnBuilt+=Built;MED_Building.OnRemoved+=Removed;subscribed=true;}Check();}
    void OnDestroy(){foreach(var source in eligibleStations)if(source!=null)source.DoseCommitted-=Dose;MED_Building.OnBuilt-=Built;MED_Building.OnRemoved-=Removed;}
}
