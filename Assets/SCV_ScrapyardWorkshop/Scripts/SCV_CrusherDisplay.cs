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
            stateText = "<color=#66FF66>PROCESSING...</color>";
        }
        else if (crusher.CrusherCondition <= 0)
        {
            stateText = "<color=#FF4444>BROKEN\nREPAIR REQUIRED</color>";
        }
        else if (resourceManager.scrap < crusher.ScrapCost)
        {
            stateText =
                "<color=#FF4444>NEED " +
                crusher.ScrapCost +
                " SCRAP</color>";
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