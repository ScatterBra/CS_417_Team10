using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// A delivered package (copper wire or glass). It rides the belts cell by cell
    /// (rubric: Path Following / Built Paths), counting the distance in its PathProgress stat.
    /// Past the last belt it drops off; landing in the receiving bin destroys it and raises the
    /// bin's Stock (rubric: Collisions).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(PWR_StatBlock))]
    public class PWR_Cargo : MonoBehaviour
    {
        public PWR_ResourceType resource = PWR_ResourceType.CopperWire;
        public string pathStat = "PathProgress";
        public float halfHeight = 0.1f;

        public bool Delivered { get; set; }
        public bool Riding { get; private set; }

        private Rigidbody body;
        private PWR_StatBlock stats;
        private Vector3 lastDirection = Vector3.forward;
        private float lastSpeed = 0.8f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            stats = GetComponent<PWR_StatBlock>();
            if (TryGetComponent(out PWR_Grabbable grabbable))
            {
                grabbable.Picked += _ => StopRiding(false);
                // XRGrabInteractable restores the kinematic flag it saw on grab; make sure a dropped
                // package falls instead of hanging in the air.
                grabbable.Dropped += _ => StartCoroutine(MakeDynamicNextFrame());
            }
        }

        private System.Collections.IEnumerator MakeDynamicNextFrame()
        {
            yield return null;
            if (!Riding && body != null) body.isKinematic = false;
        }

        /// <summary>Called by the chute when it puts the package on the first belt.</summary>
        public void StartRiding()
        {
            Riding = true;
            body.isKinematic = true;
            // Interpolation would overwrite the transform we move by hand every frame.
            body.interpolation = RigidbodyInterpolation.None;
        }

        private void Update()
        {
            if (!Riding) return;
            var grid = PWR_GridManager.Instance;
            if (grid == null) return;

            var belt = grid.Find<PWR_ConveyorBelt>(grid.WorldToCell(transform.position));
            if (belt == null)
            {
                StopRiding(true);
                return;
            }

            float dt = Time.deltaTime;
            Vector3 dir = belt.Direction;
            Vector3 center = grid.CellToWorld(belt.Building.Cell);
            Vector3 pos = transform.position;
            Vector3 offset = pos - center;
            offset.y = 0f;
            Vector3 lateral = offset - Vector3.Dot(offset, dir) * dir;
            pos += dir * (belt.speed * dt) - lateral * Mathf.Min(1f, 6f * dt);
            pos.y = belt.SurfaceY + halfHeight;
            transform.position = pos;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * dt);

            stats.Add(pathStat, belt.speed * dt);
            lastDirection = dir;
            lastSpeed = belt.speed;
        }

        private void StopRiding(bool launch)
        {
            if (!Riding) return;
            Riding = false;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            if (launch) body.linearVelocity = lastDirection * lastSpeed;
        }
    }
}
