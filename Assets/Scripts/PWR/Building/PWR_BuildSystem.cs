using System;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Input-agnostic build logic (rubric: Building Affordance + Gridded Buildings + Foundation).
    /// An input layer (XR ray, mouse, etc.) calls UpdatePreview while aiming and TryBuild on confirm.
    /// </summary>
    public class PWR_BuildSystem : MonoBehaviour
    {
        public static PWR_BuildSystem Instance { get; private set; }

        public PWR_BuildableDef selected;
        public Color validColor = new Color(0.2f, 1f, 0.3f, 0.5f);
        public Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.5f);

        /// <summary>(def, placedInstance)</summary>
        public event Action<PWR_BuildableDef, PWR_GridBuilding> OnBuilt;

        private GameObject preview;
        private PWR_BuildableDef previewDef;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock mpb;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            mpb = new MaterialPropertyBlock();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Select(PWR_BuildableDef def)
        {
            selected = def;
            HidePreview();
        }

        public bool CanBuild(PWR_BuildableDef def, Vector3 worldPos, out string reason)
        {
            var grid = PWR_GridManager.Instance;
            reason = null;
            if (def == null || def.prefab == null) { reason = "Nothing selected"; return false; }
            if (grid == null) { reason = "No grid"; return false; }

            Vector2Int cell = grid.WorldToCell(worldPos);
            if (!grid.InBounds(cell)) { reason = "Outside build area"; return false; }

            var top = grid.GetTop(cell);
            if (def.RequiresFoundation)
            {
                if (top == null || !top.isFoundation || top.buildingId != def.requiredFoundationId)
                {
                    reason = $"Needs a {def.requiredFoundationId} first";
                    return false;
                }
            }
            else if (top != null)
            {
                reason = "Cell occupied";
                return false;
            }

            var res = PWR_PlayerResources.Instance;
            if (res != null && !res.CanAfford(def.cost)) { reason = "Not enough resources"; return false; }
            return true;
        }

        public PWR_GridBuilding TryBuild(Vector3 worldPos) => TryBuild(selected, worldPos);

        public PWR_GridBuilding TryBuild(PWR_BuildableDef def, Vector3 worldPos)
        {
            if (!CanBuild(def, worldPos, out var reason))
            {
                Debug.Log($"[PWR] Can't build {def?.displayName}: {reason}");
                return null;
            }

            var grid = PWR_GridManager.Instance;
            var res = PWR_PlayerResources.Instance;
            if (res != null) res.TrySpend(def.cost);

            Vector2Int cell = grid.WorldToCell(worldPos);
            Vector3 pos = grid.CellToWorld(cell) + grid.transform.up * grid.GetStackHeight(cell);
            var instance = Instantiate(def.prefab, pos, grid.transform.rotation);
            instance.registerOnStart = false;
            instance.Register(cell);

            OnBuilt?.Invoke(def, instance);
            return instance;
        }

        // ---------- preview ghost ----------

        public void UpdatePreview(Vector3 worldPos)
        {
            if (selected == null || selected.previewPrefab == null || PWR_GridManager.Instance == null)
            {
                HidePreview();
                return;
            }
            if (preview == null || previewDef != selected)
            {
                HidePreview();
                preview = Instantiate(selected.previewPrefab);
                previewDef = selected;
            }

            var grid = PWR_GridManager.Instance;
            Vector2Int cell = grid.WorldToCell(worldPos);
            preview.SetActive(grid.InBounds(cell));
            preview.transform.SetPositionAndRotation(
                grid.CellToWorld(cell) + grid.transform.up * grid.GetStackHeight(cell),
                grid.transform.rotation);

            mpb.SetColor(BaseColorId, CanBuild(selected, worldPos, out _) ? validColor : invalidColor);
            foreach (var r in preview.GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(mpb);
        }

        public void HidePreview()
        {
            if (preview != null) Destroy(preview);
            preview = null;
            previewDef = null;
        }
    }
}
