using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Drives grid-snapped placement while a toy model is held: grid fade, ghost preview, build on release.
public class MED_BuildingPlacer : MonoBehaviour
{
    [SerializeField] MED_GridManager grid;
    [SerializeField] MED_ModelShelf shelf;
    [SerializeField] Transform buildingsContainer;
    [SerializeField] Material ghostValidMaterial;
    [SerializeField] Material ghostInvalidMaterial;
    [SerializeField] float maxRayDistance = 10f;

    struct Placement
    {
        public bool hasHit;
        public List<Vector2Int> cells;
        public Vector3 center;
        public int turns;
        public bool valid;
        public string reason;
    }

    MED_BuildingMini carried;
    IXRSelectInteractor interactor;
    GameObject ghost;
    Renderer[] ghostRenderers;
    bool ghostShowsValid;
    MED_PlayerResources resources;

    public bool IsCarrying => carried != null;

    // ---------- Carry lifecycle ----------

    public void BeginCarry(MED_BuildingMini mini, IXRSelectInteractor by)
    {
        EndGhost();
        carried = mini;
        interactor = by;
        grid.SetVisible(true);
        CreateGhost(mini.Definition);
    }

    public void EndCarry(MED_BuildingMini mini)
    {
        if (carried != mini)
        {
            mini.ReturnHome();
            return;
        }

        var p = Evaluate();
        var def = mini.Definition;
        carried = null;
        interactor = null;
        grid.SetVisible(false);
        EndGhost();

        if (p.hasHit && p.valid && TrySpendCosts(def, out _))
        {
            var building = Build(def, p);
            grid.TryOccupy(p.cells, building);
            mini.gameObject.SetActive(false);
            shelf.OnMiniConsumed(mini);
            Destroy(mini.gameObject);
            return;
        }

        string msg = !p.hasHit ? "Point at the floor inside the clinic" : (p.reason ?? "Can't build there");
        MED_PlayerHUD.ShowMessage(msg);
        mini.ReturnHome();
    }

    /// <summary>Destroys every placed building, frees all cells and restocks the shelf. No refunds.</summary>
    public void ClearAll()
    {
        foreach (var b in buildingsContainer.GetComponentsInChildren<MED_Building>())
        {
            grid.Free(b);
            Destroy(b.gameObject);
        }
        grid.FreeAll();
        shelf.RestockAll();
    }

    void OnDisable() => EndGhost();

    // ---------- Per-frame preview ----------

    void Update()
    {
        if (carried == null) return;
        var p = Evaluate();
        if (!p.hasHit)
        {
            if (ghost != null) ghost.SetActive(false);
            return;
        }
        if (ghost == null) return;
        ghost.SetActive(true);
        ghost.transform.SetPositionAndRotation(p.center, grid.transform.rotation * Quaternion.Euler(0f, 90f * p.turns, 0f));
        if (p.valid != ghostShowsValid) SetGhostMaterial(p.valid);
    }

    Placement Evaluate()
    {
        var p = new Placement { cells = new List<Vector2Int>() };
        if (carried == null || !TryGetRay(out var origin, out var dir)) return p;

        float planeY = grid.transform.position.y;
        if (dir.y > -0.02f) return p;
        float t = (planeY - origin.y) / dir.y;
        if (t < 0f || t > maxRayDistance) return p;

        var anchor = grid.WorldToCell(origin + dir * t);
        if (!grid.IsInBounds(anchor)) return p;

        var def = carried.Definition;
        p.hasHit = true;
        p.turns = HeadQuarterTurns();
        p.cells = grid.GetFootprintCells(anchor, def.footprint, p.turns);
        p.center = grid.GetFootprintCenter(p.cells);

        foreach (var c in p.cells)
            if (!grid.IsCellFree(c)) { p.reason = "Can't build there"; break; }
        if (p.reason == null && !CanAfford(def, out var why)) p.reason = why;
        p.valid = p.reason == null;
        return p;
    }

    bool TryGetRay(out Vector3 origin, out Vector3 dir)
    {
        Transform t = null;
        if (interactor is IXRRayProvider rp) t = rp.GetOrCreateRayOrigin();
        if (t == null && interactor is Component c) t = c.transform;
        origin = t != null ? t.position : Vector3.zero;
        dir = t != null ? t.forward : Vector3.forward;
        return t != null;
    }

    int HeadQuarterTurns()
    {
        var cam = Camera.main;
        if (cam == null) return 0;
        var f = cam.transform.forward;
        float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        float rel = yaw - grid.transform.eulerAngles.y;
        return ((Mathf.RoundToInt(rel / 90f) % 4) + 4) % 4;
    }

    // ---------- Costs ----------

    MED_PlayerResources Resources
    {
        get
        {
            if (resources == null) resources = FindAnyObjectByType<MED_PlayerResources>();
            return resources;
        }
    }

    bool CanAfford(MED_BuildingDefinition def, out string reason)
    {
        reason = null;
        if (def.costs == null || def.costs.Count == 0) return true;
        var r = Resources;
        foreach (var cost in def.costs)
            if (r == null || r.Get(cost.type) < cost.amount)
            {
                reason = $"Need {cost.amount} {cost.type}";
                return false;
            }
        return true;
    }

    bool TrySpendCosts(MED_BuildingDefinition def, out string reason)
    {
        if (!CanAfford(def, out reason)) return false;
        if (def.costs == null) return true;
        foreach (var cost in def.costs)
            Resources.TrySpend(cost.type, cost.amount);
        return true;
    }

    // ---------- Build ----------

    MED_Building Build(MED_BuildingDefinition def, Placement p)
    {
        var go = Instantiate(def.fullPrefab, buildingsContainer);
        go.transform.SetPositionAndRotation(p.center, grid.transform.rotation * Quaternion.Euler(0f, 90f * p.turns, 0f));
        var b = go.GetComponent<MED_Building>();
        if (b == null) b = go.AddComponent<MED_Building>();
        b.Initialize(def, p.cells.ToArray(), p.turns);
        return b;
    }

    // ---------- Ghost ----------

    void CreateGhost(MED_BuildingDefinition def)
    {
        if (def == null || def.fullPrefab == null) return;
        ghost = Instantiate(def.fullPrefab);
        ghost.name = "MED_BuildingGhost";
        foreach (var b in ghost.GetComponentsInChildren<MED_Building>()) Destroy(b);
        foreach (var c in ghost.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (var label in ghost.GetComponentsInChildren<TMP_Text>()) label.gameObject.SetActive(false);
        ghostRenderers = ghost.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in ghostRenderers)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        ghost.SetActive(false);
        SetGhostMaterial(false);
    }

    void SetGhostMaterial(bool valid)
    {
        ghostShowsValid = valid;
        var mat = valid ? ghostValidMaterial : ghostInvalidMaterial;
        if (ghostRenderers == null) return;
        foreach (var r in ghostRenderers)
        {
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    void EndGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
        ghostRenderers = null;
    }
}
