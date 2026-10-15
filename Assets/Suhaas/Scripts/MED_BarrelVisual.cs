using TMPro;
using UnityEngine;

// Water surface follows the WaterLevel stat; an "EMPTY" label shows at 0.
[RequireComponent(typeof(MED_Harvestable))]
public class MED_BarrelVisual : MonoBehaviour
{
    [SerializeField] Transform water;
    [SerializeField] float emptyHeight = 0.06f;
    [SerializeField] float fullHeight = 0.82f;
    [SerializeField] TextMeshPro nameLabel;
    [SerializeField] TextMeshPro emptyLabel;

    void Start()
    {
        var stat = GetComponent<MED_Harvestable>().Stat;
        stat.OnChanged += _ => Refresh(stat);
        Refresh(stat);
    }

    void Refresh(MED_Stat stat)
    {
        bool empty = stat.Value <= 0f;
        if (water != null)
        {
            var p = water.localPosition;
            p.y = Mathf.Lerp(emptyHeight, fullHeight, stat.Normalized);
            water.localPosition = p;
            water.gameObject.SetActive(!empty);
        }
        if (emptyLabel != null) emptyLabel.gameObject.SetActive(empty);
        if (nameLabel != null) nameLabel.gameObject.SetActive(!empty);
    }
}
