using UnityEngine;

// Test-only: ripens every herb planter in the scene. Remove with MED_TestTools.
public class MED_RipenPlantersButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources)
    {
        foreach (var p in FindObjectsByType<MED_HerbPlanter>())
            p.Ripen();
    }
}
