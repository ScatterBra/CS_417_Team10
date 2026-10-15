using UnityEngine;

// Holds the harvest tools in slots. Each tool eases back to its slot if left alone (see MED_HarvestTool).
public class MED_ToolRack : MonoBehaviour
{
    [SerializeField] Transform[] slots;
    [SerializeField] MED_HarvestTool[] tools;

    void Start()
    {
        for (int i = 0; i < tools.Length && i < slots.Length; i++)
            if (tools[i] != null) tools[i].SetHome(slots[i]);
    }
}
