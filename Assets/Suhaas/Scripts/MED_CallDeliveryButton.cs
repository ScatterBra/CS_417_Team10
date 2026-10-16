// Test-only: calls a supply-cart delivery now (the cart still has to drive the whole path). Remove with MED_TestTools.
public class MED_CallDeliveryButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources)
    {
        var spawner = FindAnyObjectByType<MED_DeliverySpawner>();
        if (spawner != null) spawner.SpawnNow();
    }
}
