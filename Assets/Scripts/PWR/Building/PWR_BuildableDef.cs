using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>Data for one buildable item in the wrist build menu.</summary>
    [CreateAssetMenu(menuName = "Deadzone/Power/Buildable Def", fileName = "PWR_Buildable_")]
    public class PWR_BuildableDef : ScriptableObject
    {
        public string displayName = "Building";
        public PWR_GridBuilding prefab;

        public List<PWR_ResourceAmount> cost = new List<PWR_ResourceAmount>();

        [Tooltip("If set, can only be built on top of a foundation building with this id.")]
        public string requiredFoundationId = "";

        [Tooltip("0 = unlimited.")]
        public int maxCount;

        [Header("Placement ghost (filled in by the editor builder)")]
        public Vector3 ghostSize = Vector3.one;
        public Vector3 ghostCenter = new Vector3(0f, 0.5f, 0f);

        public bool RequiresFoundation => !string.IsNullOrEmpty(requiredFoundationId);
        public string BuildingId => prefab != null ? prefab.buildingId : name;

        public string CostText()
        {
            if (cost == null || cost.Count == 0) return "Free";
            var parts = new List<string>();
            foreach (var c in cost) parts.Add(c.ToString());
            return string.Join(", ", parts);
        }
    }
}
