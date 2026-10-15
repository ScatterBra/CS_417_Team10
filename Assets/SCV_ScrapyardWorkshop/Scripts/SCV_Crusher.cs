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

    public void ProcessScrap()
    {
        if (isProcessing)
        {
            Debug.Log("Crusher is already processing.");
            return;
        }

        if (crusherCondition <= 0)
        {
            Debug.Log("Crusher is broken and needs repair.");
            return;
        }

        if (!resourceManager.SpendScrap(scrapCost))
        {
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

    public bool TryRepair()
    {
        if (crusherCondition >= maxCrusherCondition)
        {
            SetFeedback("ALREADY FULLY REPAIRED");

            Debug.Log(
                "Crusher is already fully repaired."
            );

            return false;
        }

        if (!resourceManager.SpendRepairKit(1))
        {
            SetFeedback("NO REPAIR KIT AVAILABLE");

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
            SetFeedback("CRUSHER FULLY REPAIRED");
        }
        else
        {
            SetFeedback("CRUSHER REPAIRED");
        }

        Debug.Log(
            "Crusher repaired: " +
            crusherCondition +
            "/" +
            maxCrusherCondition
        );

        return true;
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
}
