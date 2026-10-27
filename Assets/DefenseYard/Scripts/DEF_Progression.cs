using UnityEngine;
using S = DEF_ProgressionState.Stage;

[RequireComponent(typeof(DEF_GameState))]
public sealed class DEF_Progression : MonoBehaviour
{
    [SerializeField] DEF_PrerequisiteSource power, clinic, workshop;
    [SerializeField] DEF_ProgressionPresenter presenter;
    [SerializeField] SCV_ResourceManager repairLedger;
    DEF_WorkshopRepairReceiver trainingRepairReceiver;
    readonly System.Collections.Generic.List<DEF_Turret> attemptDefenses=new();
    readonly System.Collections.Generic.List<MonoBehaviour> advancedDefenses=new();
    int? retryScrap;
    int retryAmmo;
    int deliveryDepth;
    bool queuedRetry,queuedNewSession;
    internal sealed class DeliveryScope : System.IDisposable
    {
        readonly DEF_Progression owner;
        readonly int session,attempt;
        bool disposed;
        internal DeliveryScope(DEF_Progression owner)
        {this.owner=owner;session=owner.State.Session;attempt=owner.State.Attempt;owner.deliveryDepth++;}
        internal bool Matches => !disposed && owner!=null && Current==owner && owner.State.Session==session && owner.State.Attempt==attempt;
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            if(owner==null)return;
            owner.deliveryDepth--;
            if(owner.deliveryDepth!=0)return;
            bool full=owner.queuedNewSession,retry=owner.queuedRetry;
            owner.queuedNewSession=owner.queuedRetry=false;
            if(full)owner.BeginNewSession();else if(retry)owner.BeginDefenseRetry();
        }
    }
    internal DeliveryScope BeginDelivery()=>new DeliveryScope(this);
    internal bool DeferReset(bool full)
    {
        if(deliveryDepth==0)return false;
        if(full)queuedNewSession=true;else queuedRetry=true;
        return true;
    }
    [Header("Explicit training baseline applied only at successful first sentry placement")]
    [Range(1,99)] public float trainingDamage = 20;
    [Min(1)] public int trainingAmmo = 30;
    public DEF_ProgressionState State { get; } = new();
    public string LastFailure { get; private set; }
    public static DEF_Progression Current => DEF_GameState.I != null ? DEF_GameState.I.GetComponent<DEF_Progression>() : null;
    public static bool Allows(DEF_Action action)
    {
        var p = Current;
        return p == null || p.Check(action);
    }
    public bool IsAllowed(DEF_Action action)
    {
        if (!isActiveAndEnabled || presenter == null || !presenter.isActiveAndEnabled || !presenter.Ready) return false;
        if(GetComponent<DEF_GameState>().playerHealth<=0 || (DEF_WaveManager.I!=null && DEF_WaveManager.I.AttemptFailed))
            return action==DEF_Action.StartWave;
        var stage = State.Current;
        switch (action)
        {
            case DEF_Action.BasicSentry: return stage >= S.PlaceSentry;
            case DEF_Action.Repair: return stage == S.Repair || stage == S.Ammo || stage == S.WaveReady || stage == S.WaveActive || (State.AdvancedUnlocked && !State.IntroductionActive);
            case DEF_Action.PhysicalAmmo: return stage == S.Ammo || stage == S.WaveReady || stage == S.WaveActive || (State.AdvancedUnlocked && !State.IntroductionActive);
            case DEF_Action.StartWave: return stage == S.WaveReady || stage==S.AdvancedPractice || stage==S.MissilePractice || stage==S.JuggernautReady;
            case DEF_Action.Combat: return !State.IntroductionActive && (stage==S.WaveActive || (stage>=S.AdvancedPractice && stage<S.Complete && DEF_WaveManager.I!=null && (DEF_WaveManager.I.phase==DEF_WaveManager.Phase.Spawning || DEF_WaveManager.I.phase==DEF_WaveManager.Phase.Active)));
            case DEF_Action.Barricade:
            case DEF_Action.Shock: return State.AdvancedUnlocked;
            case DEF_Action.Missile: return State.MissileUnlocked;
            // Remote showcase actions and legacy ammo crates remain unavailable in progression.
            default: return false;
        }
    }
    public bool ValidatePlacement(int x,int z,int price)
    {
        var grid=FindAnyObjectByType<DEF_GridManager>();var path=DEF_Pathfinder.I;var state=GetComponent<DEF_GameState>();
        if(grid==null || path==null || !path.Ready)return Reject("place defense","the build grid is unavailable","Restore the Defense grid setup before placing a defense.");
        if(x<0 || z<0 || x>=grid.width || z>=grid.depth)return Reject("place defense","the tile is outside the build grid","Aim at a valid ground tile outside the base.");
        if((DEF_BaseStructure.Instance!=null && DEF_BaseStructure.Instance.Contains(grid.TileCenter(x,z),.5f)) || path.BaseCell()==new Vector2Int(x,z))return Reject("place defense","the tile is inside the base","Choose an empty ground tile outside the base.");
        if(path.IsBlocked(x,z)||path.OccupantAt(x,z)!=null)return Reject("place defense","the tile is occupied","Choose an empty valid ground tile.");
        if(state.scrap<price)return Reject("place defense","there is not enough scrap","Transfer Workshop scrap before building.");
        return true;
    }
    public bool Reject(string action, string reason, string remedy)
    {
        LastFailure = "Cannot " + action + ": " + reason + ". " + remedy;
        presenter?.ShowFailure(LastFailure); return false;
    }
    public bool Check(DEF_Action action)
    {
        if (IsAllowed(action)) return true;
        LastFailure = "Cannot " + action + ": this action is locked at " + State.Current + ". " + NextGoal;
        presenter?.ShowFailure(LastFailure);
        return false;
    }
    public string NextGoal => State.Current switch
    {
        S.TeamPrerequisites => "Complete Power Step6, Clinic Step10, and Workshop crafting plus physical teaching delivery (Step14).",
        S.PlaceSentry => "Place one basic sentry on valid empty ground.",
        S.RepairIntroduction or S.AmmoIntroduction or S.WaveIntroduction => "Read the introduction; the next action unlocks after it finishes.",
        S.Repair => "Release a crafted RepairKit in the training sentry receiver to restore integrity.",
        S.Ammo => "Physically deliver a crafted Ammo Box to refill Defense.",
        S.WaveReady => "Start the first wave when ready.",
        S.WaveActive => "Clear wave1 while keeping the base alive.",
        S.FirstWaveComplete => "Read the Barricade and Shock introduction before their controls appear.",
        S.AdvancedPractice => "Start a practice wave. Place a barricade across a moving enemy route and let it walk the detour. Also stun a live enemy with Shock.",
        S.MissileIntroduction => "Read the Missile introduction before its controls appear.",
        S.MissilePractice => "Start or finish a practice wave; damage two distinct enemies with one missile explosion.",
        S.JuggernautReady => "Finish any active practice wave, then start the Juggernaut challenge. Use two defense types, repair and refill during that wave.",
        S.JuggernautActive => "Clear the Juggernaut wave with two defense types contributing, physical repair and refill, and the base alive.",
        _ => "Defense progression complete."
    };
    void OnEnable()
    {
        Connect(power); Connect(clinic); Connect(workshop);
        Refresh();
    }
    void Connect(DEF_PrerequisiteSource source)
    { if (source != null) { source.Completed -= Accept; source.Completed += Accept; source.ResumeSession(State.Session, Accept); } }
    void OnDisable()
    { if (power != null) power.Completed -= Accept; if (clinic != null) clinic.Completed -= Accept; if (workshop != null) workshop.Completed -= Accept; }
    void Accept(DEF_PrerequisiteSource source, int session, string receipt)
    {
        bool configured = source != null && ((source == power && source.Kind == DEF_ProgressionState.Prerequisite.Power6) ||
            (source == clinic && source.Kind == DEF_ProgressionState.Prerequisite.Clinic10) ||
            (source == workshop && source.Kind == DEF_ProgressionState.Prerequisite.Workshop14));
        if (configured && State.AcceptPrerequisite(source.Kind, session, receipt)) Refresh();
    }
    internal void Placed(DEF_Turret turret)
    {
        if(turret==null)return;
        if(!attemptDefenses.Contains(turret))attemptDefenses.Add(turret);
        if(!retryScrap.HasValue){retryScrap=Mathf.Min(GetComponent<DEF_GameState>().maxScrap,GetComponent<DEF_GameState>().scrap+turret.cost);retryAmmo=GetComponent<DEF_GameState>().ammo;}
        if (turret.explosive || !State.Place(State.Session, State.Attempt, turret.GetEntityId().ToString())) {RegisterAdvanced(turret);return;}
        // This is an explicit tutorial baseline, not an unrelated startup mutation.
        turret.health = Mathf.Max(1, turret.maxHealth - Mathf.Clamp(trainingDamage, 1, turret.maxHealth - 1));
        var receiverObject = new GameObject("Physical RepairKit intake");
        receiverObject.transform.SetParent(turret.transform, false); receiverObject.transform.localPosition = Vector3.up;
        trainingRepairReceiver = receiverObject.AddComponent<DEF_WorkshopRepairReceiver>();
        receiverObject.GetComponent<BoxCollider>().size = Vector3.one * 1.4f;
        receiverObject.GetComponent<BoxCollider>().isTrigger = true;
        trainingRepairReceiver.target = turret; trainingRepairReceiver.ledger = repairLedger;
        var state = GetComponent<DEF_GameState>(); state.ammo = Mathf.Clamp(trainingAmmo, 0, Mathf.Max(0, state.maxAmmo - 1));
        Refresh();
    }
    internal void PhysicallyRepaired(DEF_WorkshopRepairReceiver source,MonoBehaviour target,float before,float after,string receipt)
    {
        if(source==null || source!=trainingRepairReceiver || source.target!=target || target==null ||
            repairLedger==null || source.ledger!=repairLedger || string.IsNullOrEmpty(receipt) || !(before>0) || !(after>before))return;
        if(State.Repair(State.Session,State.Attempt,target.GetEntityId().ToString(),after-before))Refresh();
    }
    internal void Refilled(int amount,string identity)
    {if(amount>0)State.RecordSustainment(State.Session,State.Attempt,false,identity);if(State.Refill(State.Session,State.Attempt,amount,identity))Refresh();}
    internal void RegisterAdvanced(MonoBehaviour defense)
    {
        if(defense==null || advancedDefenses.Contains(defense))return;
        advancedDefenses.Add(defense);
        var go=new GameObject("Physical RepairKit intake");go.transform.SetParent(defense.transform,false);go.transform.localPosition=Vector3.up;
        var receiver=go.AddComponent<DEF_WorkshopRepairReceiver>();receiver.target=defense;receiver.ledger=repairLedger;
        go.GetComponent<BoxCollider>().size=Vector3.one*1.4f;go.GetComponent<BoxCollider>().isTrigger=true;
    }
    internal void CombatRepair(DEF_WorkshopRepairReceiver receiver,MonoBehaviour target,float before,float after,string receipt)
    {
        if(receiver!=null && receiver.target==target && receiver.ledger==repairLedger && repairLedger!=null && after>before && before>0 &&
           ((target is DEF_Turret t && attemptDefenses.Contains(t)) || advancedDefenses.Contains(target)))
            State.RecordSustainment(State.Session,State.Attempt,true,receipt);
    }
    internal bool ValidEnemy(DEF_Zombie enemy)=>enemy!=null && DEF_WaveManager.I!=null && DEF_WaveManager.I.OwnsActor(enemy) && IsAllowed(DEF_Action.Combat);
    internal void Control(MonoBehaviour defense,DEF_Zombie enemy,bool stun,string receipt)
    {
        if(!ValidEnemy(enemy) || !advancedDefenses.Contains(defense))return;
        State.RecordCombat(State.Session,State.Attempt,stun?"Shock":"Barricade",receipt,enemy.kind==DEF_Zombie.Kind.Brute);
        if(State.RecordControl(State.Session,State.Attempt,receipt,stun))Refresh();
    }
    internal void BarricadeIntercept(DEF_Barricade defense,DEF_Zombie enemy)
    {
        if(!ValidEnemy(enemy) || !advancedDefenses.Contains(defense))return;
        // Damage absorption is combat contribution, never proof of route redirection.
        State.RecordCombat(State.Session,State.Attempt,"Barricade",defense.GetEntityId()+":intercept:"+enemy.GetEntityId(),enemy.kind==DEF_Zombie.Kind.Brute);
    }
    internal void ProjectileDamage(DEF_Projectile projectile,DEF_Zombie enemy,float delta)
    {
        if(!(delta>0) || !ValidEnemy(enemy) || !DEF_WaveManager.I.OwnsProjectile(projectile))return;
        State.RecordCombat(State.Session,State.Attempt,projectile.explosive?"Missile":"Sentry",projectile.GetEntityId()+":"+enemy.GetEntityId(),enemy.kind==DEF_Zombie.Kind.Brute);
    }
    internal void Splash(DEF_Projectile projectile,int victims)
    {
        if(projectile==null || !projectile.explosive || !IsAllowed(DEF_Action.Combat) || DEF_WaveManager.I==null || !DEF_WaveManager.I.OwnsProjectile(projectile))return;
        if(State.RecordSplash(State.Session,State.Attempt,projectile.GetEntityId().ToString(),victims))Refresh();
    }
    internal bool WaveStarting() { bool ok = State.BeginWave(State.Session, State.Attempt); if (ok) Refresh(); return ok; }
    internal void WaveCleared(int wave, float baseHealth)
    { if (State.ClearWave(State.Session, State.Attempt, wave, baseHealth)) Refresh(); }
    internal void IntroductionFinished(DEF_ProgressionPresenter source, int session, int attempt, int intro)
    { if (source == presenter && source.isActiveAndEnabled && State.CompleteIntroduction(session, attempt, intro)) Refresh(); }
    public void BeginDefenseRetry() { DEF_WaveManager.I?.TryRetryDefense(); }
    internal void ResetDefenseAttempt()
    {
        ResetOwnedDefenses();
        State.RetryDefense(); Refresh();
    }
    void ResetOwnedDefenses()
    {
        foreach(var turret in attemptDefenses)if(turret!=null){turret.ReleaseCell();DEF_WaveManager.Retire(turret.gameObject);}
        attemptDefenses.Clear();trainingRepairReceiver=null;
        foreach(var defense in advancedDefenses)if(defense!=null)
        {
            if(defense is DEF_Barricade b)b.ReleaseCell();
            if(defense is DEF_ShockTrap t)t.ReleaseCell();
            DEF_WaveManager.Retire(defense.gameObject);
        }
        advancedDefenses.Clear();
        var state=GetComponent<DEF_GameState>();
        state.playerHealth=state.maxHealth;state.waveNumber=0;state.selectedTile=new Vector2Int(-1,-1);
        if(retryScrap.HasValue){state.scrap=Mathf.Clamp(retryScrap.Value,0,state.maxScrap);state.ammo=Mathf.Clamp(retryAmmo,0,state.maxAmmo);}
    }
    public void BeginNewSession()
    {
        if(DeferReset(true))return;
        var training=DEF_ClinicTraining.For(this);
        if(Application.isPlaying && training!=null){training.RestartFullRun();return;}
        DEF_WaveManager.I?.ResetOwnedWave();ResetOwnedDefenses();retryScrap=null;
        State.NewSession();power?.BeginSession(State.Session);clinic?.BeginSession(State.Session);workshop?.BeginSession(State.Session);Refresh();
    }
    void Refresh()
    {
        if (presenter != null) presenter.Present(this);
        var input=DEF_VRInput.Instance;
        if(input!=null && input.BuildArmed && !IsAllowed(input.mode==DEF_VRInput.PlaceMode.Turret?DEF_Action.BasicSentry:input.mode==DEF_VRInput.PlaceMode.BoomTurret?DEF_Action.Missile:input.mode==DEF_VRInput.PlaceMode.Barricade?DEF_Action.Barricade:DEF_Action.Shock))input.CancelBuild();
    }
}
public enum DEF_Action { BasicSentry, Repair, PhysicalAmmo, RemoteAmmo, AmmoCrate, StartWave, Combat, Barricade, Shock, Missile }


