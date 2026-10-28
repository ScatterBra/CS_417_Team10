using TMPro;
using UnityEngine;
using S = DEF_ProgressionState.Stage;

// Owns only its own texts/roots; never writes Felix's wrist or objective fields.
public sealed class DEF_ProgressionPresenter : MonoBehaviour
{
    public TMP_Text introductionText, goalText, failureText;
    [Min(.1f)] public float introductionSeconds = 4;
    public GameObject[] repairContent, ammoContent, waveContent, advancedContent, missileContent;
    DEF_Progression owner;
    int session, attempt, introduction;
    float finishAt;
    bool presenting;
    string displayedFailure;
    float failureExpires;
    float lastFailureCall=float.NegativeInfinity;
    int failureSession=-1,failureAttempt=-1,failureStage=-1;
    public bool Ready => introductionText != null && goalText != null;
    public void Present(DEF_Progression progression)
    {
        owner = progression;
        if (!Ready) return;
        var state = owner.State;
        if(failureSession!=state.Session||failureAttempt!=state.Attempt||failureStage!=(int)state.Current)
        {
            failureSession=state.Session;failureAttempt=state.Attempt;failureStage=(int)state.Current;
            displayedFailure=null;if(failureText!=null)failureText.text=string.Empty;
        }
        Set(repairContent, owner.IsAllowed(DEF_Action.Repair));
        Set(ammoContent, owner.IsAllowed(DEF_Action.PhysicalAmmo));
        Set(waveContent, owner.IsAllowed(DEF_Action.StartWave) || owner.IsAllowed(DEF_Action.Combat));
        Set(advancedContent, owner.IsAllowed(DEF_Action.Barricade));
        Set(missileContent,owner.IsAllowed(DEF_Action.Missile));
        goalText.text = (owner.workshopRecording ? "NEXT: WORKSHOP TO DEFENSE (SIMPLIFIED)\n" : "") + owner.NextGoal;
        bool intro = state.IntroductionActive;
        introductionText.gameObject.SetActive(intro);
        if (!intro) { presenting = false; return; }
        if (presenting && session == state.Session && attempt == state.Attempt && introduction == state.Introduction) return;
        session = state.Session; attempt = state.Attempt; introduction = state.Introduction;
        introductionText.text = state.Current == S.RepairIntroduction ? "REPAIR TRAINING\nYour placed sentry will receive training damage. Release a crafted RepairKit inside its receiver to restore integrity." :
            state.Current == S.AmmoIntroduction ? "AMMO TRAINING\nBring a crafted Workshop Ammo Box to the Defense intake. One box adds up to10 rounds." :
            state.Current==S.FirstWaveComplete ? "BARRICADE AND SHOCK\nPlace a barricade across a moving enemy route and observe it walk the detour. Also stun a live enemy with Shock. Both are required before Missile unlocks." :
            state.Current==S.MissileIntroduction ? "MISSILE SENTRY\nDamage at least two distinct enemies with one explosion. Then sustain a Juggernaut wave using two defense types, a physical repair and refill." :
            "FIRST WAVE\nPreparation is complete. Start wave1 and protect the base.";
        finishAt = Time.unscaledTime + Mathf.Max(.1f, introductionSeconds); presenting = true;
    }
    void Update()
    {
        if(failureText!=null&&Time.unscaledTime>=failureExpires)failureText.text=string.Empty;
        if (!presenting || !Ready || !introductionText.gameObject.activeInHierarchy || Time.unscaledTime < finishAt) return;
        presenting = false;
        owner.IntroductionFinished(this, session, attempt, introduction);
    }
    public void ShowFailure(string message)
    {
        // Repeated polling of the same rejected action must not pin its card forever.
        // A different failure or successful progression context permits a fresh card.
        bool polling=displayedFailure==message&&Time.unscaledTime-lastFailureCall<.5f;
        lastFailureCall=Time.unscaledTime;
        if(polling)return;
        displayedFailure=message;failureExpires=Time.unscaledTime+4f;
        if(failureText!=null)failureText.text=message;
    }
    static void Set(GameObject[] objects, bool active)
    { if (objects != null) foreach (var obj in objects) if (obj != null) obj.SetActive(active); }
}

