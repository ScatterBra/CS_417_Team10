using System.Collections;
using UnityEngine;

public class SCV_Crusher : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;

    [Header("Recipe")]
    [SerializeField] private int scrapCost = 2;
    [SerializeField] private int partsProduced = 1;

    [Header("Processing")]
    [SerializeField] private float processingTime = 3f;

    [Header("Crusher Condition")]
    [SerializeField] private int crusherCondition = 70;
    [SerializeField] private int maxCrusherCondition = 100;
    [SerializeField] private int repairAmount = 30;
    [SerializeField] private int conditionLossPerProcess = 10;

    [Header("Feedback")]
    [SerializeField] private float feedbackDuration = 2f;

    [SerializeField] private SCV_ProgressionManager progressionManager;

    private string feedbackMessage = "";
    private float feedbackEndTime = -1f;

    public string FeedbackMessage
    {
        get
        {
            if (Time.time <= feedbackEndTime)
                return feedbackMessage;

            return "";
        }
    }

    private void SetFeedback(string message)
    {
        feedbackMessage = message;
        feedbackEndTime = Time.time + feedbackDuration;
    }


    private bool isProcessing = false;

    public bool IsProcessing => isProcessing;

    public int ScrapCost => scrapCost;
    public int PartsProduced => partsProduced;

    public int CrusherCondition => crusherCondition;
    public int MaxCrusherCondition => maxCrusherCondition;

    [SerializeField] private int emergencyRepairScrapCost = 20;
    [SerializeField] private int emergencyRepairCondition = 20;

    public void ProcessScrap()
    {
        if (isProcessing)
        {
            SetFeedback(
                "<color=#FF4444>CRUSHER BUSY\n" +
                "WAIT FOR PROCESSING TO FINISH</color>"
            );

            Debug.Log("Crusher is already processing.");
            return;
        }

        if (crusherCondition <= 0)
        {
            if (resourceManager.repairKits > 0)
            {
                SetFeedback(
                    "<color=#FF4444>CRUSHER BROKEN\n" +
                    "USE A REPAIR KIT TO FIX IT</color>"
                );
            }
            else if (resourceManager.parts >= 2)
            {
                SetFeedback(
                    "<color=#FF4444>CRUSHER BROKEN\n" +
                    "CRAFT A REPAIR KIT AT THE WORKBENCH</color>"
                );
            }
            else
            {
                SetFeedback(
                    "<color=#FF4444>CRUSHER BROKEN\n" +
                    "NO REPAIR KIT OR ENOUGH PARTS\n" +
                    "EMERGENCY REPAIR: " +
                    emergencyRepairScrapCost +
                    " SCRAP</color>"
                );
            }

            Debug.Log("Crusher is broken and needs repair.");
            return;
        }

        if (!resourceManager.SpendScrap(scrapCost))
        {
            SetFeedback(
                "<color=#FF4444>CANNOT PROCESS\n" +
                "NEED " + scrapCost + " SCRAP\n" +
                "HARVEST MORE SALVAGE</color>"
            );

            Debug.Log("Not enough scrap.");
            return;
        }

        StartCoroutine(ProcessRoutine());
    }

    private IEnumerator ProcessRoutine()
    {
        isProcessing = true;

        Debug.Log("Crusher started processing...");

        yield return new WaitForSeconds(processingTime);

        resourceManager.AddParts(partsProduced);

        if (progressionManager != null)
        {
            progressionManager.FirstPartProduced();
        }

        crusherCondition = Mathf.Max(
            crusherCondition - conditionLossPerProcess,
            0
        );

        Debug.Log(
            "Crusher finished. Produced " +
            partsProduced +
            " part. Condition: " +
            crusherCondition +
            "/" +
            maxCrusherCondition
        );

        isProcessing = false;
    }

    public void TestRun()
    {
        if (crusherCondition <= 0)
        {
            SetFeedback(
                "<color=#FF4444>TEST FAILED\n" +
                "CRUSHER MUST BE REPAIRED FIRST</color>"
            );

            return;
        }

        SetFeedback(
            "<color=#66FF66>CRUSHER TEST SUCCESSFUL\n" +
            "SYSTEM READY FOR PROCESSING</color>"
        );

        Debug.Log("Crusher sandbox test completed.");
    }

    public bool TryRepair()
    {
        if (crusherCondition >= maxCrusherCondition)
        {
            SetFeedback(
                "<color=#FF4444>CRUSHER ALREADY FULLY REPAIRED\n" +
                "NO REPAIR KIT NEEDED</color>"
            );

            Debug.Log(
                "Crusher is already fully repaired."
            );

            return false;
        }

        if (!resourceManager.SpendRepairKit(1))
        {
            SetFeedback(
                "<color=#FF4444>CANNOT REPAIR CRUSHER\n" +
                "NO REPAIR KIT AVAILABLE\n" +
                "CRAFT ONE AT THE WORKBENCH</color>"
            );

            Debug.Log(
                "No repair kits available."
            );

            return false;
        }

        crusherCondition = Mathf.Min(
            crusherCondition + repairAmount,
            maxCrusherCondition
        );

        if (crusherCondition >= maxCrusherCondition)
        {
            SetFeedback("<color=#66FF66>CRUSHER FULLY REPAIRED</color>");
        }
        else
        {
            SetFeedback("<color=#66FF66>CRUSHER REPAIRED</color>");
        }

        Debug.Log(
            "Crusher repaired: " +
            crusherCondition +
            "/" +
            maxCrusherCondition
        );

        return true;
    }

    public bool TryEmergencyRepair()
    {
        if (crusherCondition > 0)
        {
            SetFeedback(
                "<color=#FFCC66>EMERGENCY REPAIR NOT NEEDED\n" +
                "CRUSHER IS STILL OPERATIONAL</color>"
            );

            return false;
        }

        if (resourceManager.repairKits > 0)
        {
            SetFeedback(
                "<color=#FFCC66>REPAIR KIT AVAILABLE\n" +
                "USE THE REPAIR KIT INSTEAD</color>"
            );

            return false;
        }

        if (resourceManager.parts >= 2)
        {
            SetFeedback(
                "<color=#FFCC66>REPAIR KIT CAN BE CRAFTED\n" +
                "USE 2 PARTS AT THE WORKBENCH</color>"
            );

            return false;
        }

        if (!resourceManager.SpendScrap(emergencyRepairScrapCost))
        {
            SetFeedback(
                "<color=#FF4444>CRUSHER DISABLED\n" +
                "NO REPAIR KIT OR ENOUGH PARTS\n" +
                "EMERGENCY REPAIR NEEDS " +
                emergencyRepairScrapCost +
                " SCRAP</color>"
            );

            return false;
        }

        crusherCondition = emergencyRepairCondition;

        SetFeedback(
            "<color=#66FF66>EMERGENCY REPAIR COMPLETE\n" +
            "CRUSHER RESTORED TO " +
            crusherCondition + "/" +
            maxCrusherCondition + "</color>"
        );

        Debug.Log(
            "Emergency crusher repair completed. Condition: " +
            crusherCondition + "/" + maxCrusherCondition
        );

        return true;
    }

    public void EmergencyRepair()
    {
        TryEmergencyRepair();
    }

    [ContextMenu("Process Scrap")]
    private void TestProcessScrap()
    {
        ProcessScrap();
    }

    [ContextMenu("Repair Crusher")]
    private void TestRepairCrusher()
    {
        TryRepair();
    }

    [ContextMenu("Emergency Repair Crusher")]
    private void TestEmergencyRepair()
    {
        TryEmergencyRepair();
    }
}
