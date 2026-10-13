using UnityEngine;

public class SCV_DamagedPanel : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;
    [SerializeField] private SCV_DamagedPanelDisplay panelDisplay;

    [Header("Condition")]
    [SerializeField] private int currentCondition = 40;
    [SerializeField] private int maxCondition = 100;
    [SerializeField] private int repairAmount = 30;

    public int CurrentCondition => currentCondition;
    public int MaxCondition => maxCondition;

    public void Repair()
    {
        if (currentCondition >= maxCondition)
        {
            Debug.Log("Panel is already fully repaired.");

            panelDisplay.ShowFeedback("DEFENSE ALREADY FULL");
            return;
        }

        if (!resourceManager.SpendRepairKit(1))
        {
            Debug.Log("No repair kits available.");

            panelDisplay.ShowFeedback("NO REPAIRS AVAILABLE");
            return;
        }

        currentCondition = Mathf.Min(
            currentCondition + repairAmount,
            maxCondition
        );

        Debug.Log(
            "Panel repaired. Condition: " +
            currentCondition + "/" + maxCondition
        );
    }

    [ContextMenu("Repair Panel")]
    private void TestRepair()
    {
        Repair();
    }
}