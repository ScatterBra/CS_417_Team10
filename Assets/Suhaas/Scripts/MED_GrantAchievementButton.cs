// Test-only: earns the next achievement that is not yet earned (its trophy appears). Remove with MED_TestTools.
public class MED_GrantAchievementButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources)
    {
        var m = MED_AchievementManager.Instance;
        if (m != null) m.GrantNext();
    }
}
