using UnityEngine;
using TMPro;

public class SCV_ProgressionManager : MonoBehaviour
{
    public bool defenseRecording;
    public enum WorkshopStage
    {
        HarvestSalvage,
        ProcessPart,
        CraftSupplies,
        WorkshopComplete
    }

    [Header("Current Progress")]
    [SerializeField]
    private WorkshopStage currentStage =
        WorkshopStage.HarvestSalvage;

    [Header("Unlockable Systems")]
    [SerializeField] private GameObject crusherInteraction;
    [SerializeField] private GameObject workbenchInteraction;

    [Header("Objective UI")]
    [SerializeField] private TMP_Text objectiveText;

    [Header("Unlock Reveal UI")]
    [SerializeField] private GameObject revealCanvas;
    [SerializeField] private TMP_Text revealText;
    [SerializeField] private float revealDuration = 4f;

    [SerializeField] private float interactionUnlockDelay = 2f;

    [SerializeField] private GameObject defenseSupplyInteraction;

    public bool CrusherReady => !defenseRecording || (crusherInteraction != null && crusherInteraction.activeInHierarchy);
    public bool WorkbenchReady => !defenseRecording || (workbenchInteraction != null && workbenchInteraction.activeInHierarchy);
    public bool DefenseSupplyReady => !defenseRecording || (defenseSupplyInteraction != null && defenseSupplyInteraction.activeInHierarchy);
    private float UnlockDelay => defenseRecording ? Mathf.Max(interactionUnlockDelay,revealDuration)+.1f : interactionUnlockDelay;
    private bool craftedAmmo = false;
    private bool craftedRepairKit = false;

    public bool WorkshopComplete =>
    currentStage == WorkshopStage.WorkshopComplete;

    private void Start()
    {
        // MP2B begins with later mechanics locked.
        if (crusherInteraction != null)
            crusherInteraction.SetActive(false);

        if (workbenchInteraction != null)
            workbenchInteraction.SetActive(false);

        if (revealCanvas != null)
            revealCanvas.SetActive(false);

        if (defenseSupplyInteraction != null)
            defenseSupplyInteraction.SetActive(false);

        UpdateObjective();
    }

    public void FirstWreckDepleted()
    {
        if (currentStage != WorkshopStage.HarvestSalvage)
            return;

        currentStage = WorkshopStage.ProcessPart;

        ShowReveal(
            "<color=#66D9FF>CRUSHER ONLINE</color>\n" +
            "Process salvaged Scrap into reusable Parts."
        );

        UpdateObjective();

        Invoke(
            nameof(EnableCrusherInteraction),
            UnlockDelay
        );
    }

    private void EnableCrusherInteraction()
    {
        if (crusherInteraction != null)
            crusherInteraction.SetActive(true);
    }

    public void FirstPartProduced()
    {
        if (currentStage != WorkshopStage.ProcessPart)
            return;

        currentStage = WorkshopStage.CraftSupplies;

        ShowReveal(
            "<color=#66D9FF>WORKBENCH ONLINE</color>\n" +
            "Use Parts to manufacture Ammo and Repair Kits."
        );

        UpdateObjective();

        Invoke(
            nameof(EnableWorkbenchInteraction),
            UnlockDelay
        );
    }

    private void EnableWorkbenchInteraction()
    {
        if (workbenchInteraction != null)
            workbenchInteraction.SetActive(true);
    }
    public void AmmoCrafted()
    {
        craftedAmmo = true;
        CheckWorkshopComplete();
    }

    public void RepairKitCrafted()
    {
        craftedRepairKit = true;
        CheckWorkshopComplete();
    }

    private void CheckWorkshopComplete()
    {
        if (currentStage != WorkshopStage.CraftSupplies)
            return;

        if (!craftedAmmo || !craftedRepairKit)
            return;

        currentStage = WorkshopStage.WorkshopComplete;

        ShowReveal(
            "<color=#66D9FF>WORKSHOP READY</color>\n" +
            "Supplies are prepared for the Defense Yard."
        );

        UpdateObjective();

        Invoke(
            nameof(EnableDefenseSupplyInteraction),
            UnlockDelay
        );
    }

    private void EnableDefenseSupplyInteraction()
    {
        if (defenseSupplyInteraction != null)
            defenseSupplyInteraction.SetActive(true);
    }

    private void UpdateObjective()
    {
        if (objectiveText == null)
            return;

        if(defenseRecording) objectiveText.gameObject.SetActive(currentStage != WorkshopStage.WorkshopComplete);
        switch (currentStage)
        {
            case WorkshopStage.HarvestSalvage:
                objectiveText.text =
                    "WORKSHOP OBJECTIVE\n" +
                    "HARVEST A SALVAGE WRECK";
                break;

            case WorkshopStage.ProcessPart:
                objectiveText.text =
                    "WORKSHOP OBJECTIVE\n" +
                    "PROCESS 2 SCRAP INTO 1 PART";
                break;

            case WorkshopStage.CraftSupplies:
                objectiveText.text =
                    "WORKSHOP OBJECTIVE\n" +
                    "CRAFT AMMO AND A REPAIR KIT";
                break;

            case WorkshopStage.WorkshopComplete:
                objectiveText.text =
                    "WORKSHOP OBJECTIVE\n" +
                    (defenseRecording ? "CRAFTING READY: Keep your kit and ammo. Transfer 4 Scrap, then carry supplies to Defense." : "SUPPLY THE DEFENSE YARD");
                break;
        }
    }

    private void ShowReveal(string message)
    {
        if (revealCanvas == null || revealText == null)
            return;

        revealText.text = message;
        revealCanvas.SetActive(true);

        CancelInvoke(nameof(HideReveal));
        Invoke(nameof(HideReveal), revealDuration);
    }

    private void HideReveal()
    {
        if (revealCanvas != null)
            revealCanvas.SetActive(false);
    }
}

