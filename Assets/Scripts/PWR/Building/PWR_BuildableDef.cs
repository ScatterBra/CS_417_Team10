using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>Data for one buildable item in the build menu.</summary>
    [CreateAssetMenu(menuName = "Deadzone/Power/Buildable Def", fileName = "PWR_Buildable_")]
    public class PWR_BuildableDef : ScriptableObject
    {
        public string displayName = "Building";
        public PWR_GridBuilding prefab;

        [Tooltip("Optional visual-only ghost shown while aiming. Should have no colliders or gameplay scripts.")]
        public GameObject previewPrefab;

        public List<PWR_ResourceAmount> cost = new List<PWR_ResourceAmount>();

        [Tooltip("If set, can only be built on top of a foundation building with this id.")]
        public string requiredFoundationId = "";

        public bool RequiresFoundation => !string.IsNullOrEmpty(requiredFoundationId);
    }
}
