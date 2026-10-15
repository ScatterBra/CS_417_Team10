// Test-only: adds 4 each of Herbs, Water, Alcohol and Gauze. Remove with MED_TestTools.
public class MED_AddSuppliesButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources)
    {
        resources.Add(MED_ResourceType.Herbs, 4);
        resources.Add(MED_ResourceType.Water, 4);
        resources.Add(MED_ResourceType.Alcohol, 4);
        resources.Add(MED_ResourceType.Gauze, 4);
    }
}
