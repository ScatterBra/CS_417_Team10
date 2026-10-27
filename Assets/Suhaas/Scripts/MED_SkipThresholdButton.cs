// Test-only: clears the next threshold as if the player had done it (reveal card and unlock follow). Remove with MED_TestTools.
public class MED_SkipThresholdButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources)
    {
        var m = MED_ProgressionManager.Instance;
        if (m != null) m.SkipToNext();
    }
}
