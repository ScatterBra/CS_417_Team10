using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// A player-built conveyor segment (rubric: Built Paths). It must sit on a concrete pad
    /// (Foundation) and carries cargo along its local +Z. The arrow strip on top uses the
    /// PWR/UVScroll material so the arrows visibly travel in the belt's direction.
    /// </summary>
    [RequireComponent(typeof(PWR_GridBuilding))]
    public class PWR_ConveyorBelt : MonoBehaviour
    {
        [Tooltip("Height of the belt surface above this object's pivot.")]
        public float surfaceHeight = 0.1f;
        public float speed = 0.8f;

        public PWR_GridBuilding Building { get; private set; }
        public Vector3 Direction => transform.forward;
        public float SurfaceY => transform.position.y + surfaceHeight;

        private void Awake() => Building = GetComponent<PWR_GridBuilding>();
    }
}
