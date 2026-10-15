using UnityEngine;

// One plant per remaining unit; plants disappear as the stat drops.
[RequireComponent(typeof(MED_Harvestable))]
public class MED_HerbPatchVisual : MonoBehaviour
{
    [SerializeField] GameObject[] plants;

    void Start()
    {
        var stat = GetComponent<MED_Harvestable>().Stat;
        stat.OnChanged += _ => Refresh(stat);
        Refresh(stat);
    }

    void Refresh(MED_Stat stat)
    {
        for (int i = 0; i < plants.Length; i++)
            if (plants[i] != null) plants[i].SetActive(i < Mathf.RoundToInt(stat.Value));
    }
}
