using UnityEngine;

// Test-only: refills every non-destroyable harvestable (the water barrel) to its max.
public class MED_RefillBarrelButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources)
    {
        foreach (var h in FindObjectsByType<MED_Harvestable>())
            if (!h.DestroyOnEmpty) h.Refill();
    }
}
