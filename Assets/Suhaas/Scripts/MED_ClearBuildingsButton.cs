using UnityEngine;

// Test-only: removes all placed buildings and restocks the shelf. Remove with MED_TestTools.
public class MED_ClearBuildingsButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources)
    {
        var placer = FindAnyObjectByType<MED_BuildingPlacer>();
        if (placer != null) placer.ClearAll();
        else Debug.LogWarning("MED_ClearBuildingsButton: no MED_BuildingPlacer found in scene.", this);
    }
}
