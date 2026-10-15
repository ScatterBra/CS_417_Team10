// Test-only: lets every pop-up tutorial appear again. Remove with MED_TestTools.
public class MED_ResetTutorialsButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources) => MED_TutorialManager.ReArmAll();
}
