using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Input-agnostic build logic (rubric: Building Affordance + Gridded Buildings + Foundation).
    /// An input layer (XR ray, mouse) calls UpdatePreview while aiming and TryBuild on confirm.
    /// Demolish mode removes something the player built (UpdateDemolishPreview / TryDemolish): the
    /// cost is refunded and a machine's stats (charge, fuel) carry over to the next one of its kind,
    /// so moving a building neither loses nor gains anything.
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
        [Tooltip("Floor area a power building reaches (the grid's neighbor range), shown while placing it.")]
        public Color rangeColor = new Color(1f, 0.75f, 0.15f, 0.4f);
        [Tooltip("Box around the building that demolish mode is aiming at.")]
        public Color demolishColor = new Color(1f, 0.25f, 0.15f, 0.45f);
        public Color demolishBlockedColor = new Color(0.6f, 0.6f, 0.6f, 0.3f);

        /// <summary>(def, placedInstance)</summary>
        public event Action<PWR_BuildableDef, PWR_GridBuilding> OnBuilt;
        public event Action<PWR_BuildableDef> OnSelectionChanged;
        /// <summary>(def, removedInstance) — the instance is destroyed at the end of the frame.</summary>
        public event Action<PWR_BuildableDef, PWR_GridBuilding> OnDemolished;
        public event Action<bool> OnDemolishModeChanged;
        /// <summary>A building was locked or unlocked (PWR_Progression).</summary>
        public event Action OnUnlocksChanged;

        public int QuarterTurns { get; private set; }
        public string LastMessage { get; private set; } = "";
        public bool Demolishing { get; private set; }
        /// <summary>Placing a building or demolishing: the build controls own the trigger.</summary>
        public bool IsActive => selected != null || Demolishing;

        private readonly Dictionary<string, int> builtCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, string> lockHints = new Dictionary<string, string>();
        private readonly Dictionary<PWR_GridBuilding, PWR_BuildableDef> placed = new Dictionary<PWR_GridBuilding, PWR_BuildableDef>();
        private readonly Dictionary<string, Dictionary<string, float>> keptStats = new Dictionary<string, Dictionary<string, float>>();
        private Transform demolishBox;
        private PWR_GridBuilding demolishTarget;
        private MaterialPropertyBlock demolishMpb;
        private GameObject ghost;
        private Transform ghostBox;
        private PWR_BuildableDef ghostDef;
        private Transform rangeArea;
        private PWR_PowerRole ghostRole;
        private readonly List<PWR_PowerNode> rangeNodes = new List<PWR_PowerNode>();
        private readonly List<string> linkNames = new List<string>();
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock mpb;
        private MaterialPropertyBlock rangeMpb;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            mpb = new MaterialPropertyBlock();
            rangeMpb = new MaterialPropertyBlock();
            rangeMpb.SetColor(BaseColorId, rangeColor);
            demolishMpb = new MaterialPropertyBlock();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- unlocks (rubric: Mastery Thresholding) ----------

        public bool IsUnlocked(PWR_BuildableDef def) => def == null || !lockHints.ContainsKey(def.BuildingId);

        /// <summary>Hides a building from the menu; the hint says how to unlock it.</summary>
        public void Lock(string buildingId, string hint)
        {
            lockHints[buildingId] = hint;
            if (selected != null && selected.BuildingId == buildingId) Select(null);
            OnUnlocksChanged?.Invoke();
        }

        public void Unlock(string buildingId)
        {
            if (lockHints.Remove(buildingId)) OnUnlocksChanged?.Invoke();
        }

        private string LockedText(PWR_BuildableDef def) =>
            $"{def.displayName} is locked. {(lockHints.TryGetValue(def.BuildingId, out var hint) ? hint : "")}";

        public void Select(PWR_BuildableDef def)
        {
            if (def != null && !IsUnlocked(def))
            {
                LastMessage = LockedText(def);
                return;
            }
            bool wasDemolishing = Demolishing;
            Demolishing = false;
            selected = def;
            HidePreview();
            if (PWR_GridManager.Instance != null) PWR_GridManager.Instance.SetOverlayVisible(def != null);
            LastMessage = def != null ? $"Placing {def.displayName} ({def.CostText()})" : "";
            OnSelectionChanged?.Invoke(def);
            if (wasDemolishing) OnDemolishModeChanged?.Invoke(false);
        }

        public void SelectDemolish()
        {
            Select(null);
            Demolishing = true;
            if (PWR_GridManager.Instance != null) PWR_GridManager.Instance.SetOverlayVisible(true);
            LastMessage = "Demolish: aim at something you built";
            OnDemolishModeChanged?.Invoke(true);
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
            if (!IsUnlocked(def)) { reason = LockedText(def); return false; }
            if (def.maxCount > 0 && CountBuilt(def.BuildingId) >= def.maxCount)
            {
                reason = $"Only {def.maxCount} {def.displayName} allowed. Use Demolish to move the one you built.";
                return false;
            }

            Vector2Int cell = grid.WorldToCell(worldPos);
            if (!grid.InBounds(cell)) { reason = "Can't build here: outside the build area. Aim at the floor grid."; return false; }

            var top = grid.GetTop(cell);
            if (def.RequiresFoundation)
            {
                if (top == null || !top.isFoundation || top.buildingId != def.requiredFoundationId)
                {
                    reason = top != null && top.buildingId == def.BuildingId ? OccupiedText
                        : $"{def.displayName} needs a Concrete Pad underneath. Place a pad here first.";
                    return false;
                }
            }
            else if (top != null)
            {
                reason = OccupiedText;
                return false;
            }

            var res = PWR_PlayerResources.Instance;
            if (res != null && !res.CanAfford(def.cost)) { reason = MissingCostText(def, res); return false; }
            return true;
        }

        private const string OccupiedText = "Can't build here: this cell is taken. Pick an empty cell or demolish what's there.";

        private static string MissingCostText(PWR_BuildableDef def, PWR_PlayerResources res)
        {
            foreach (var c in def.cost)
                if (res.Get(c.type) < c.amount)
                    return $"{def.displayName} needs {c}. {PWR_Names.HowToGet(c.type)}";
            return $"{def.displayName} needs {def.CostText()}.";
        }

        public PWR_GridBuilding TryBuild(Vector3 worldPos) => TryBuild(selected, worldPos);

        public PWR_GridBuilding TryBuild(PWR_BuildableDef def, Vector3 worldPos)
        {
            if (!CanBuild(def, worldPos, out var reason))
            {
                LastMessage = reason;
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
            placed[instance] = def;
            RestoreKeptStats(def, instance);

            builtCounts[def.BuildingId] = CountBuilt(def.BuildingId) + 1;
            OnBuilt?.Invoke(def, instance);

            // A limited building is done once its last copy is placed.
            if (def.maxCount > 0 && CountBuilt(def.BuildingId) >= def.maxCount) Select(null);
            LastMessage = $"Built {def.displayName}";
            return instance;
        }

        // ---------- demolish ----------

        /// <summary>
        /// The building along the ray: the first grid building hit, or else whatever is built in the
        /// floor cell the ray lands on (thin pads and belts are easier to pick that way).
        /// </summary>
        public PWR_GridBuilding FindDemolishTarget(Ray ray, float maxDistance)
        {
            var grid = PWR_GridManager.Instance;
            if (grid == null) return null;
            var hits = Physics.RaycastAll(ray, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<PWR_Grabbable>() != null) continue; // loose items
                var building = hit.collider.GetComponentInParent<PWR_GridBuilding>();
                // Just demolished: already off the grid, destroyed at the end of the frame.
                if (building != null && !InGrid(grid, building)) continue;
                if (building != null) return building;
                return grid.GetTop(grid.WorldToCell(hit.point));
            }
            return null;
        }

        private static bool InGrid(PWR_GridManager grid, PWR_GridBuilding building)
        {
            foreach (var b in grid.GetStack(building.Cell))
                if (b == building) return true;
            return false;
        }

        public bool CanDemolish(PWR_GridBuilding target, out PWR_BuildableDef def, out string reason)
        {
            def = null;
            reason = null;
            if (target == null) { reason = "Nothing to demolish here. Aim at something you built."; return false; }
            if (!placed.TryGetValue(target, out def)) { reason = "Can't demolish this: only things you built can be removed."; return false; }
            var grid = PWR_GridManager.Instance;
            if (grid != null && grid.GetTop(target.Cell) != target) { reason = "Something is built on top of this. Demolish that first."; return false; }
            return true;
        }

        public void UpdateDemolishPreview(Ray ray, float maxDistance)
        {
            if (!Demolishing) return;
            demolishTarget = FindDemolishTarget(ray, maxDistance);
            if (demolishBox == null)
            {
                demolishBox = MakeGhostPart(PrimitiveType.Cube, null, Vector3.zero, Vector3.one);
                demolishBox.name = "PWR_DemolishBox";
            }
            if (demolishTarget == null)
            {
                demolishBox.gameObject.SetActive(false);
                LastMessage = "Demolish: aim at something you built";
                return;
            }

            var renderers = demolishTarget.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds(demolishTarget.transform.position, Vector3.zero);
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            demolishBox.gameObject.SetActive(true);
            demolishBox.SetPositionAndRotation(bounds.center, Quaternion.identity);
            demolishBox.localScale = bounds.size + Vector3.one * 0.06f;

            bool ok = CanDemolish(demolishTarget, out var def, out var reason);
            demolishMpb.SetColor(BaseColorId, ok ? demolishColor : demolishBlockedColor);
            demolishBox.GetComponent<Renderer>().SetPropertyBlock(demolishMpb);
            LastMessage = ok ? $"Demolish {def.displayName}" + (def.cost.Count > 0 ? $" (+{def.CostText()})" : "") : reason;
        }

        /// <summary>Removes the building the last preview aimed at. Refunds its cost.</summary>
        public bool TryDemolish(Vector3 feedbackPos)
        {
            var target = demolishTarget;
            if (!CanDemolish(target, out var def, out var reason))
            {
                LastMessage = reason;
                PWR_Feedback.Show(feedbackPos, reason, PWR_Feedback.Bad);
                return false;
            }

            Vector3 at = target.transform.position + Vector3.up;
            KeepStats(def, target);
            var grid = PWR_GridManager.Instance;
            if (grid != null) grid.Unregister(target, target.Cell); // now, not at end of frame
            placed.Remove(target);
            builtCounts[def.BuildingId] = Mathf.Max(0, CountBuilt(def.BuildingId) - 1);
            var res = PWR_PlayerResources.Instance;
            if (res != null)
                foreach (var c in def.cost) res.Add(c.type, c.amount);
            Destroy(target.gameObject);
            demolishTarget = null;
            if (demolishBox != null) demolishBox.gameObject.SetActive(false);

            string refund = def.cost.Count > 0 ? $" (+{def.CostText()})" : "";
            LastMessage = $"Demolished {def.displayName}{refund}";
            PWR_Feedback.Show(at, LastMessage, PWR_Feedback.Good);
            OnDemolished?.Invoke(def, target);
            return true;
        }

        /// <summary>Remembers a demolished machine's stats for the next one of its kind.</summary>
        private void KeepStats(PWR_BuildableDef def, PWR_GridBuilding building)
        {
            if (!building.TryGetComponent(out PWR_StatBlock stats) || stats.Stats.Count == 0) return;
            var values = new Dictionary<string, float>();
            foreach (var s in stats.Stats)
                if (s != null && !string.IsNullOrEmpty(s.name)) values[s.name] = s.value;
            keptStats[def.BuildingId] = values;
        }

        private void RestoreKeptStats(PWR_BuildableDef def, PWR_GridBuilding building)
        {
            if (!keptStats.TryGetValue(def.BuildingId, out var values)) return;
            keptStats.Remove(def.BuildingId);
            if (!building.TryGetComponent(out PWR_StatBlock stats)) return;
            foreach (var kv in values)
                if (stats.Has(kv.Key)) stats.Set(kv.Key, kv.Value);
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
            if (rangeArea != null) rangeArea.gameObject.SetActive(inBounds);
            if (!inBounds) return;
            ghost.transform.SetPositionAndRotation(
                grid.CellToWorld(cell) + grid.transform.up * grid.GetStackHeight(cell), PlacementRotation);

            bool ok = CanBuild(selected, worldPos, out var reason);
            LastMessage = ok ? $"Placing {selected.displayName} ({selected.CostText()})" : reason;
            mpb.SetColor(BaseColorId, ok ? validColor : invalidColor);
            foreach (var r in ghost.GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(mpb);

            if (rangeArea != null)
            {
                PlaceRangeArea(grid, cell);
                if (ok) LastMessage += "\n" + LinksText(grid, cell);
            }
        }

        /// <summary>Covers the cells within the grid's neighbor range, clipped to the grid.</summary>
        private void PlaceRangeArea(PWR_GridManager grid, Vector2Int cell)
        {
            var r = new Vector2Int(grid.neighborRange, grid.neighborRange);
            var min = Vector2Int.Max(cell - r, Vector2Int.zero);
            var max = Vector2Int.Min(cell + r, grid.gridSize - Vector2Int.one);
            float cs = grid.cellSize;
            var center = new Vector3((min.x + max.x + 1) * 0.5f * cs, 0.02f, (min.y + max.y + 1) * 0.5f * cs);
            rangeArea.SetPositionAndRotation(grid.transform.TransformPoint(center),
                grid.transform.rotation * Quaternion.Euler(90f, 0f, 0f));
            rangeArea.localScale = new Vector3((max.x - min.x + 1) * cs, (max.y - min.y + 1) * cs, 1f);
        }

        /// <summary>Which machines in range this building would exchange power with.</summary>
        private string LinksText(PWR_GridManager grid, Vector2Int cell)
        {
            grid.GetNeighbors(cell, rangeNodes);
            linkNames.Clear();
            foreach (var n in rangeNodes)
            {
                if (n == null || !(PWR_PowerNode.Feeds(ghostRole, n.role) || PWR_PowerNode.Feeds(n.role, ghostRole))) continue;
                string name = n.TryGetComponent(out PWR_StatLabel label) && !string.IsNullOrEmpty(label.title) ? label.title : n.name;
                if (!linkNames.Contains(name)) linkNames.Add(name);
            }
            return linkNames.Count > 0 ? "Connects to: " + string.Join(", ", linkNames) : "Nothing to connect to in range";
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

            // Power buildings also show the floor area their power reaches. It is a separate object
            // so it stays aligned with the grid when the ghost rotates.
            if (def.prefab.TryGetComponent(out PWR_PowerNode node))
            {
                ghostRole = node.role;
                rangeArea = MakeGhostPart(PrimitiveType.Quad, null, Vector3.zero, Vector3.one);
                rangeArea.name = "PWR_BuildRange";
                rangeArea.GetComponent<Renderer>().SetPropertyBlock(rangeMpb);
            }
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

        /// <summary>World point on top of the placement ghost or demolish box while shown (the VR status label sits here).</summary>
        public bool TryGetPreviewTop(out Vector3 point)
        {
            point = default;
            if (Demolishing)
            {
                if (demolishBox == null || !demolishBox.gameObject.activeSelf) return false;
                point = demolishBox.position + Vector3.up * demolishBox.localScale.y * 0.5f;
                return true;
            }
            if (ghost == null || !ghost.activeSelf || ghostDef == null) return false;
            point = ghost.transform.position + ghost.transform.up * (ghostDef.ghostCenter.y + ghostDef.ghostSize.y * 0.5f);
            return true;
        }

        public void HidePreview()
        {
            if (ghost != null) Destroy(ghost);
            if (rangeArea != null) Destroy(rangeArea.gameObject);
            if (demolishBox != null) demolishBox.gameObject.SetActive(false);
            demolishTarget = null;
            ghost = null;
            rangeArea = null;
            ghostDef = null;
        }
    }
}
