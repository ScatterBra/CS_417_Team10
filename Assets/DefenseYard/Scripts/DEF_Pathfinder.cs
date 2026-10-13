using UnityEngine;
using System.Collections.Generic;

public class DEF_Pathfinder : MonoBehaviour
{
    public static DEF_Pathfinder I;

    DEF_GridManager grid;
    bool[,] blocked;
    Dictionary<Vector2Int, GameObject> occupants = new Dictionary<Vector2Int, GameObject>();
    bool ready = false;

    void Awake()
    {
        I = this;
    }

    void Start()
    {
        grid = FindObjectOfType<DEF_GridManager>();
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
        var copy = new List<DEF_Zombie>(DEF_Zombie.all);
        foreach (var z in copy)
        {
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
