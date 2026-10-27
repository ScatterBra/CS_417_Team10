// Test-only: removes every trophy and clears the achievement counters. Remove with MED_TestTools.
public class MED_ResetAchievementsButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources)
    {
        var m = MED_AchievementManager.Instance;
        if (m != null) m.ResetAchievements();
    }
}
