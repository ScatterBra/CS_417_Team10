public class MED_DamageTestButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources) => resources.Add(MED_ResourceType.Health, -10);
}
