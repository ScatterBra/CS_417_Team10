using System.Collections.Generic;
using UnityEngine;

// Square building grid aligned to the room. Cell (0,0) is the min-X/min-Z corner. The grid is defined in
// this object's local space, so moving/rotating the station prefab moves the grid with it.
public class MED_GridManager : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    [Header("Grid")]
    [SerializeField] float cellSize = 1f;
    [SerializeField] Vector2Int gridSize = new Vector2Int(15, 15);

    [Header("Reserved cells")]
    [SerializeField] int wallMargin = 1;
    [SerializeField] Transform doorMarker;
    [SerializeField] int doorCorridorWidth = 2;
    [SerializeField] int doorCorridorDepth = 3;
    [SerializeField] Transform spawnMarker;
    [SerializeField] Vector2Int spawnReserveSize = new Vector2Int(2, 2);
    [Tooltip("Cells under the colliders of these objects (shelf, test tools, ...) are blocked.")]
    [SerializeField] List<Transform> reservedAreas = new List<Transform>();
    [SerializeField] float reservedAreaPadding = 0.1f;
    [SerializeField] List<Vector2Int> extraReservedCells = new List<Vector2Int>();
    [SerializeField] bool drawGizmos = true;

    [Header("Visual")]
    [SerializeField] Renderer lineOverlay;
    [SerializeField] Renderer blockedOverlay;
    [SerializeField] float fadeSpeed = 5f;
    [SerializeField, Range(0f, 1f)] float lineAlpha = 0.6f;
    [SerializeField, Range(0f, 1f)] float blockedAlpha = 0.28f;

    readonly HashSet<Vector2Int> reserved = new HashSet<Vector2Int>();
    readonly Dictionary<Vector2Int, object> occupied = new Dictionary<Vector2Int, object>();
    MaterialPropertyBlock lineBlock, blockedBlock;
    Texture2D blockedTexture;
    float alpha, targetAlpha;

    public float CellSize => cellSize;
    public Vector2Int GridSize => gridSize;
    public IReadOnlyCollection<Vector2Int> ReservedCells => reserved;

    /// <summary>World position of the min corner of cell (0,0).</summary>
    public Vector3 Origin => transform.TransformPoint(new Vector3(-gridSize.x * cellSize * 0.5f, 0f, -gridSize.y * cellSize * 0.5f));

    void Awake()
    {
        RebuildReserved();
        BuildBlockedTexture();
        lineBlock = new MaterialPropertyBlock();
        blockedBlock = new MaterialPropertyBlock();
        ApplyFade();
    }

    // ---------- Coordinates ----------

    public Vector2Int WorldToCell(Vector3 world)
    {
        var l = transform.InverseTransformPoint(world);
        return new Vector2Int(
            Mathf.FloorToInt(l.x / cellSize + gridSize.x * 0.5f),
            Mathf.FloorToInt(l.z / cellSize + gridSize.y * 0.5f));
    }

    /// <summary>World position of the cell center, on the grid floor plane.</summary>
    public Vector3 CellToWorld(Vector2Int c)
    {
        return transform.TransformPoint(new Vector3(
            (c.x + 0.5f - gridSize.x * 0.5f) * cellSize, 0f,
            (c.y + 0.5f - gridSize.y * 0.5f) * cellSize));
    }

    public bool IsInBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < gridSize.x && c.y < gridSize.y;
    public bool IsReserved(Vector2Int c) => reserved.Contains(c);
    public bool IsCellFree(Vector2Int c) => IsInBounds(c) && !reserved.Contains(c) && !occupied.ContainsKey(c);

    // ---------- Footprints ----------

    /// <summary>
    /// Cells covered by a building of the given footprint (local X width, Z depth) rotated by quarterTurns 90-degree
    /// steps. The anchor is the cell under the cursor; for even sizes the extra cell is on the + side.
    /// </summary>
    public List<Vector2Int> GetFootprintCells(Vector2Int anchor, Vector2Int footprint, int quarterTurns)
    {
        int w = Mathf.Max(1, footprint.x), d = Mathf.Max(1, footprint.y);
        if ((((quarterTurns % 2) + 2) % 2) == 1) { int t = w; w = d; d = t; }
        int minX = anchor.x - (w - 1) / 2;
        int minZ = anchor.y - (d - 1) / 2;
        var cells = new List<Vector2Int>(w * d);
        for (int x = 0; x < w; x++)
            for (int z = 0; z < d; z++)
                cells.Add(new Vector2Int(minX + x, minZ + z));
        return cells;
    }

    /// <summary>World center of a set of cells (exactly a cell center or a cell-edge midpoint).</summary>
    public Vector3 GetFootprintCenter(IReadOnlyList<Vector2Int> cells)
    {
        int minX = int.MaxValue, minZ = int.MaxValue, maxX = int.MinValue, maxZ = int.MinValue;
        foreach (var c in cells)
        {
            minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
            minZ = Mathf.Min(minZ, c.y); maxZ = Mathf.Max(maxZ, c.y);
        }
        return transform.TransformPoint(new Vector3(
            ((minX + maxX + 1) * 0.5f - gridSize.x * 0.5f) * cellSize, 0f,
            ((minZ + maxZ + 1) * 0.5f - gridSize.y * 0.5f) * cellSize));
    }

    // ---------- Occupancy ----------

    public bool TryOccupy(IEnumerable<Vector2Int> cells, object owner)
    {
        foreach (var c in cells)
            if (!IsCellFree(c)) return false;
        foreach (var c in cells)
            occupied[c] = owner;
        return true;
    }

    public void Free(object owner)
    {
        var toRemove = new List<Vector2Int>();
        foreach (var kv in occupied)
            if (ReferenceEquals(kv.Value, owner)) toRemove.Add(kv.Key);
        foreach (var c in toRemove) occupied.Remove(c);
    }

    public void FreeAll() => occupied.Clear();

    // ---------- Reserved cells ----------

    [ContextMenu("Rebuild Reserved Cells")]
    public void RebuildReserved()
    {
        reserved.Clear();

        for (int x = 0; x < gridSize.x; x++)
            for (int z = 0; z < gridSize.y; z++)
                if (x < wallMargin || z < wallMargin || x >= gridSize.x - wallMargin || z >= gridSize.y - wallMargin)
                    reserved.Add(new Vector2Int(x, z));

        if (doorMarker != null)
        {
            var m = transform.InverseTransformPoint(doorMarker.position);
            for (int x = 0; x < gridSize.x; x++)
                for (int z = 0; z < gridSize.y; z++)
                {
                    var c = CellLocalCenter(new Vector2Int(x, z));
                    float dz = m.z - c.z;
                    if (Mathf.Abs(c.x - m.x) < doorCorridorWidth * cellSize * 0.5f && dz >= 0f && dz <= doorCorridorDepth * cellSize)
                        reserved.Add(new Vector2Int(x, z));
                }
        }

        if (spawnMarker != null)
        {
            var c = WorldToCell(spawnMarker.position);
            int minX = c.x - (spawnReserveSize.x - 1) / 2, minZ = c.y - (spawnReserveSize.y - 1) / 2;
            for (int x = 0; x < spawnReserveSize.x; x++)
                for (int z = 0; z < spawnReserveSize.y; z++)
                    reserved.Add(new Vector2Int(minX + x, minZ + z));
        }

        foreach (var area in reservedAreas)
            if (area != null) ReserveBounds(area);

        foreach (var c in extraReservedCells) reserved.Add(c);

        reserved.RemoveWhere(c => !IsInBounds(c));
    }

    Vector3 CellLocalCenter(Vector2Int c) =>
        new Vector3((c.x + 0.5f - gridSize.x * 0.5f) * cellSize, 0f, (c.y + 0.5f - gridSize.y * 0.5f) * cellSize);

    void ReserveBounds(Transform area)
    {
        bool any = false;
        var b = new Bounds();
        foreach (var col in area.GetComponentsInChildren<Collider>())
        {
            if (!any) { b = col.bounds; any = true; } else b.Encapsulate(col.bounds);
        }
        if (!any) return;

        float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            var l = transform.InverseTransformPoint(corner);
            minX = Mathf.Min(minX, l.x); maxX = Mathf.Max(maxX, l.x);
            minZ = Mathf.Min(minZ, l.z); maxZ = Mathf.Max(maxZ, l.z);
        }
        int x0 = Mathf.FloorToInt((minX - reservedAreaPadding) / cellSize + gridSize.x * 0.5f);
        int x1 = Mathf.FloorToInt((maxX + reservedAreaPadding) / cellSize + gridSize.x * 0.5f);
        int z0 = Mathf.FloorToInt((minZ - reservedAreaPadding) / cellSize + gridSize.y * 0.5f);
        int z1 = Mathf.FloorToInt((maxZ + reservedAreaPadding) / cellSize + gridSize.y * 0.5f);
        for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
                reserved.Add(new Vector2Int(x, z));
    }

    // ---------- Visual ----------

    public void SetVisible(bool visible) => targetAlpha = visible ? 1f : 0f;

    void BuildBlockedTexture()
    {
        blockedTexture = new Texture2D(gridSize.x, gridSize.y, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "MED_GridBlockedRuntime"
        };
        var px = new Color32[gridSize.x * gridSize.y];
        for (int z = 0; z < gridSize.y; z++)
            for (int x = 0; x < gridSize.x; x++)
                px[z * gridSize.x + x] = reserved.Contains(new Vector2Int(x, z)) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
        blockedTexture.SetPixels32(px);
        blockedTexture.Apply(false);
    }

    void Update()
    {
        if (!Mathf.Approximately(alpha, targetAlpha))
        {
            alpha = Mathf.MoveTowards(alpha, targetAlpha, fadeSpeed * Time.deltaTime);
            ApplyFade();
        }
    }

    void ApplyFade()
    {
        bool on = alpha > 0.001f;
        if (lineOverlay != null)
        {
            lineOverlay.enabled = on;
            lineOverlay.GetPropertyBlock(lineBlock);
            lineBlock.SetColor(BaseColorId, new Color(0.8f, 1f, 1f, alpha * lineAlpha));
            lineOverlay.SetPropertyBlock(lineBlock);
        }
        if (blockedOverlay != null)
        {
            blockedOverlay.enabled = on;
            blockedOverlay.GetPropertyBlock(blockedBlock);
            blockedBlock.SetColor(BaseColorId, new Color(1f, 0.15f, 0.15f, alpha * blockedAlpha));
            if (blockedTexture != null) blockedBlock.SetTexture(BaseMapId, blockedTexture);
            blockedOverlay.SetPropertyBlock(blockedBlock);
        }
    }

    void OnDestroy()
    {
        if (blockedTexture != null) Destroy(blockedTexture);
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        if (!Application.isPlaying) RebuildReserved();
        var old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        foreach (var c in reserved)
            Gizmos.DrawCube(CellLocalCenter(c) + Vector3.up * 0.02f, new Vector3(cellSize * 0.95f, 0.02f, cellSize * 0.95f));
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.6f);
        for (int x = 0; x <= gridSize.x; x++)
            Gizmos.DrawLine(new Vector3((x - gridSize.x * 0.5f) * cellSize, 0.02f, -gridSize.y * cellSize * 0.5f),
                            new Vector3((x - gridSize.x * 0.5f) * cellSize, 0.02f, gridSize.y * cellSize * 0.5f));
        for (int z = 0; z <= gridSize.y; z++)
            Gizmos.DrawLine(new Vector3(-gridSize.x * cellSize * 0.5f, 0.02f, (z - gridSize.y * 0.5f) * cellSize),
                            new Vector3(gridSize.x * cellSize * 0.5f, 0.02f, (z - gridSize.y * 0.5f) * cellSize));
        Gizmos.matrix = old;
    }
}
