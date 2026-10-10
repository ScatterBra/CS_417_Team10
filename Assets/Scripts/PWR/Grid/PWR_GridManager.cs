using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Regular build grid on this transform's local XZ plane (rubric: Gridded Buildings).
    /// Each cell holds a stack of buildings so a building can sit on a foundation
    /// (rubric: Foundation Buildings). Cell (0,0) starts at this transform's position.
    /// </summary>
    public class PWR_GridManager : MonoBehaviour
    {
        public static PWR_GridManager Instance { get; private set; }

        public float cellSize = 1f;
        public Vector2Int gridSize = new Vector2Int(8, 8);

        private static readonly Vector2Int[] NeighborOffsets =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        };

        private readonly Dictionary<Vector2Int, List<PWR_GridBuilding>> cells =
            new Dictionary<Vector2Int, List<PWR_GridBuilding>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- coordinates ----------

        public Vector2Int WorldToCell(Vector3 worldPos)
        {
            Vector3 local = transform.InverseTransformPoint(worldPos);
            return new Vector2Int(Mathf.FloorToInt(local.x / cellSize), Mathf.FloorToInt(local.z / cellSize));
        }

        /// <summary>World position of the cell's center, on the grid plane.</summary>
        public Vector3 CellToWorld(Vector2Int cell)
        {
            var local = new Vector3((cell.x + 0.5f) * cellSize, 0f, (cell.y + 0.5f) * cellSize);
            return transform.TransformPoint(local);
        }

        public Vector3 Snap(Vector3 worldPos) => CellToWorld(WorldToCell(worldPos));

        public bool InBounds(Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < gridSize.x && cell.y < gridSize.y;

        // ---------- occupancy ----------

        public IReadOnlyList<PWR_GridBuilding> GetStack(Vector2Int cell) =>
            cells.TryGetValue(cell, out var list) ? list : (IReadOnlyList<PWR_GridBuilding>)System.Array.Empty<PWR_GridBuilding>();

        public PWR_GridBuilding GetTop(Vector2Int cell)
        {
            var stack = GetStack(cell);
            return stack.Count > 0 ? stack[stack.Count - 1] : null;
        }

        public bool IsEmpty(Vector2Int cell) => GetStack(cell).Count == 0;

        /// <summary>Height at which the next building in this cell should be placed.</summary>
        public float GetStackHeight(Vector2Int cell)
        {
            float h = 0f;
            foreach (var b in GetStack(cell)) h += b.stackHeight;
            return h;
        }

        public void Register(PWR_GridBuilding building, Vector2Int cell)
        {
            if (!cells.TryGetValue(cell, out var list))
            {
                list = new List<PWR_GridBuilding>();
                cells[cell] = list;
            }
            if (!list.Contains(building)) list.Add(building);
        }

        public void Unregister(PWR_GridBuilding building, Vector2Int cell)
        {
            if (!cells.TryGetValue(cell, out var list)) return;
            list.Remove(building);
            if (list.Count == 0) cells.Remove(cell);
        }

        /// <summary>All buildings in the 4 orthogonally adjacent cells.</summary>
        public void GetNeighbors(Vector2Int cell, List<PWR_GridBuilding> results)
        {
            results.Clear();
            foreach (var off in NeighborOffsets)
                if (cells.TryGetValue(cell + off, out var list))
                    results.AddRange(list);
        }

        /// <summary>Neighboring components of type T (e.g. PWR_PowerNode).</summary>
        public void GetNeighbors<T>(Vector2Int cell, List<T> results) where T : Component
        {
            results.Clear();
            foreach (var off in NeighborOffsets)
            {
                if (!cells.TryGetValue(cell + off, out var list)) continue;
                foreach (var b in list)
                    if (b && b.TryGetComponent(out T comp)) results.Add(comp);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.5f);
            Gizmos.matrix = transform.localToWorldMatrix;
            for (int x = 0; x <= gridSize.x; x++)
                Gizmos.DrawLine(new Vector3(x * cellSize, 0, 0), new Vector3(x * cellSize, 0, gridSize.y * cellSize));
            for (int z = 0; z <= gridSize.y; z++)
                Gizmos.DrawLine(new Vector3(0, 0, z * cellSize), new Vector3(gridSize.x * cellSize, 0, z * cellSize));
        }
    }
}
