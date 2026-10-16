using UnityEngine;

// Turns the object (a text label) to face the main camera. Yaw-only keeps labels upright.
public class MED_Billboard : MonoBehaviour
{
    [SerializeField] bool yawOnly = true;

    Camera cam;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        var away = transform.position - cam.transform.position;
        if (yawOnly) away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(away, Vector3.up);
    }
}
