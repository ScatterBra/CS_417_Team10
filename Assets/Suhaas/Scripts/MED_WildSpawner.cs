using System.Collections.Generic;
using UnityEngine;

// Scatters wild herb patches and supply crates on random free grid cells when play starts.
public class MED_WildSpawner : MonoBehaviour
{
    [SerializeField] MED_GridManager grid;
    [SerializeField] Transform container;
    [SerializeField] GameObject herbPatchPrefab;
    [SerializeField] GameObject supplyCratePrefab;
    [SerializeField] int minHerbPatches = 5;
    [SerializeField] int maxHerbPatches = 8;
    [SerializeField] int minCrates = 3;
    [SerializeField] int maxCrates = 4;

    void Start()
    {
        // New seed every run.
        Random.InitState((int)(System.DateTime.Now.Ticks & 0x7FFFFFFF) ^ System.Guid.NewGuid().GetHashCode());
        Respawn();
    }

    /// <summary>Destroys all wild instances and generates a new random set.</summary>
    public void Respawn()
    {
        foreach (var h in container.GetComponentsInChildren<MED_Harvestable>())
        {
            grid.Free(h);
            Destroy(h.gameObject);
        }

        var free = new List<Vector2Int>();
        for (int x = 0; x < grid.GridSize.x; x++)
            for (int z = 0; z < grid.GridSize.y; z++)
            {
                var c = new Vector2Int(x, z);
                if (grid.IsCellFree(c)) free.Add(c);
            }

        Spawn(herbPatchPrefab, Random.Range(minHerbPatches, maxHerbPatches + 1), free);
        Spawn(supplyCratePrefab, Random.Range(minCrates, maxCrates + 1), free);
    }

    void Spawn(GameObject prefab, int count, List<Vector2Int> free)
    {
        for (int i = 0; i < count && free.Count > 0; i++)
        {
            int idx = Random.Range(0, free.Count);
            var cell = free[idx];
            free.RemoveAt(idx);

            var go = Instantiate(prefab, container);
            go.transform.SetPositionAndRotation(grid.CellToWorld(cell),
                grid.transform.rotation * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            var h = go.GetComponent<MED_Harvestable>();
            h.AssignGrid(grid);
            grid.TryOccupy(new[] { cell }, h);
        }
    }
}
