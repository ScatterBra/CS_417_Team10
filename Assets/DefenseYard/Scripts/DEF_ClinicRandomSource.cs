using UnityEngine;
// Provenance stamped by the actual spawning owner.
public sealed class DEF_ClinicRandomSource : MonoBehaviour
{
    public MED_WildSpawner Owner {get; private set;}
    public void Bind(MED_WildSpawner owner)
    {
        if(Owner!=null&&Owner!=owner)throw new System.InvalidOperationException("Random source already owned");
        Owner=owner;
    }
}
