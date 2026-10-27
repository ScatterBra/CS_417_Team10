using System.Collections.Generic;
using UnityEngine;

// Semi-random patch stamping (Procedural Generation). On Play (new seed) and on Respawn() it picks 2-3 hand-designed
// MED_WildPatch layouts from the library, rotates each by 0/90/180/270 and stamps it onto a random spot of the grid.
// A patch is only stamped where ALL of its cells are free (not reserved: wall margin, doorway, spawn, shelf, delivery
// route, test tools; not built on) and clear of the patches stamped before it. It tries up to attemptsPerPatch
// positions and is skipped if none fits. The spawned herb patches and crates are ordinary MED_Harvestable objects, so
// they keep their Death rules and release their grid cells when depleted.
public class MED_WildSpawner : MonoBehaviour
{
    [SerializeField] MED_GridManager grid;
    [SerializeField] Transform container;
    [SerializeField] GameObject herbPatchPrefab;
    [SerializeField] GameObject supplyCratePrefab;
    [Header("Patch stamping")]
    [SerializeField] MED_WildPatch[] patches;
    [SerializeField] int minPatches = 2;
    [SerializeField] int maxPatches = 3;
    [Tooltip("Random positions tried for each patch before it is skipped.")]
    [SerializeField] int attemptsPerPatch = 100;
    [SerializeField] bool logStamps = true;
    [Header("Editor preview (gizmo, select this object)")]
    [SerializeField] MED_WildPatch previewPatch;
    [SerializeField] Vector2Int previewCell = new Vector2Int(5, 5);
    [SerializeField, Range(0, 3)] int previewQuarterTurns;

    struct Stamp
    {
        public string name;
        public Vector2Int min;
        public Vector2Int size;
        public int rotation;
        public List<MED_WildPatch.StampCell> cells;
    }

    readonly List<Stamp> lastStamps = new List<Stamp>();

    /// <summary>The patches stamped by the latest Respawn (read-only, for tests and the gizmo).</summary>
    public int LastStampCount => lastStamps.Count;

    void Start()
    {
        // New seed every run.
        Random.InitState((int)(System.DateTime.Now.Ticks & 0x7FFFFFFF) ^ System.Guid.NewGuid().GetHashCode());
        Respawn();
    }

    /// <summary>Destroys all wild instances and stamps a new random set of patches.</summary>
    public void Respawn()
    {
        foreach (var h in container.GetComponentsInChildren<MED_Harvestable>())
        {
            grid.Free(h);
            Destroy(h.gameObject);
        }
        lastStamps.Clear();
        if (patches == null || patches.Length == 0)
        {
            if (DEF_ClinicTraining.For(this) != null) EnsureLearningSource();
            else Debug.LogWarning("MED_WildSpawner: no patches assigned, nothing to stamp.", this);
            return;
        }

        int want = Mathf.Clamp(Random.Range(minPatches, maxPatches + 1), 0, patches.Length);
        var order = Shuffled(patches);
        var taken = new HashSet<Vector2Int>();   // every cell (empty ones too) claimed by a stamp this round

        for (int i = 0; i < want; i++)
        {
            var patch = order[i];
            if (patch == null) continue;
            if (!TryStamp(patch, taken)) continue;
        }
    }

    bool TryStamp(MED_WildPatch patch, HashSet<Vector2Int> taken)
    {
        for (int attempt = 0; attempt < attemptsPerPatch; attempt++)
        {
            int rot = Random.Range(0, 4);
            var cells = patch.GetCells(rot, out var size);
            var min = new Vector2Int(Random.Range(0, grid.GridSize.x - size.x + 1), Random.Range(0, grid.GridSize.y - size.y + 1));
            if (!Fits(cells, min, taken)) continue;

            foreach (var c in cells)
            {
                var cell = min + c.offset;
                taken.Add(cell);
                if (c.kind == MED_PatchCell.Herb) Place(herbPatchPrefab, cell);
                else if (c.kind == MED_PatchCell.Crate) Place(supplyCratePrefab, cell);
            }
            string name = string.IsNullOrEmpty(patch.displayName) ? patch.name : patch.displayName;
            lastStamps.Add(new Stamp { name = name, min = min, size = size, rotation = rot * 90, cells = cells });
            if (logStamps) Debug.Log($"[MED_WildSpawner] Stamped {name} at ({min.x},{min.y}) rot {rot * 90}", this);
            return true;
        }
        string skipped = string.IsNullOrEmpty(patch.displayName) ? patch.name : patch.displayName;
        if (logStamps) Debug.Log($"[MED_WildSpawner] Skipped {skipped}: no free spot after {attemptsPerPatch} tries", this);
        return false;
    }

