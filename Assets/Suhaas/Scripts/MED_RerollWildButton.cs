using UnityEngine;

// Test-only: re-rolls the wild herb patches and crates. Remove with MED_TestTools.
public class MED_RerollWildButton : MED_TestButtonBase
{
    protected override void OnPressed(MED_PlayerResources resources)
    {
        var spawner = FindAnyObjectByType<MED_WildSpawner>();
        if (spawner != null) spawner.Respawn();
        else Debug.LogWarning("MED_RerollWildButton: no MED_WildSpawner found in scene.", this);
    }
}
