// Test-only: adds 5 Medicine. Remove with MED_TestTools.
public class MED_AddMedicineButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources) => resources.Add(MED_ResourceType.Medicine, 5);
}
