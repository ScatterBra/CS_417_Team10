using UnityEngine;

// Distance check against the player's head (XZ plane only).
public static class MED_PlayerProximity
{
    public static bool IsNear(Vector3 target, float maxDistance)
    {
        var cam = Camera.main;
        if (cam == null) return false;
        var d = cam.transform.position - target;
        d.y = 0f;
        return d.magnitude <= maxDistance;
    }
}
