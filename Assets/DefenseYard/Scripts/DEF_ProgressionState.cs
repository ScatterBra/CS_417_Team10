using System;
using System.Collections.Generic;

// Deterministic first-wave milestone. No UI or saved flags can advance it.
public sealed class DEF_ProgressionState
{
    public enum Stage { TeamPrerequisites, PlaceSentry, RepairIntroduction, Repair, AmmoIntroduction, Ammo, WaveIntroduction, WaveReady, WaveActive, FirstWaveComplete, AdvancedPractice, MissileIntroduction, MissilePractice, JuggernautReady, JuggernautActive, Complete }
    public enum Prerequisite { Power6, Clinic10, Workshop14 }
    public Stage Current { get; private set; } = Stage.TeamPrerequisites;
    public int Session { get; private set; } = 1;
    public int Attempt { get; private set; } = 1;
    public int Introduction { get; private set; }
    readonly HashSet<Prerequisite> ready = new();
    readonly HashSet<string> receipts = new();
    string sentry;
    bool routeControlled, shocked, juggernautSeen, combatRepair, combatRefill;
    readonly HashSet<string> defenseTypes=new();
    public bool AdvancedUnlocked => Current>=Stage.AdvancedPractice;
    public bool MissileUnlocked => Current>=Stage.MissilePractice;
    public bool IntroductionActive => Current==Stage.RepairIntroduction || Current==Stage.AmmoIntroduction || Current==Stage.WaveIntroduction || Current==Stage.FirstWaveComplete || Current==Stage.MissileIntroduction;
    public bool RecordControl(int session,int attempt,string receipt,bool stun)
    {
        if(!Matches(session,attempt) || Current!=Stage.AdvancedPractice || string.IsNullOrEmpty(receipt) || !receipts.Add("control:"+receipt))return false;
        if(stun)shocked=true;else routeControlled=true;
        if(routeControlled && shocked)BeginIntroduction(Stage.MissileIntroduction);
        return true;
    }
    public bool RecordSplash(int session,int attempt,string explosion,int distinctPositiveVictims)
    {
        if(!Matches(session,attempt) || Current!=Stage.MissilePractice || distinctPositiveVictims<2 || string.IsNullOrEmpty(explosion) || !receipts.Add("splash:"+explosion))return false;
        Current=Stage.JuggernautReady;return true;
    }
    public bool RecordCombat(int session,int attempt,string defense,string receipt,bool brute)
    {
        if(!Matches(session,attempt) || Current!=Stage.JuggernautActive || string.IsNullOrEmpty(receipt))return false;
        if(defense!="Sentry" && defense!="Missile" && defense!="Shock" && defense!="Barricade")return false;
        if(!receipts.Add("combat:"+receipt))return false;
        defenseTypes.Add(defense);juggernautSeen|=brute;return true;
    }
    public void RecordSustainment(int session,int attempt,bool repair,string receipt)
    {
        if(!Matches(session,attempt) || Current!=Stage.JuggernautActive || string.IsNullOrEmpty(receipt) || !receipts.Add("sustain:"+receipt))return;
        if(repair)combatRepair=true;else combatRefill=true;
    }
    void ClearAdvanced()
    {routeControlled=shocked=juggernautSeen=combatRepair=combatRefill=false;defenseTypes.Clear();}
        public bool WorkshopRecording { get; private set; }
    public void ConfigureWorkshopRecording()
    {
        if(Current!=Stage.TeamPrerequisites || ready.Count!=0)throw new InvalidOperationException("Configure before evidence");
        WorkshopRecording=true;
    }
    public bool TeamReady => WorkshopRecording ? ready.Contains(Prerequisite.Workshop14) : ready.Count == 3;
    public bool AcceptPrerequisite(Prerequisite kind, int session, string receipt)
    {
        if ((WorkshopRecording && kind != Prerequisite.Workshop14) || session != Session || Current != Stage.TeamPrerequisites || string.IsNullOrEmpty(receipt) ||
            !Enum.IsDefined(typeof(Prerequisite), kind) || !receipts.Add(kind + ":" + receipt)) return false;
        ready.Add(kind);
        if (TeamReady) Current = Stage.PlaceSentry;
        return true;
    }
    public bool Place(int session, int attempt, string instance)
    {
        if (!Matches(session, attempt) || Current != Stage.PlaceSentry || string.IsNullOrEmpty(instance)) return false;
        sentry = instance; BeginIntroduction(Stage.RepairIntroduction); return true;
    }
    public bool Repair(int session, int attempt, string instance, float positiveDelta)
    {
        if (!Matches(session, attempt) || Current != Stage.Repair || sentry != instance || !(positiveDelta > 0) || float.IsInfinity(positiveDelta)) return false;
        BeginIntroduction(Stage.AmmoIntroduction); return true;
    }
    public bool Refill(int session, int attempt, int amount, string receipt)
    {
        if (!Matches(session, attempt) || Current != Stage.Ammo || amount <= 0 || string.IsNullOrEmpty(receipt) || !receipts.Add("refill:" + Attempt + ":" + receipt)) return false;
        BeginIntroduction(Stage.WaveIntroduction); return true;
    }
    void BeginIntroduction(Stage stage) { Introduction++; Current = stage; }
    public bool CompleteIntroduction(int session, int attempt, int introduction)
    {
        if (!Matches(session, attempt) || introduction != Introduction) return false;
        if (Current == Stage.RepairIntroduction) Current = Stage.Repair;
        else if (Current == Stage.AmmoIntroduction) Current = Stage.Ammo;
        else if (Current == Stage.WaveIntroduction) Current = Stage.WaveReady;
        else if(Current==Stage.FirstWaveComplete)Current=Stage.AdvancedPractice;
        else if(Current==Stage.MissileIntroduction)Current=Stage.MissilePractice;
        else return false;
        return true;
    }
    public bool BeginWave(int session, int attempt)
    {
        if(!Matches(session,attempt))return false;
        if(Current==Stage.AdvancedPractice || Current==Stage.MissilePractice)return true;
        if(Current==Stage.JuggernautReady){defenseTypes.Clear();juggernautSeen=combatRepair=combatRefill=false;Current=Stage.JuggernautActive;return true;}
        if(Current!=Stage.WaveReady)return false;
        Current = Stage.WaveActive; return true;
    }
    public bool ClearWave(int session, int attempt, int wave, float baseHealth)
    {
        if(!Matches(session,attempt) || !(baseHealth>0))return false;
        if(Current==Stage.JuggernautActive)
        {
            if(wave<3)return false;
            Current=juggernautSeen && defenseTypes.Count>=2 && combatRepair && combatRefill ? Stage.Complete : Stage.JuggernautReady;
            return true;
        }
        if(Current!=Stage.WaveActive || wave!=1)return false;
        BeginIntroduction(Stage.FirstWaveComplete); return true;
    }
    public void RetryDefense() { Attempt++; ClearAdvanced(); sentry = null; Introduction++; Current = TeamReady ? Stage.PlaceSentry : Stage.TeamPrerequisites; }
    public void NewSession() { Session++; ClearAdvanced(); Attempt = 1; sentry = null; Introduction++; ready.Clear(); receipts.Clear(); Current = Stage.TeamPrerequisites; }
    bool Matches(int session, int attempt) => session == Session && attempt == Attempt;
}


