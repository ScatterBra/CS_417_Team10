using UnityEngine;

// Constrain the simulator's tracked viewpoint to the walkable bunker interior.
// Input and tracked poses still come from the official XR Interaction Simulator.
[DefaultExecutionOrder(10000)]
public class DEF_PCXRPlayer : MonoBehaviour
{
    public Transform head;
    public Vector3 baseCenter = new Vector3(1f, 0f, 1f);
    public float interiorHalfSize = 2.9f;
    public float eyeHeight = 1.85f;
    void OnEnable() { Application.onBeforeRender += Constrain; }
    void OnDisable() { Application.onBeforeRender -= Constrain; }
    void LateUpdate() { Constrain(); }
    void Constrain()
    {
        if (!Application.isEditor || !DEF_VRInput.Active || head == null) return;
        var p = head.position;
        var target = new Vector3(Mathf.Clamp(p.x, baseCenter.x - interiorHalfSize, baseCenter.x + interiorHalfSize),
            baseCenter.y + eyeHeight,
            Mathf.Clamp(p.z, baseCenter.z - interiorHalfSize, baseCenter.z + interiorHalfSize));
        transform.position += target - p;
    }
}
