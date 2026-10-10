using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Marks a prefab as a grid building. Registers itself in PWR_GridManager so neighbors
    /// can find it, and unregisters when destroyed.
    /// </summary>
    public class PWR_GridBuilding : MonoBehaviour
    {
        [Tooltip("Type id used for foundation checks, e.g. \"ConcretePad\", \"Generator\".")]
        public string buildingId = "Building";

        [Tooltip("Other buildings may be placed on top of this one (rubric: Foundation Buildings).")]
        public bool isFoundation;

        [Tooltip("Vertical offset added for whatever is stacked on top of this building.")]
        public float stackHeight = 0f;

        [Tooltip("Snap and register on Start when the building is pre-placed in the scene.")]
        public bool registerOnStart = true;

        public Vector2Int Cell { get; private set; }
        public bool IsRegistered { get; private set; }

        private void Start()
        {
            if (registerOnStart && !IsRegistered) SnapAndRegister();
        }

        /// <summary>Snaps to the cell under the current position and registers there.</summary>
        public void SnapAndRegister()
        {
            var grid = PWR_GridManager.Instance;
            if (grid == null)
            {
                Debug.LogWarning("[PWR] No PWR_GridManager in scene.", this);
                return;
            }
            Vector2Int cell = grid.WorldToCell(transform.position);
            float height = grid.GetStackHeight(cell);
            transform.position = grid.CellToWorld(cell) + grid.transform.up * height;
            Register(cell);
        }

        public void Register(Vector2Int cell)
        {
            var grid = PWR_GridManager.Instance;
            if (grid == null) return;
            if (IsRegistered) grid.Unregister(this, Cell);
            Cell = cell;
            grid.Register(this, cell);
            IsRegistered = true;
        }

        private void OnDestroy()
        {
            if (IsRegistered && PWR_GridManager.Instance != null)
                PWR_GridManager.Instance.Unregister(this, Cell);
        }
    }
}
