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
        TryRepair();
    }

    public bool TryRepair()
    {
        if (currentCondition >= maxCondition)
        {
            Debug.Log("Defense panel is already fully repaired.");

            if (panelDisplay != null)
                panelDisplay.ShowFeedback(
                    "<color=#FF4444>DEFENSE ALREADY FULLY REPAIRED\n" +
                    "SAVE THE REPAIR KIT FOR DAMAGE</color>"
                );

            return false;
        }

        if (!resourceManager.SpendRepairKit(1))
        {
            Debug.Log("No repair kits available.");

            if (panelDisplay != null)
                panelDisplay.ShowFeedback(
                    "<color=#FF4444>CANNOT REPAIR DEFENSE\n" +
                    "NO REPAIR KIT AVAILABLE\n" +
                    "CRAFT ONE AT THE WORKBENCH</color>"
                );

            return false;
        }

        currentCondition = Mathf.Min(
            currentCondition + repairAmount,
            maxCondition
        );

        Debug.Log(
            "Defense panel repaired: " +
            currentCondition + "/" + maxCondition
        );

        if (panelDisplay != null)
            panelDisplay.ShowFeedback(
                "<color=#66FF66>REPAIR TEST SUCCESSFUL\n" +
                "DEFENSE RESTORED</color>"
            );

        return true;
    }

    [ContextMenu("Repair Panel")]
    private void TestRepair()
    {
        Repair();
    }
}