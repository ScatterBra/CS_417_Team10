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

    private bool isProcessing = false;

    public bool IsProcessing => isProcessing;

    public void ProcessScrap()
    {
        if (isProcessing)
        {
            Debug.Log("Crusher is already processing.");
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

        Debug.Log("Crusher finished. Produced " + partsProduced + " part.");

        isProcessing = false;
    }

    [ContextMenu("Process Scrap")]
    private void TestProcessScrap()
    {
        ProcessScrap();
    }
}