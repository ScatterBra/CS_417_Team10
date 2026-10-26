using System;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Tells the station whether the player is inside the Power Station area, so its lighting and
    /// controller inputs only apply there once it shares a scene with the other areas. The area is
    /// the build grid plus a margin (to cover the walls and doorway); a small extra margin on the way
    /// out stops it flickering at the edge. Added at runtime by PWR_LevelDirector.
    /// </summary>
    public class PWR_AreaGate : MonoBehaviour
    {
        [Tooltip("Metres around the build grid that still count as the Power Station.")]
        public float margin = 3f;
        [Tooltip("Extra metres the player must go past the edge before counting as outside.")]
        public float exitHysteresis = 1f;

        /// <summary>True while the player's head is in the Power Station area (true when there is no grid).</summary>
        public static bool PlayerInside { get; private set; } = true;
        public static event Action<bool> Changed;

        private void Awake() => PlayerInside = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            PlayerInside = true;
            Changed = null;
        }

        private void Update() => Evaluate();

        /// <summary>Re-checks the player's position now (PWR_LevelDirector calls it before its first lighting update).</summary>
        public void Evaluate()
        {
            var grid = PWR_GridManager.Instance;
            var cam = Camera.main;
            if (grid == null || cam == null) return;

            float edge = margin + (PlayerInside ? exitHysteresis : 0f);
            Vector3 local = grid.transform.InverseTransformPoint(cam.transform.position);
            float width = grid.gridSize.x * grid.cellSize;
            float depth = grid.gridSize.y * grid.cellSize;
            bool inside = local.x >= -edge && local.x <= width + edge && local.z >= -edge && local.z <= depth + edge;

            if (inside == PlayerInside) return;
            PlayerInside = inside;
            Changed?.Invoke(inside);
        }
    }
}
