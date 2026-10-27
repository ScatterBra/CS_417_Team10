using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(DEF_ContentGate))]
public class DEF_AmmoCrate : MonoBehaviour
{
    void Awake() { if (GetComponent<DEF_ContentGate>() == null) gameObject.AddComponent<DEF_ContentGate>(); }
    [FormerlySerializedAs("supply")] public int ammoRemaining = 40;
    public int supply { get => ammoRemaining; set => ammoRemaining = value; }
    public int maxSupply = 40;

    [Min(1f)] public float refillSeconds = 20f;
    [SerializeField] float refillRemaining;
    public float RefillRemaining => refillRemaining;
    public bool IsRefilling => supply <= 0 && refillRemaining > 0;

    Vector2Int cell;
    bool placed = false;

    public static void SpawnAt(Vector3 pos, int amount)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/DEF_AmmoCrate");
        if (prefab == null) { Debug.LogError("Ammo crate gameplay prefab is missing."); return; }
        GameObject go = Instantiate(prefab);
        go.transform.position = pos;
        DEF_AmmoCrate c = go.GetComponent<DEF_AmmoCrate>();
        c.supply = amount;
        c.maxSupply = amount;
        var grid = FindFirstObjectByType<DEF_GridManager>();
        var pf = DEF_Pathfinder.I;
        int x, z;
        if (grid != null && pf != null && pf.Ready && grid.WorldToTile(pos, out x, out z))
        {
            go.transform.position = grid.TileCenter(x, z);
            c.cell = new Vector2Int(x, z);
            c.placed = true;
            pf.SetBlocked(x, z, true);
            pf.Register(c.cell, go);
        }
    }

    void OnDestroy()
    {
        ClearCell();
    }

    public void ClearCell()
    {
        if (placed && DEF_Pathfinder.I != null)
        {
            var pf = DEF_Pathfinder.I;
            if (pf.OccupantAt(cell.x, cell.y) == gameObject)
            { pf.SetBlocked(cell.x, cell.y, false); pf.Unregister(cell); pf.RepathAll(); }
            placed = false;
        }
    }

    void Update()
    {
        if (!IsRefilling || DEF_GameState.I == null || DEF_GameState.I.playerHealth <= 0) return;
        refillRemaining = Mathf.Max(0, refillRemaining - Time.deltaTime);
        if (refillRemaining <= 0) Restock();
    }

    public void Restock()
    {
        supply = maxSupply;
        refillRemaining = 0;
        transform.localScale = Vector3.one;
    }

    public int Harvest(int amount)
    {
        if (!DEF_Progression.Allows(DEF_Action.AmmoCrate)) return 0;
        var state = DEF_GameState.I;
        if (supply <= 0)
        {
            DEF_HUD.Instance?.Notify("SUPPLY REFILLING  /  " + Mathf.CeilToInt(refillRemaining) + "s remaining");
            return 0;
        }
        int room = state != null ? Mathf.Max(0, state.maxAmmo - state.ammo) : amount;
        int taken = Mathf.Min(Mathf.Max(0, amount), Mathf.Min(supply, room));
        if (taken <= 0) return 0;
        supply -= taken;
        if (state != null) state.ammo += taken;
        DEF_HUD.Instance?.RecordHarvest();
        DEF_Audio.Play("ammo_pickup", transform.position, 0.75f);
        DEF_Tutorials.Show("harvest", transform, "SUPPLY CRATE\nRight grip collects 10 ammo.\nRefills in 20s or on wave clear.");
        float fraction = (float)supply / Mathf.Max(1, maxSupply);
        transform.localScale = Vector3.one * (0.5f + 0.5f * fraction);
        if (supply <= 0) refillRemaining = Mathf.Max(1f, refillSeconds);
        return taken;
    }
}
