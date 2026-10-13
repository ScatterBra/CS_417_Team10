using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Input-agnostic build logic (rubric: Building Affordance + Gridded Buildings + Foundation).
    /// An input layer (XR ray, mouse) calls UpdatePreview while aiming and TryBuild on confirm.
    /// </summary>
    public class PWR_BuildSystem : MonoBehaviour
    {
        public static PWR_BuildSystem Instance { get; private set; }

        [Tooltip("Menu order: also the desktop hotkeys 1..n.")]
        public List<PWR_BuildableDef> buildables = new List<PWR_BuildableDef>();
        public PWR_BuildableDef selected;
        public Material ghostMaterial;
        public Color validColor = new Color(0.2f, 1f, 0.3f, 0.35f);
        public Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.35f);

        /// <summary>(def, placedInstance)</summary>
        public event Action<PWR_BuildableDef, PWR_GridBuilding> OnBuilt;
        public event Action<PWR_BuildableDef> OnSelectionChanged;

        public int QuarterTurns { get; private set; }
        public string LastMessage { get; private set; } = "";

        private readonly Dictionary<string, int> builtCounts = new Dictionary<string, int>();
        private GameObject ghost;
        private Transform ghostBox;
        private PWR_BuildableDef ghostDef;
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
            if (PWR_GridManager.Instance != null) PWR_GridManager.Instance.SetOverlayVisible(def != null);
            LastMessage = def != null ? $"Placing {def.displayName} ({def.CostText()})" : "";
            OnSelectionChanged?.Invoke(def);
        }

        public void Rotate(int direction)
        {
            QuarterTurns = ((QuarterTurns + direction) % 4 + 4) % 4;
        }

        public Quaternion PlacementRotation =>
            (PWR_GridManager.Instance != null ? PWR_GridManager.Instance.transform.rotation : Quaternion.identity)
            * Quaternion.Euler(0f, 90f * QuarterTurns, 0f);

        public int CountBuilt(string buildingId) => builtCounts.TryGetValue(buildingId, out var n) ? n : 0;

        public bool CanBuild(PWR_BuildableDef def, Vector3 worldPos, out string reason)
        {
            var grid = PWR_GridManager.Instance;
            reason = null;
            if (def == null || def.prefab == null) { reason = "Nothing selected"; return false; }
            if (grid == null) { reason = "No grid"; return false; }
            if (def.maxCount > 0 && CountBuilt(def.BuildingId) >= def.maxCount)
            {
                reason = $"Only {def.maxCount} {def.displayName} allowed";
                return false;
            }

            Vector2Int cell = grid.WorldToCell(worldPos);
            if (!grid.InBounds(cell)) { reason = "Outside build area"; return false; }

            var top = grid.GetTop(cell);
            if (def.RequiresFoundation)
            {
                if (top == null || !top.isFoundation || top.buildingId != def.requiredFoundationId)
                {
                    reason = top != null && top.buildingId == def.BuildingId ? "Cell occupied" : "Needs a Concrete Pad first";
                    return false;
                }
            }
            else if (top != null)
            {
                reason = "Cell occupied";
                return false;
            }

            var res = PWR_PlayerResources.Instance;
            if (res != null && !res.CanAfford(def.cost)) { reason = $"Needs {def.CostText()}"; return false; }
            return true;
        }

        public PWR_GridBuilding TryBuild(Vector3 worldPos) => TryBuild(selected, worldPos);

        public PWR_GridBuilding TryBuild(PWR_BuildableDef def, Vector3 worldPos)
        {
            if (!CanBuild(def, worldPos, out var reason))
            {
                LastMessage = $"Can't build: {reason}";
                PWR_Feedback.Show(worldPos + Vector3.up * 0.6f, reason, PWR_Feedback.Bad);
                return null;
            }

            var grid = PWR_GridManager.Instance;
            var res = PWR_PlayerResources.Instance;
            if (res != null) res.TrySpend(def.cost);

            Vector2Int cell = grid.WorldToCell(worldPos);
            Vector3 pos = grid.CellToWorld(cell) + grid.transform.up * grid.GetStackHeight(cell);
            var instance = Instantiate(def.prefab, pos, PlacementRotation);
            instance.registerOnStart = false;
            instance.Register(cell);

            builtCounts[def.BuildingId] = CountBuilt(def.BuildingId) + 1;
            OnBuilt?.Invoke(def, instance);

            // A limited building is done once its last copy is placed.
            if (def.maxCount > 0 && CountBuilt(def.BuildingId) >= def.maxCount) Select(null);
            LastMessage = $"Built {def.displayName}";
            return instance;
        }

        // ---------- preview ghost ----------

        public void UpdatePreview(Vector3 worldPos)
        {
            var grid = PWR_GridManager.Instance;
            if (selected == null || grid == null)
            {
                HidePreview();
                return;
            }
            if (ghost == null || ghostDef != selected) CreateGhost(selected);

            Vector2Int cell = grid.WorldToCell(worldPos);
            bool inBounds = grid.InBounds(cell);
            ghost.SetActive(inBounds);
            if (!inBounds) return;
            ghost.transform.SetPositionAndRotation(
                grid.CellToWorld(cell) + grid.transform.up * grid.GetStackHeight(cell), PlacementRotation);

            bool ok = CanBuild(selected, worldPos, out var reason);
            LastMessage = ok ? $"Placing {selected.displayName} ({selected.CostText()})" : reason;
            mpb.SetColor(BaseColorId, ok ? validColor : invalidColor);
            foreach (var r in ghost.GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(mpb);
        }

        private void CreateGhost(PWR_BuildableDef def)
        {
            HidePreview();
            ghostDef = def;
            ghost = new GameObject("PWR_BuildGhost");
            ghostBox = MakeGhostPart(PrimitiveType.Cube, ghost.transform, def.ghostCenter, def.ghostSize);
            // Arrow showing the building's forward (+Z): matters for conveyor belts.
            float len = Mathf.Max(0.3f, def.ghostSize.z * 0.5f);
            MakeGhostPart(PrimitiveType.Cube, ghost.transform,
                new Vector3(0f, def.ghostCenter.y + def.ghostSize.y * 0.5f + 0.03f, len * 0.5f),
                new Vector3(0.08f, 0.04f, len));
        }

        private Transform MakeGhostPart(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(type);
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = scale;
            var r = part.GetComponent<Renderer>();
            if (ghostMaterial != null) r.sharedMaterial = ghostMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return part.transform;
        }

        public void HidePreview()
        {
            if (ghost != null) Destroy(ghost);
            ghost = null;
            ghostDef = null;
        }
    }
}
