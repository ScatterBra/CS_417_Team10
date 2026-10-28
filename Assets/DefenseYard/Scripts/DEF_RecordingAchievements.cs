using UnityEngine;

// Presentation only: these objects observe authenticated progression outcomes.
// They cannot grant inventory, receipts, readiness, or mastery.
public sealed class DEF_RecordingAchievements : MonoBehaviour
{
    public DEF_Progression owner;
    public GameObject fieldMedic, supplyLine, waveDefender;
    int session = -1, attempt = -1;
    void Awake() { Clear(); }
    void Clear()
    {
        if (fieldMedic != null) fieldMedic.SetActive(false);
        if (supplyLine != null) supplyLine.SetActive(false);
        if (waveDefender != null) waveDefender.SetActive(false);
    }
    void Update()
    {
        if (owner == null || !owner.workshopRecording) { Clear(); return; }
        var state = owner.State;
        if (session != state.Session || attempt != state.Attempt)
        { Clear(); session = state.Session; attempt = state.Attempt; }
        // These milestones are reachable only through the existing accepted
        // positive repair/refill and live first-wave-clear transaction paths.
        if (fieldMedic != null && state.Current >= DEF_ProgressionState.Stage.AmmoIntroduction) fieldMedic.SetActive(true);
        if (supplyLine != null && state.Current >= DEF_ProgressionState.Stage.WaveIntroduction) supplyLine.SetActive(true);
        if (waveDefender != null && state.Current >= DEF_ProgressionState.Stage.FirstWaveComplete) waveDefender.SetActive(true);
    }
}
