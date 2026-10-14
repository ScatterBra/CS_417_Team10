using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// The wrench tool. Crates break when it strikes them; pulling the trigger while holding
    /// it also strikes the nearest crate within reach (helps when a swing is too gentle).
    /// </summary>
    public class PWR_Wrench : MonoBehaviour
    {
        public Transform head;
        public float reach = 0.6f;

        private void Awake()
        {
            if (TryGetComponent(out PWR_Interactable interactable)) interactable.Used += _ => StrikeNearest();
        }

        public bool StrikeNearest()
        {
            Vector3 origin = head != null ? head.position : transform.position;
            PWR_Shatterable best = null;
            float bestDist = float.MaxValue;
            foreach (var col in Physics.OverlapSphere(origin, reach, ~0, QueryTriggerInteraction.Ignore))
            {
                var target = col.GetComponentInParent<PWR_Shatterable>();
                if (target == null) continue;
                float d = (col.ClosestPoint(origin) - origin).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = target; }
            }
            if (best == null) return false;
            best.Hit();
            return true;
        }
    }
}
