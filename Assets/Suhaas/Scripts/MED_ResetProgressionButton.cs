// Test-only: back to the start state (thresholds, unlocks, placed buildings, health). Remove with MED_TestTools.
public class MED_ResetProgressionButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources)
    {
        var m = MED_ProgressionManager.Instance;
        if (m != null) m.ResetProgression();
    }
}
