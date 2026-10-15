using UnityEngine;
using System.Collections.Generic;

public class DEF_Pathfinder : MonoBehaviour
{
    public static DEF_Pathfinder I;

    DEF_GridManager grid;
    bool[,] blocked;
    Dictionary<Vector2Int, GameObject> occupants = new Dictionary<Vector2Int, GameObject>();
    bool ready = false;

    public bool Ready { get { return ready; } }

    void Awake()
    {
        I = this;
        grid = FindFirstObjectByType<DEF_GridManager>();
        if (grid == null) return;
        blocked = new bool[grid.width, grid.depth];
        ready = true;
    }

    bool InBounds(int x, int z)
    {
        return x >= 0 && z >= 0 && x < grid.width && z < grid.depth;
    }

    public void SetBlocked(int x, int z, bool b)
    {
        if (!ready || !InBounds(x, z)) return;
        blocked[x, z] = b;
    }

    public bool IsBlocked(int x, int z)
    {
        if (!ready || !InBounds(x, z)) return true;
        return blocked[x, z];
    }

    public void Register(Vector2Int cell, GameObject go)
    {
        occupants[cell] = go;
    }

    public void Unregister(Vector2Int cell)
    {
        occupants.Remove(cell);
    }

    public GameObject OccupantAt(int x, int z)
    {
        GameObject go;
        occupants.TryGetValue(new Vector2Int(x, z), out go);
        return go;
    }

    public void RepathAll()
    {
        for (int i = DEF_Zombie.all.Count - 1; i >= 0; i--)
        {
            var z = DEF_Zombie.all[i];
            if (z != null) z.Repath();
        }
    }

    public Vector2Int BaseCell()
    {
        return new Vector2Int(grid.width / 2, grid.depth / 2);
    }

    public Vector3 CellCenter(Vector2Int cell)
    {
        return grid.TileCenter(cell.x, cell.y);
    }

    public bool SegmentBlocked(Vector3 a, Vector3 b)
    {
        if (!ready) return false;
        float dist = Vector3.Distance(a, b);
        int steps = Mathf.Max(1, Mathf.CeilToInt(dist / 0.5f));
        int ax, az, bx, bz;
        bool aOk = grid.WorldToTile(a, out ax, out az);
        bool bOk = grid.WorldToTile(b, out bx, out bz);
        for (int i = 1; i < steps; i++)
        {
            Vector3 p = a + (b - a) * ((float)i / steps);
            int x, z;
            if (!grid.WorldToTile(p, out x, out z)) continue;
            if (aOk && x == ax && z == az) continue;
            if (bOk && x == bx && z == bz) continue;
            if (blocked[x, z]) return true;
        }
        return false;
    }

    // Test the whole corridor, including both ends, against expanded occupied cells.
    // This keeps the grid for planning/building without making walkers visit every cell.
    public bool CorridorClear(Vector3 from, Vector3 to, float radius)
    {
        if (!ready) return false;
        Vector3 a = grid.transform.InverseTransformPoint(from);
        Vector3 b = grid.transform.InverseTransformPoint(to);
        float half = grid.tileSize * 0.5f + radius;
        grid.WorldToTile(from, out int startX, out int startZ);
        int minX = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x,b.x)-radius)/grid.tileSize + grid.width*0.5f));
        int maxX = Mathf.Min(grid.width-1, Mathf.FloorToInt((Mathf.Max(a.x,b.x)+radius)/grid.tileSize + grid.width*0.5f));
        int minZ = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.z,b.z)-radius)/grid.tileSize + grid.depth*0.5f));
        int maxZ = Mathf.Min(grid.depth-1, Mathf.FloorToInt((Mathf.Max(a.z,b.z)+radius)/grid.tileSize + grid.depth*0.5f));
        for (int x=minX; x<=maxX; x++) for (int z=minZ; z<=maxZ; z++)
        {
            if (!blocked[x,z]) continue;
            // A defense can be built under a moving zombie; let it leave that cell.
            if (x==startX && z==startZ) continue;
            Vector3 center = grid.transform.InverseTransformPoint(grid.TileCenter(x,z));
            float enter=0f, exit=1f;
            if (IntersectAxis(a.x,b.x-a.x,center.x-half,center.x+half,ref enter,ref exit) &&
                IntersectAxis(a.z,b.z-a.z,center.z-half,center.z+half,ref enter,ref exit)) return false;
        }
        return true;
    }

    static bool IntersectAxis(float origin,float direction,float low,float high,ref float enter,ref float exit)
    {
        if (Mathf.Abs(direction)<0.00001f) return origin>=low && origin<=high;
        float a=(low-origin)/direction, b=(high-origin)/direction;
        enter=Mathf.Max(enter,Mathf.Min(a,b)); exit=Mathf.Min(exit,Mathf.Max(a,b));
        return enter<=exit;
    }

    public List<Vector2Int> FindPath(Vector2Int from, Vector2Int to)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        if (!ready) return path;
        if (!InBounds(from.x, from.y) || !InBounds(to.x, to.y)) return path;
        if (from == to) return path;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        frontier.Enqueue(from);
        cameFrom[from] = from;

        while (frontier.Count > 0)
        {
            Vector2Int cur = frontier.Dequeue();
            if (cur == to) break;
            foreach (var d in dirs)
            {
                Vector2Int n = cur + d;
                if (!InBounds(n.x, n.y)) continue;
                if (cameFrom.ContainsKey(n)) continue;
                if (blocked[n.x, n.y] && n != to) continue;
                cameFrom[n] = cur;
                frontier.Enqueue(n);
            }
        }

        if (!cameFrom.ContainsKey(to)) return path;

        Vector2Int c = to;
        while (c != from)
        {
            path.Add(c);
            c = cameFrom[c];
        }
        path.Reverse();
        return path;
    }
}
