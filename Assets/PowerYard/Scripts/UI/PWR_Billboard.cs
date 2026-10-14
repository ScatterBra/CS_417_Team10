using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>Keeps world-space UI facing the player's camera.</summary>
    public class PWR_Billboard : MonoBehaviour
    {
        public bool lockVertical = true;

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 dir = transform.position - cam.transform.position;
            if (lockVertical) dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
