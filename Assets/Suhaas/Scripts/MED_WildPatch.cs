using System.Collections.Generic;
using UnityEngine;

public enum MED_PatchCell { Empty, Herb, Crate }

/// <summary>
/// A hand-designed stamp for MED_WildSpawner: a small grid layout of Herb / Crate / Empty cells.
/// The layout is typed as rows of characters, top row first: H = herb patch, C = supply crate, . = empty
/// (e.g. ".H." / "HHH" / ".H." is a 5-herb cross). Rows may differ in length; short rows are padded with empty.
/// All cells of the rectangle, empty ones included, must be free for the patch to be stamped.
/// </summary>
[CreateAssetMenu(menuName = "MED/Wild Patch", fileName = "MED_Patch_New")]
public class MED_WildPatch : ScriptableObject
{
    public string displayName;
    [TextArea(2, 5)] public string layout = ".H.\nHHH\n.H.";

    public struct StampCell
    {
        public Vector2Int offset;   // from the stamp's min corner
        public MED_PatchCell kind;
    }

    /// <summary>
    /// The layout rotated by quarterTurns x 90 degrees. Returns every cell (empty ones too) with its offset from the
    /// min corner of the rotated rectangle, and the rectangle size.
    /// </summary>
    public List<StampCell> GetCells(int quarterTurns, out Vector2Int size)
    {
        var rows = (layout ?? "").Replace("\r", "").Split('\n');
        int h = rows.Length, w = 0;
        foreach (var r in rows) w = Mathf.Max(w, r.Length);
        w = Mathf.Max(1, w);

        var kinds = new MED_PatchCell[w, h];   // [x, z]; the first text row is the highest z
        for (int row = 0; row < h; row++)
            for (int x = 0; x < rows[row].Length; x++)
                kinds[x, h - 1 - row] = Parse(rows[row][x]);

        int turns = ((quarterTurns % 4) + 4) % 4;
        for (int t = 0; t < turns; t++)
        {
            // Rotate 90 degrees: (x, z) -> (z, w - 1 - x); the rectangle swaps width and height.
            var next = new MED_PatchCell[h, w];
            for (int x = 0; x < w; x++)
                for (int z = 0; z < h; z++)
                    next[z, w - 1 - x] = kinds[x, z];
            kinds = next;
            int tmp = w; w = h; h = tmp;
        }

        size = new Vector2Int(w, h);
        var cells = new List<StampCell>(w * h);
        for (int x = 0; x < w; x++)
            for (int z = 0; z < h; z++)
                cells.Add(new StampCell { offset = new Vector2Int(x, z), kind = kinds[x, z] });
        return cells;
    }

    static MED_PatchCell Parse(char c)
    {
        switch (char.ToUpperInvariant(c))
        {
            case 'H': return MED_PatchCell.Herb;
            case 'C': return MED_PatchCell.Crate;
            default: return MED_PatchCell.Empty;
        }
    }

    public int Count(MED_PatchCell kind)
    {
        int n = 0;
        foreach (var c in GetCells(0, out _)) if (c.kind == kind) n++;
        return n;
    }
}