    bool Fits(List<MED_WildPatch.StampCell> cells, Vector2Int min, HashSet<Vector2Int> taken)
    {
        foreach (var c in cells)
        {
            var cell = min + c.offset;
            if (!grid.IsCellFree(cell) || taken.Contains(cell)) return false;
        }
        return true;
    }

    void Place(GameObject prefab, Vector2Int cell)
    {
        var go = Instantiate(prefab, container);
        go.transform.SetPositionAndRotation(grid.CellToWorld(cell),
            grid.transform.rotation * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        var h = go.GetComponent<MED_Harvestable>();
        if (DEF_ClinicTraining.For(this) != null) { var marker = go.GetComponent<DEF_ClinicRandomSource>(); if (marker == null) marker = go.AddComponent<DEF_ClinicRandomSource>(); marker.Bind(this); }
        h.AssignGrid(grid);
        grid.TryOccupy(new[] { cell }, h);
    }

    static T[] Shuffled<T>(T[] source)
    {
        var a = (T[])source.Clone();
        for (int i = a.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (a[i], a[j]) = (a[j], a[i]);
        }
        return a;
    }

    // ---------- Gizmos ----------

    void OnDrawGizmosSelected()
    {
        if (grid == null) return;
        if (Application.isPlaying)
        {
            foreach (var s in lastStamps) DrawStamp(s.cells, s.min, s.name);
        }
        else if (previewPatch != null)
        {
            var cells = previewPatch.GetCells(previewQuarterTurns, out _);
            DrawStamp(cells, previewCell, previewPatch.displayName);
        }
    }

    void DrawStamp(List<MED_WildPatch.StampCell> cells, Vector2Int min, string label)
    {
        float s = grid.CellSize;
        foreach (var c in cells)
        {
            var center = grid.CellToWorld(min + c.offset) + Vector3.up * 0.02f;
            Gizmos.color = c.kind == MED_PatchCell.Herb ? new Color(0.2f, 0.9f, 0.3f, 0.6f)
                         : c.kind == MED_PatchCell.Crate ? new Color(0.9f, 0.6f, 0.2f, 0.6f)
                         : new Color(1f, 1f, 1f, 0.12f);
            Gizmos.DrawCube(center, new Vector3(s * 0.9f, 0.02f, s * 0.9f));
        }
#if UNITY_EDITOR
        if (cells.Count > 0) UnityEditor.Handles.Label(grid.CellToWorld(min) + Vector3.up * 0.6f, label);
#endif
    }
    public bool EnsureLearningSource()
    {
        if(grid==null||container==null||herbPatchPrefab==null)return false;
        foreach(var existing in container.GetComponentsInChildren<MED_Harvestable>())
            if(existing.isActiveAndEnabled&&!existing.IsEmpty&&existing.GetComponent<DEF_ClinicRandomSource>()!=null&&existing.GetComponent<DEF_ClinicRandomSource>().Owner==this)return true;
        var free=new List<Vector2Int>();
        for(int x=0;x<grid.GridSize.x;x++)for(int z=0;z<grid.GridSize.y;z++)
            if(grid.IsCellFree(new Vector2Int(x,z)))free.Add(new Vector2Int(x,z));
        if(free.Count==0)return false;
        Spawn(herbPatchPrefab,1,free);
        foreach(var existing in container.GetComponentsInChildren<MED_Harvestable>())
            if(existing.isActiveAndEnabled&&!existing.IsEmpty&&existing.GetComponent<DEF_ClinicRandomSource>()!=null&&existing.GetComponent<DEF_ClinicRandomSource>().Owner==this)return true;
        return false;
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

            if (DEF_ClinicTraining.For(this) != null) { var marker = go.GetComponent<DEF_ClinicRandomSource>(); if (marker == null) marker = go.AddComponent<DEF_ClinicRandomSource>(); marker.Bind(this); }
        h.AssignGrid(grid);
            grid.TryOccupy(new[] { cell }, h);
        }
    }
}