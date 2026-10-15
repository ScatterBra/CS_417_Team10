using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct MED_ResourceCost
{
    public MED_ResourceType type;
    public int amount;
}

[CreateAssetMenu(menuName = "MED/Building Definition", fileName = "MED_Def_New")]
public class MED_BuildingDefinition : ScriptableObject
{
    public string displayName;
    public GameObject fullPrefab;
    public GameObject miniPrefab;
    [Tooltip("Footprint in cells along the building's local X (width) and Z (depth).")]
    public Vector2Int footprint = new Vector2Int(1, 1);
    [Tooltip("Resource costs. Empty in Step 3; checked and spent through MED_PlayerResources.TrySpend if present.")]
    public List<MED_ResourceCost> costs = new List<MED_ResourceCost>();
}
