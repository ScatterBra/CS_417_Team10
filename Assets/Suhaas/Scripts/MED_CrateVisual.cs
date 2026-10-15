using TMPro;
using UnityEngine;

// Picks the crate contents at spawn (Alcohol or Gauze), colors the lid band, and opens the lid as supplies drop.
[RequireComponent(typeof(MED_Harvestable))]
public class MED_CrateVisual : MonoBehaviour
{
    [SerializeField] Transform lidPivot;
    [SerializeField] Renderer bandRenderer;
    [SerializeField] Material alcoholBand;
    [SerializeField] Material gauzeBand;
    [SerializeField] TextMeshPro label;
    [SerializeField] float maxLidAngle = 35f;

    MED_Harvestable harvestable;

    void Awake()
    {
        harvestable = GetComponent<MED_Harvestable>();
        var type = Random.value < 0.5f ? MED_ResourceType.Alcohol : MED_ResourceType.Gauze;
        harvestable.ConfigureResource(type);
        bool alcohol = type == MED_ResourceType.Alcohol;
        if (bandRenderer != null) bandRenderer.sharedMaterial = alcohol ? alcoholBand : gauzeBand;
        if (label != null) label.text = alcohol ? "ALCOHOL" : "GAUZE";
    }

    void Start()
    {
        var stat = harvestable.Stat;
        stat.OnChanged += _ => Refresh(stat);
        Refresh(stat);
    }

    void Refresh(MED_Stat stat)
    {
        if (lidPivot != null)
            lidPivot.localRotation = Quaternion.Euler(maxLidAngle * (1f - stat.Normalized), 0f, 0f);
    }
}
