using UnityEngine;
using TMPro;

public class SCV_ProgressionManager : MonoBehaviour
{
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

        UpdateObjective();
    }

    public void FirstWreckDepleted()
    {
        if (currentStage != WorkshopStage.HarvestSalvage)
            return;

        currentStage = WorkshopStage.ProcessPart;

        if (crusherInteraction != null)
            crusherInteraction.SetActive(true);

        ShowReveal(
            "<color=#66D9FF>CRUSHER ONLINE</color>\n" +
            "Process salvaged Scrap into reusable Parts."
        );

        UpdateObjective();
    }

    public void FirstPartProduced()
    {
        if (currentStage != WorkshopStage.ProcessPart)
            return;

        currentStage = WorkshopStage.CraftSupplies;

        if (workbenchInteraction != null)
            workbenchInteraction.SetActive(true);

        ShowReveal(
            "<color=#66D9FF>WORKBENCH ONLINE</color>\n" +
            "Use Parts to manufacture Ammo and Repair Kits."
        );

        UpdateObjective();
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
    }

    private void UpdateObjective()
    {
        if (objectiveText == null)
            return;

        switch (currentStage)
        {
            case WorkshopStage.HarvestSalvage:
                objectiveText.text =
                    "CURRENT OBJECTIVE\n" +
                    "HARVEST A SALVAGE WRECK";
                break;

            case WorkshopStage.ProcessPart:
                objectiveText.text =
                    "CURRENT OBJECTIVE\n" +
                    "PROCESS 2 SCRAP INTO 1 PART";
                break;

            case WorkshopStage.CraftSupplies:
                objectiveText.text =
                    "CURRENT OBJECTIVE\n" +
                    "CRAFT AMMO AND A REPAIR KIT";
                break;

            case WorkshopStage.WorkshopComplete:
                objectiveText.text =
                    "CURRENT OBJECTIVE\n" +
                    "SUPPLY THE DEFENSE YARD";
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