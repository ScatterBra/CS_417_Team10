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
    float finishAt, failureUntil;
    DEF_GridManager defenseGrid;
    bool integrated;
    bool presenting;
    float noticeUntil, completedUntil;
    int observedSession, observedAttempt;
    S observedStage;
    bool observed;
    string completedText;

    public bool Ready => introductionText != null && goalText != null;
    public void Present(DEF_Progression progression)
    {
        owner = progression;
        if (!Ready) return;
        var state = owner.State;
        Set(repairContent, owner.IsAllowed(DEF_Action.Repair));
        Set(ammoContent, owner.IsAllowed(DEF_Action.PhysicalAmmo));
        Set(waveContent, owner.IsAllowed(DEF_Action.StartWave) || owner.IsAllowed(DEF_Action.Combat));
        Set(advancedContent, owner.IsAllowed(DEF_Action.Barricade));
        Set(missileContent,owner.IsAllowed(DEF_Action.Missile));
        integrated = DEF_IntegratedXRPresentation.IsIntegratedScene(gameObject.scene);
        if (integrated && observed && observedSession == state.Session && observedAttempt == state.Attempt && state.Current > observedStage)
        {
            string done = CompletedAction(observedStage);
            if (done != null) { completedText = done; completedUntil = Time.unscaledTime + 2.2f; }
        }
        if (!observed || observedSession != state.Session || observedAttempt != state.Attempt) { completedUntil = 0; noticeUntil = 0; }
        observed = true; observedSession = state.Session; observedAttempt = state.Attempt; observedStage = state.Current;
        RefreshObjective();
        if (failureText != null) failureText.text = "";
        integrated = DEF_IntegratedXRPresentation.IsIntegratedScene(gameObject.scene);
        UpdateGoalVisibility();
        bool intro = state.IntroductionActive;
        introductionText.gameObject.SetActive(intro || (integrated && Time.unscaledTime < noticeUntil));
        if (!intro) { presenting = false; return; }
        if (presenting && session == state.Session && attempt == state.Attempt && introduction == state.Introduction) return;
        session = state.Session; attempt = state.Attempt; introduction = state.Introduction;
        introductionText.text = state.Current == S.RepairIntroduction ? "REPAIR TRAINING\nYour placed sentry has deliberate training damage. Release a crafted RepairKit inside its receiver to restore integrity." :
            state.Current == S.AmmoIntroduction ? "AMMO TRAINING\nBring a crafted Workshop Ammo Box to the Defense intake. One box adds up to10 rounds." :
            state.Current==S.FirstWaveComplete ? "BARRICADE AND SHOCK\nPlace a barricade across a moving enemy route and observe it walk the detour. Also stun a live enemy with Shock. Both are required before Missile unlocks." :
            state.Current==S.MissileIntroduction ? "MISSILE SENTRY\nDamage at least two distinct enemies with one explosion. Then sustain a Juggernaut wave using two defense types, a physical repair and refill." :
            "FIRST WAVE\nPreparation is complete. Start wave1 and protect the base.";
        if (integrated) noticeUntil = Time.unscaledTime + DEF_UIPresentationPolicy.ReadSeconds(introductionText.text);
        finishAt = Time.unscaledTime + Mathf.Max(.1f, introductionSeconds); presenting = true;
    }
    void Update()
    {
        UpdateGoalVisibility();
        if (owner != null && integrated)
        {
            RefreshObjective();
            bool inDefense = goalText == null || goalText.gameObject.activeSelf;
            if (!inDefense && Time.unscaledTime < noticeUntil) noticeUntil += Time.unscaledDeltaTime;
            if (introductionText != null) introductionText.gameObject.SetActive(inDefense && (presenting || Time.unscaledTime < noticeUntil));
        }
        if (failureText != null && !string.IsNullOrEmpty(failureText.text) && Time.unscaledTime >= failureUntil)
            failureText.text = "";
        if (!presenting || !Ready || (!integrated && !introductionText.gameObject.activeInHierarchy) || Time.unscaledTime < finishAt) return;
        presenting = false;
        owner.IntroductionFinished(this, session, attempt, introduction);
    }
    void UpdateGoalVisibility()
    {
        if (owner == null || goalText == null) return;
        bool visible = true;
        if (integrated)
        {
            if (defenseGrid == null)
                foreach (var grid in FindObjectsByType<DEF_GridManager>(FindObjectsSortMode.None))
                    if (grid.gameObject.scene == gameObject.scene) { defenseGrid = grid; break; }
            var input = DEF_VRInput.Instance;
            var camera = input != null && input.gameObject.scene == gameObject.scene && DEF_VRInput.Active && input.xrCamera != null ? input.xrCamera : Camera.main;
            // Local station guidance owns the view until the player returns to Defense.
            // Missing setup must not hide the only available guidance.
            if (defenseGrid != null && camera != null)
                visible = defenseGrid.WorldToTile(camera.transform.position, out _, out _);
        }
        if (goalText.gameObject.activeSelf != visible) goalText.gameObject.SetActive(visible);
    }
    void RefreshObjective()
    {
        if (goalText == null || owner == null) return;
        bool check = integrated && Time.unscaledTime < completedUntil;
        if (integrated) goalText.color = check ? new Color(.55f, 1f, .65f) : Color.white;
        goalText.text = check ? "✓ COMPLETED\n" + completedText :
            (integrated ? (owner.State.Current == S.Complete ? "DEFENSE COMPLETE\n" : "DEFENSE — NEXT\n") : "") + owner.NextGoal;
    }
    static string CompletedAction(S stage)
    {
        switch (stage)
        {
            case S.TeamPrerequisites: return "All three station requirements verified.";
            case S.PlaceSentry: return "Basic sentry placed.";
            case S.Repair: return "Physical RepairKit delivered.";
            case S.Ammo: return "Physical Ammo Box delivered.";
            case S.WaveActive: return "First wave cleared.";
            case S.AdvancedPractice: return "Barricade detour and Shock stun verified.";
            case S.MissilePractice: return "One explosion damaged two enemies.";
            case S.JuggernautActive: return "Juggernaut challenge completed.";
            default: return null; // Merely waiting through an introduction is not an accomplishment.
        }
    }
    public void ShowFailure(string message)
    {
        failureUntil = Time.unscaledTime + (integrated ? DEF_UIPresentationPolicy.ReadSeconds(message) : 8f);
        if (failureText != null) failureText.text = message;
    }
    static void Set(GameObject[] objects, bool active)
    { if (objects != null) foreach (var obj in objects) if (obj != null) obj.SetActive(active); }
}
