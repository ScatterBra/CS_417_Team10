using TMPro;
using UnityEngine;

public class SCV_CrusherDisplay : MonoBehaviour
{
    [SerializeField] private SCV_Crusher crusher;
    [SerializeField] private SCV_ResourceManager resourceManager;
    [SerializeField] private TMP_Text statusText;

    private void Update()
    {
        if (crusher == null ||
            resourceManager == null ||
            statusText == null)
        {
            return;
        }

        string stateText;

        if (!string.IsNullOrEmpty(crusher.FeedbackMessage))
        {
            stateText = crusher.FeedbackMessage;
        }
        else if (crusher.IsProcessing)
        {
            stateText = "PROCESSING...";
        }
        else if (crusher.CrusherCondition <= 0)
        {
            stateText = "BROKEN\nREPAIR REQUIRED";
        }
        else if (resourceManager.scrap < crusher.ScrapCost)
        {
            stateText =
                "NEED " +
                crusher.ScrapCost +
                " SCRAP";
        }
        else
        {
            stateText =
                "READY\n" +
                crusher.ScrapCost +
                " SCRAP -> " +
                crusher.PartsProduced +
                " PART";
        }

        statusText.text =
            "CRUSHER\n" +
            "CONDITION: " +
            crusher.CrusherCondition +
            "/" +
            crusher.MaxCrusherCondition +
            "\n" +
            stateText;
    }
}