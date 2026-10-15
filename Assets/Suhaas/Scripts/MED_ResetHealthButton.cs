public class MED_ResetHealthButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources) => resources.ResetHealth();
}
